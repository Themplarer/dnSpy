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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler;

static class TypeFormatterUtils
{
    public const int DigitGroupSizeHex = 4;
    public const int DigitGroupSizeDecimal = 3;
    public const string DigitSeparator = "_";
    public const string NaN = "NaN";
    public const string NegativeInfinity = "-Infinity";
    public const string PositiveInfinity = "Infinity";

    public static string ToFormattedNumber(bool digitSeparators, string prefix, string number, int digitGroupSize)
    {
        if (digitSeparators)
            number = AddDigitSeparators(number, digitGroupSize, DigitSeparator);

        var res = number;

        return prefix.Length != 0 ? prefix + res : res;
    }

    private static string AddDigitSeparators(string number, int digitGroupSize, string digitSeparator)
    {
        if (number.Length <= digitGroupSize)
            return number;

        var sb = new StringBuilder();

        for (var i = 0; i < number.Length; i++)
        {
            var d = number.Length - i;

            if (i != 0 && d % digitGroupSize == 0 && number[i - 1] != '-')
                sb.Append(digitSeparator);

            sb.Append(number[i]);
        }

        return sb.ToString();
    }

    public const int MaxRecursion = 200;
    public const int MaxOutputLen = 1024 * 4;

    public static string FilterName(string? s)
    {
        const int maxNameLen = 0x100;

        if (s is null)
            return "<<NULL>>";

        var sb = new StringBuilder(s.Length);

        foreach (var c in s.TakeWhile(_ => sb.Length < maxNameLen))
        {
            if (c >= ' ')
                sb.Append(c);
            else
                sb.Append($"\\u{(ushort)c:X4}");
        }

        if (sb.Length > maxNameLen)
            sb.Length = maxNameLen;

        return sb.ToString();
    }

    public static string RemoveGenericTick(string s)
    {
        var index = s.LastIndexOf('`');

        if (index < 0)
            return s;

        if (s[0] == '<') // check if compiler generated name
            return s;

        return s[..index];
    }

    public static string GetFileName(string s)
    {
        // Don't use Path.GetFileName() since it can throw if input contains invalid chars
        var index = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
        return index < 0 ? s : s[(index + 1)..];
    }

    public static string? GetNumberOfOverloadsString(TypeDef type, IMethod method)
    {
        string name = method.Name;
        var overloads = GetNumberOfOverloads(type, name, checkBaseTypes: name is not (".ctor" or ".cctor"));

        return overloads switch
        {
            1 => $" (+ {dnSpy_Decompiler_Resources.ToolTip_OneMethodOverload})",
            > 1 => $" (+ {string.Format(dnSpy_Decompiler_Resources.ToolTip_NMethodOverloads, overloads)})",
            _ => null
        };
    }

    private static int GetNumberOfOverloads(TypeDef? type, string name, bool checkBaseTypes)
    {
        var hash = new HashSet<MethodDef>(MethodEqualityComparer.DontCompareDeclaringTypes);

        while (type is not null)
        {
            foreach (var m in type.Methods)
            {
                if (m.Name == name)
                    hash.Add(m);
            }

            if (!checkBaseTypes)
                break;

            type = type.BaseType.ResolveTypeDef();
        }

        Debug.Assert(hash.Count >= 1);
        return hash.Count - 1;
    }

    public static string? GetPropertyName(IMethod? method)
    {
        if (method is null)
            return null;

        var name = method.Name;

        if (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal))
            return name.Substring(4);

