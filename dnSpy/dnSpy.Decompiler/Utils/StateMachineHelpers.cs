/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Decompiler.Utils;

public static class StateMachineHelpers
{
    private static readonly UTF8String System_Runtime_CompilerServices = new("System.Runtime.CompilerServices");
    private static readonly UTF8String IAsyncStateMachine = new("IAsyncStateMachine");
    private static readonly UTF8String AsyncStateMachineAttribute = new("AsyncStateMachineAttribute");
    private static readonly UTF8String IteratorStateMachineAttribute = new("IteratorStateMachineAttribute");
    private static readonly UTF8String stringSystem = new("System");
    private static readonly UTF8String stringType = new("Type");
    private static readonly UTF8String stringIDisposable = new("IDisposable");
    private static readonly UTF8String stringDispose = new("Dispose");
    private static readonly UTF8String System_Collections = new("System.Collections");
    private static readonly UTF8String System_Collections_Generic = new("System.Collections.Generic");
    private static readonly UTF8String IEnumerable = new("IEnumerable");
    private static readonly UTF8String IEnumerator = new("IEnumerator");
    private static readonly UTF8String IEnumerable_1 = new("IEnumerable`1");
    private static readonly UTF8String IEnumerator_1 = new("IEnumerator`1");

    private static bool EqualsName(ITypeDefOrRef tdr, UTF8String @namespace, UTF8String name) =>
        tdr switch
        {
            TypeRef tr => tr.Name == name && tr.Namespace == @namespace,
            TypeDef td => td.Name == name && td.Namespace == @namespace,
            _ => false
        };

    public static TypeDef? GetStateMachineType(MethodDef method)
    {
        var stateMachineType = GetStateMachineTypeCore(method);

        if (stateMachineType is null)
            return null;

        var body = method.Body;
        if (body is null)
            return null;

        foreach (var instr in body.Instructions)
        {
            var def = instr.Operand as IMemberDef;

            if (def?.DeclaringType == stateMachineType)
                return stateMachineType;
        }

        return null;
    }

    private static TypeDef? GetStateMachineTypeCore(MethodDef method) =>
        GetStateMachineTypeFromCustomAttributesCore(method) ??
        GetAsyncStateMachineTypeFromInstructionsCore(method) ??
        GetIteratorStateMachineTypeFromInstructionsCore(method);

    private static TypeDef? GetStateMachineTypeFromCustomAttributesCore(MethodDef method)
    {
        foreach (var ca in method.CustomAttributes)
        {
            if (ca.ConstructorArguments.Count != 1 || ca.Constructor?.MethodSig?.Params.Count != 1)
                continue;

            var typeType = (ca.Constructor.MethodSig.Params[0] as ClassOrValueTypeSig)?.TypeDefOrRef;
            if (typeType is null || !EqualsName(typeType, stringSystem, stringType) || !IsStateMachineTypeAttribute(ca.AttributeType))
                continue;

            var tdr = (ca.ConstructorArguments[0].Value as ClassOrValueTypeSig)?.TypeDefOrRef;

            if (tdr is null)
                continue;

            var td = tdr.Module.Find(tdr);

            if (td?.DeclaringType == method.DeclaringType)
                return td;
        }

        return null;
    }

    private static bool IsStateMachineTypeAttribute(ITypeDefOrRef tdr) =>
        EqualsName(tdr, System_Runtime_CompilerServices, AsyncStateMachineAttribute) ||
        EqualsName(tdr, System_Runtime_CompilerServices, IteratorStateMachineAttribute);

    private static TypeDef? GetAsyncStateMachineTypeFromInstructionsCore(MethodDef method)
    {
        var body = method.Body;
        if (body is null)
            return null;

        foreach (var local in body.Variables)
        {
            var type = local.Type.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
            var nested = type?.TypeDef;

            if (nested is null ||
                nested.DeclaringType != method.DeclaringType ||
                !ImplementsInterface(nested, System_Runtime_CompilerServices, IAsyncStateMachine))
                continue;

            return nested;
        }

        return null;
    }