        return null;
    }

    public static string GetName(ISourceVariable variable)
    {
        var n = variable.Name;

        if (!string.IsNullOrWhiteSpace(n))
            return n;

        if (variable.Variable is not null)
            return variable.IsLocal ? "V_" + variable.Variable.Index : "A_" + variable.Variable.Index;

        Debug.Fail("Decompiler generated variable without a name");
        return "???";
    }

    public static bool IsSystemNullable(GenericInstSig gis) =>
        gis.GenericType as ValueTypeSig is { TypeDefOrRef: { } } gt &&
        gt.TypeDefOrRef.DefinitionAssembly.IsCorLib() &&
        gt.TypeDefOrRef.FullName == "System.Nullable`1";

    public static bool IsSystemValueTuple(GenericInstSig gis) => GetSystemValueTupleRank(gis) >= 0;

    private static int GetSystemValueTupleRank(GenericInstSig gis)
    {
        var gis2 = gis;
        var rank = 0;

        for (var i = 0; i < 1000; i++)
        {
            var currentRank = GetValueTupleSimpleRank(gis2);

            if (currentRank < 0)
                return -1;

            if (rank < 8)
                return rank + currentRank;

            rank += currentRank - 1;
            gis2 = gis2.GenericArguments[currentRank - 1] as GenericInstSig;

            if (gis2 is null)
                return -1;
        }

        return -1;
    }

    private static int GetValueTupleSimpleRank(GenericInstSig gis)
    {
        var gt = gis.GenericType as ValueTypeSig;

        if (gt?.TypeDefOrRef is null)
            return -1;

        if (gt.Namespace != "System")
            return -1;

        int rank;

        switch (gt.TypeDefOrRef.Name.String)
        {
            case "ValueTuple`1":
                rank = 1;
                break;
            case "ValueTuple`2":
                rank = 2;
                break;
            case "ValueTuple`3":
                rank = 3;
                break;
            case "ValueTuple`4":
                rank = 4;
                break;
            case "ValueTuple`5":
                rank = 5;
                break;
            case "ValueTuple`6":
                rank = 6;
                break;
            case "ValueTuple`7":
                rank = 7;
                break;
            case "ValueTuple`8":
                rank = 8;
                break;
            default: return -1;
        }

        if (gis.GenericArguments.Count != rank)
            return -1;

        return rank;
    }

    public static bool IsDelegate(TypeDef? td) =>
        td is not null &&
        new SigComparer().Equals(td.BaseType, td.Module.CorLibTypes.GetTypeRef("System", "MulticastDelegate")) &&
        td.BaseType.DefinitionAssembly.IsCorLib();

    public static (PropertyDef? property, AccessorKind kind) TryGetProperty(MethodDef? method)
    {
        if (method is null)
            return (null, AccessorKind.None);

        foreach (var p in method.DeclaringType.Properties)
        {
            if (method == p.GetMethod)
                return (p, AccessorKind.Getter);

            if (method == p.SetMethod)
                return (p, AccessorKind.Setter);
        }

        return (null, AccessorKind.None);
    }

    public static (EventDef? @event, AccessorKind kind) TryGetEvent(MethodDef? method)
    {
        if (method is null)
            return (null, AccessorKind.None);

        foreach (var e in method.DeclaringType.Events)
        {
            if (method == e.AddMethod)
                return (e, AccessorKind.Adder);

            if (method == e.RemoveMethod)
                return (e, AccessorKind.Remover);
        }

        return (null, AccessorKind.None);
    }

    public static bool IsDeprecated(IMethod? method)
    {
        var md = method.ResolveMethodDef();
        return md is not null && IsDeprecated(md.CustomAttributes);
    }

    public static bool IsDeprecated(IField? field)
    {
        var fd = field.ResolveFieldDef();
        return fd is not null && IsDeprecated(fd.CustomAttributes);
    }

    public static bool IsDeprecated(PropertyDef? prop) => prop is not null && IsDeprecated(prop.CustomAttributes);

    public static bool IsDeprecated(EventDef? evt) => evt is not null && IsDeprecated(evt.CustomAttributes);

    public static bool IsDeprecated(ITypeDefOrRef? type)
    {
        var td = type.ResolveTypeDef();

        if (td is null)
            return false;

        var foundByRefLikeMarker = false;

        foreach (var ca in td.CustomAttributes)
        {
            if (ca.TypeFullName != "System.ObsoleteAttribute")
                continue;

            if (ca.ConstructorArguments.Count != 2)
                return true;

            if (!(ca.ConstructorArguments[0].Value is UTF8String s && s == ByRefLikeMarker))
                return true;

            if (ca.ConstructorArguments[1].Value is not true)
                return true;

            foundByRefLikeMarker = true;
        }

        return foundByRefLikeMarker && !IsByRefLike(td);
    }

    private static readonly UTF8String ByRefLikeMarker = new("Types with embedded references are not supported in this version of your compiler.");

    static bool IsDeprecated(CustomAttributeCollection customAttributes) => customAttributes.Any(ca => ca.TypeFullName == "System.ObsoleteAttribute");

    public static bool IsByRefLike(TypeDef td) =>
        td.CustomAttributes.Any(ca => ca.TypeFullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");

    static bool IsExtension(CustomAttributeCollection customAttributes) =>
        customAttributes.Any(ca => ca.TypeFullName == "System.Runtime.CompilerServices.ExtensionAttribute");

    static bool IsAwaitableType(TypeSig? type)
    {
        var td = type?.Resolve();
        return td is not null && IsAwaitableType(td);
    }

    static bool IsAwaitableType(TypeDef? td)
    {
        if (td is null)
            return false;

        // See (Roslyn): IsCustomTaskType

        if (td.GenericParameters.Count > 1)
            return false;

        if (td.Namespace == stringSystem_Threading_Tasks)
        {
            if (td.Name == stringTask || td.Name == stringTask_1)
                return true;
        }

        foreach (var ca in td.CustomAttributes)
        {
            if (ca.TypeFullName != "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute")
                continue;
            if (ca.ConstructorArguments.Count != 1)
                continue;
            if ((ca.ConstructorArguments[0].Type as ClassSig)?.TypeDefOrRef.FullName != "System.Type")
                continue;

            return true;
        }

        return false;
    }

    static readonly UTF8String stringSystem_Threading_Tasks = new("System.Threading.Tasks");
    static readonly UTF8String stringTask = new("Task");
    static readonly UTF8String stringTask_1 = new("Task`1");

    public static MemberSpecialFlags GetMemberSpecialFlags(IMethod method)
    {
        var flags = MemberSpecialFlags.None;

        var md = method.ResolveMethodDef();
        if (md is not null && IsExtension(md.CustomAttributes))
            flags |= MemberSpecialFlags.Extension;

        if (IsAwaitableType(method.MethodSig.GetRetType()))
            flags |= MemberSpecialFlags.Awaitable;

        return flags;
    }

    public static MemberSpecialFlags GetMemberSpecialFlags(ITypeDefOrRef type)
    {
        var flags = MemberSpecialFlags.None;

        if (IsAwaitableType(type.ResolveTypeDef()))
            flags |= MemberSpecialFlags.Awaitable;

        return flags;
    }

    public static bool HasConstant(IHasConstant? hc, out CustomAttribute? constantAttribute)
    {
        constantAttribute = null;
        if (hc is null)
            return false;
        if (hc.Constant is not null)
            return true;

        foreach (var ca in hc.CustomAttributes)
        {
            var type = ca.AttributeType;

            while (type is not null)
            {
                var fullName = type.FullName;

                if (fullName is "System.Runtime.CompilerServices.CustomConstantAttribute" or "System.Runtime.CompilerServices.DecimalConstantAttribute")
                {
                    constantAttribute = ca;
                    return true;
                }

                type = type.GetBaseType();
            }
        }

        return false;
    }

    public static bool TryGetConstant(IHasConstant? hc, CustomAttribute? constantAttribute, out object? constant)
    {
        if (hc?.Constant is not null)
        {
            constant = hc.Constant.Value;
            return true;
        }

        if (constantAttribute is not null &&
            constantAttribute.TypeFullName == "System.Runtime.CompilerServices.DecimalConstantAttribute" &&
            TryGetDecimalConstantAttributeValue(constantAttribute, out var decimalValue))
        {
            constant = decimalValue;
            return true;
        }

        constant = null;
        return false;
    }

    static bool TryGetDecimalConstantAttributeValue(CustomAttribute ca, out decimal value)
    {
        value = 0;
        if (ca.ConstructorArguments.Count != 5)
            return false;
        if (!(ca.ConstructorArguments[0].Value is byte scale))
            return false;
        if (!(ca.ConstructorArguments[1].Value is byte sign))
            return false;

        int hi, mid, low;

        if (ca.ConstructorArguments[2].Value is int)
        {
            if (ca.ConstructorArguments[2].Value is not int ||
                ca.ConstructorArguments[3].Value is not int ||
                ca.ConstructorArguments[4].Value is not int)
                return false;

            hi = (int)ca.ConstructorArguments[2].Value;
            mid = (int)ca.ConstructorArguments[3].Value;
            low = (int)ca.ConstructorArguments[4].Value;
        }
        else if (ca.ConstructorArguments[2].Value is uint)
        {
            if (ca.ConstructorArguments[2].Value is not uint ||
                ca.ConstructorArguments[3].Value is not uint ||
                ca.ConstructorArguments[4].Value is not uint)
                return false;

            hi = (int)(uint)ca.ConstructorArguments[2].Value;
            mid = (int)(uint)ca.ConstructorArguments[3].Value;
            low = (int)(uint)ca.ConstructorArguments[4].Value;
        }
        else
            return false;

        try
        {
            value = new decimal(low, mid, hi, sign > 0, scale);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool IsReadOnlyProperty(PropertyDef property) => HasIsReadOnlyAttribute(property.CustomAttributes);

    public static bool IsReadOnlyMethod(MethodDef? method)
    {
        if (method is null || method.IsConstructor)
            return false;

        return HasIsReadOnlyAttribute(method.Parameters.ReturnParameter.ParamDef?.CustomAttributes);
    }

    public static bool IsReadOnlyParameter(ParamDef? pd) => HasIsReadOnlyAttribute(pd?.CustomAttributes);

    public static bool IsReadOnlyType(TypeDef? td) => HasIsReadOnlyAttribute(td?.CustomAttributes);

    static bool HasIsReadOnlyAttribute(CustomAttributeCollection? customAttributes) =>
        customAttributes is not null &&
        customAttributes.Any(ca => ca.AttributeType is { FullName: "System.Runtime.CompilerServices.IsReadOnlyAttribute", DeclaringType: null });

    public static bool HasDynamicAttribute(IHasCustomAttribute? attributeProvider, int typeIndex)
    {
        if (attributeProvider is null)
            return false;

        foreach (var a in attributeProvider.CustomAttributes)
        {
            if (a.AttributeType?.FullName != "System.Runtime.CompilerServices.DynamicAttribute")
                continue;

            if (a.ConstructorArguments.Count == 0)
                return true;
            
            if (a.ConstructorArguments.Count == 1 &&
                a.ConstructorArguments[0].Value is IList<CAArgument> values &&
                typeIndex < values.Count &&
                values[typeIndex].Value is bool b)
                return b;
        }

        return false;
    }
}

enum AccessorKind
{
    None,
    Getter,
    Setter,
    Adder,
    Remover,
}

[Flags]
enum MemberSpecialFlags
{
    None = 0,
    Extension = 1,
    Awaitable = 2,
}