    private static TypeDef? GetIteratorStateMachineTypeFromInstructionsCore(MethodDef method)
    {
        if (!IsIteratorReturnType(method.MethodSig.GetRetType().RemovePinnedAndModifiers()))
            return null;

        var instrs = method.Body?.Instructions;

        if (instrs is null)
            return null;

        foreach (var instr in instrs)
        {
            if (instr.OpCode.Code != Code.Newobj ||
                instr.Operand is not MethodDef ctor ||
                ctor.DeclaringType.DeclaringType != method.DeclaringType ||
                !ImplementsInterface(ctor.DeclaringType, stringSystem, stringIDisposable))
                continue;

            var disposeMethod = FindDispose(ctor.DeclaringType);

            if (disposeMethod is null)
                continue;

            if (!disposeMethod.CustomAttributes.IsDefined("System.Diagnostics.DebuggerHiddenAttribute"))
            {
                // This attribute isn't always present. Make sure the type has a compiler generated name
                var name = ctor.DeclaringType.Name.String;

                if (!name.StartsWith("<") && !name.StartsWith("VB$StateMachine_"))
                    continue;
            }

            return ctor.DeclaringType;
        }

        return null;
    }

    private static bool IsIteratorReturnType(TypeSig typeSig)
    {
        var tdr = (typeSig as ClassSig)?.TypeDefOrRef ?? (typeSig as GenericInstSig)?.GenericType.TypeDefOrRef;

        if (tdr is null)
            return false;

        return EqualsName(tdr, System_Collections, IEnumerable) ||
               EqualsName(tdr, System_Collections, IEnumerator) ||
               EqualsName(tdr, System_Collections_Generic, IEnumerable_1) ||
               EqualsName(tdr, System_Collections_Generic, IEnumerator_1);
    }

    private static bool ImplementsInterface(TypeDef type, UTF8String @namespace, UTF8String name) =>
        type.Interfaces.Select(t => t.Interface).Any(t => t is not null && EqualsName(t, @namespace, name));

    private static MethodDef? FindDispose(TypeDef type)
    {
        foreach (var method in type.Methods)
            if (method.Overrides
                .Any(o => o.MethodDeclaration.Name == stringDispose && IsDisposeSig(o.MethodDeclaration.MethodSig)))
                return method;

        return type.Methods.FirstOrDefault(method => method.Name == stringDispose && IsDisposeSig(method.MethodSig));
    }

    private static bool IsDisposeSig(MethodSig sig) =>
        sig is { GenParamCount: 0, ParamsAfterSentinel: null } &&
        sig.Params.Count == 0 &&
        sig.RetType.GetElementType() == ElementType.Void &&
        sig.CallingConvention == CallingConvention.HasThis;

    /// <summary>
    /// Gets the state machine kickoff method. It's the original async/iterator method that the compiler moves to the MoveNext method
    /// </summary>
    /// <param name="method">A possible state machine MoveNext method</param>
    /// <param name="kickoffMethod">Updated with kickoff method on success</param>
    /// <returns></returns>
    public static bool TryGetKickoffMethod(MethodDef method, [NotNullWhen(true)] out MethodDef? kickoffMethod)
    {
        kickoffMethod = null;
        var declType = method.DeclaringType;

        // Assume all state machine types are nested types
        if (!declType.IsNested)
            return false;

        if (ImplementsInterface(declType, System_Runtime_CompilerServices, IAsyncStateMachine))
        {
            // async method

            if (TryGetKickoffMethodFromAttributes(declType, out kickoffMethod))
                return true;

            foreach (var possibleKickoffMethod in declType.DeclaringType.Methods)
                if (GetAsyncStateMachineTypeFromInstructionsCore(possibleKickoffMethod) == declType)
                {
                    kickoffMethod = possibleKickoffMethod;
                    return true;
                }
        }
        else if (ImplementsInterface(declType, System_Collections, IEnumerator))
        {
            // IEnumerable, IEnumerable<T>, IEnumerator, IEnumerator<T>

            if (TryGetKickoffMethodFromAttributes(declType, out kickoffMethod))
                return true;

            foreach (var possibleKickoffMethod in declType.DeclaringType.Methods)
                if (GetIteratorStateMachineTypeFromInstructionsCore(possibleKickoffMethod) == declType)
                {
                    kickoffMethod = possibleKickoffMethod;
                    return true;
                }
        }

        return false;
    }

    private static bool TryGetKickoffMethodFromAttributes(TypeDef smType, [NotNullWhen(true)] out MethodDef? kickoffMethod)
    {
        foreach (var possibleKickoffMethod in smType.DeclaringType.Methods)
            if (GetStateMachineTypeFromCustomAttributesCore(possibleKickoffMethod) == smType)
            {
                kickoffMethod = possibleKickoffMethod;
                return true;
            }

        kickoffMethod = null;
        return false;
    }
}