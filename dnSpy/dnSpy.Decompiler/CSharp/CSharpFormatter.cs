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
using System.Globalization;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.CSharp;

public struct CSharpFormatter
{
    private const string Keyword_true = "true";
    private const string Keyword_false = "false";
    private const string Keyword_null = "null";
    private const string Keyword_out = "out";
    private const string Keyword_in = "in";
    private const string Keyword_ref = "ref";
    private const string Keyword_readonly = "readonly";
    private const string Keyword_this = "this";
    private const string Keyword_get = "get";
    private const string Keyword_set = "set";
    private const string Keyword_add = "add";
    private const string Keyword_remove = "remove";
    private const string Keyword_enum = "enum";
    private const string Keyword_struct = "struct";
    private const string Keyword_interface = "interface";
    private const string Keyword_class = "class";
    private const string Keyword_namespace = "namespace";
    private const string Keyword_params = "params";
    private const string Keyword_default = "default";
    private const string Keyword_delegate = "delegate";
    private const string Keyword_arglist = "__arglist";
    private const string HexPrefix = "0x";
    private const string VerbatimStringPrefix = "@";
    private const string IdentifierEscapeBegin = "@";
    private const string ModuleNameSeparator = "!";
    private const string CommentBegin = "/*";
    private const string CommentEnd = "*/";
    private const string DeprecatedParenOpen = "[";
    private const string DeprecatedParenClose = "]";
    private const string MemberSpecialParenOpen = "(";
    private const string MemberSpecialParenClose = ")";
    private const string MethodParenOpen = "(";
    private const string MethodParenClose = ")";
    private const string DescriptionParenOpen = "(";
    private const string DescriptionParenClose = ")";
    private const string IndexerParenOpen = "[";
    private const string IndexerParenClose = "]";
    private const string PropertyParenOpen = "[";
    private const string PropertyParenClose = "]";
    private const string ArrayParenOpen = "[";
    private const string ArrayParenClose = "]";
    private const string TupleParenOpen = "(";
    private const string TupleParenClose = ")";
    private const string GenericParenOpen = "<";
    private const string GenericParenClose = ">";
    private const string DefaultParamValueParenOpen = "[";
    private const string DefaultParamValueParenClose = "]";

    private static readonly char[] NamespaceSeparators = { '.' };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };

    private static readonly Dictionary<string, string[]> NameToOperatorName = new(StringComparer.Ordinal)
    {
        { "op_Addition", "operator +".Split(' ') },
        { "op_BitwiseAnd", "operator &".Split(' ') },
        { "op_BitwiseOr", "operator |".Split(' ') },
        { "op_Decrement", "operator --".Split(' ') },
        { "op_Division", "operator /".Split(' ') },
        { "op_Equality", "operator ==".Split(' ') },
        { "op_ExclusiveOr", "operator ^".Split(' ') },
        { "op_Explicit", "explicit operator".Split(' ') },
        { "op_False", "operator false".Split(' ') },
        { "op_GreaterThan", "operator >".Split(' ') },
        { "op_GreaterThanOrEqual", "operator >=".Split(' ') },
        { "op_Implicit", "implicit operator".Split(' ') },
        { "op_Increment", "operator ++".Split(' ') },
        { "op_Inequality", "operator !=".Split(' ') },
        { "op_LeftShift", "operator <<".Split(' ') },
        { "op_LessThan", "operator <".Split(' ') },
        { "op_LessThanOrEqual", "operator <=".Split(' ') },
        { "op_LogicalNot", "operator !".Split(' ') },
        { "op_Modulus", "operator %".Split(' ') },
        { "op_Multiply", "operator *".Split(' ') },
        { "op_OnesComplement", "operator ~".Split(' ') },
        { "op_RightShift", "operator >>".Split(' ') },
        { "op_Subtraction", "operator -".Split(' ') },
        { "op_True", "operator true".Split(' ') },
        { "op_UnaryNegation", "operator -".Split(' ') },
        { "op_UnaryPlus", "operator +".Split(' ') },
    };

    private int _recursionCounter;
    private int _lineLength;
    private bool _outputLengthExceeded;
    private readonly bool _forceWrite;
    private readonly StringBuilder _sb;
    private readonly ITextColorWriter _output;
    private FormatterOptions _options;
    private readonly CultureInfo _cultureInfo;

    private bool ShowModuleNames => (_options & FormatterOptions.ShowModuleNames) != 0;

    private bool ShowParameterTypes => (_options & FormatterOptions.ShowParameterTypes) != 0;

    private bool ShowParameterNames => (_options & FormatterOptions.ShowParameterNames) != 0;

    private bool ShowDeclaringTypes => (_options & FormatterOptions.ShowDeclaringTypes) != 0;

    private bool ShowReturnTypes => (_options & FormatterOptions.ShowReturnTypes) != 0;

    private bool ShowNamespaces => (_options & FormatterOptions.ShowNamespaces) != 0;

    private bool ShowIntrinsicTypeKeywords => (_options & FormatterOptions.ShowIntrinsicTypeKeywords) != 0;

    private bool UseDecimal => (_options & FormatterOptions.UseDecimal) != 0;

    private bool ShowTokens => (_options & FormatterOptions.ShowTokens) != 0;

    private bool ShowArrayValueSizes => (_options & FormatterOptions.ShowArrayValueSizes) != 0;

    private bool ShowFieldLiteralValues => (_options & FormatterOptions.ShowFieldLiteralValues) != 0;

    private bool ShowParameterLiteralValues => (_options & FormatterOptions.ShowParameterLiteralValues) != 0;

    private bool DigitSeparators => (_options & FormatterOptions.DigitSeparators) != 0;

    public CSharpFormatter(ITextColorWriter output, FormatterOptions options, CultureInfo? cultureInfo)
    {
        _output = output;
        _options = options;
        _cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        _recursionCounter = 0;
        _lineLength = 0;
        _outputLengthExceeded = false;
        _forceWrite = false;
        _sb = new StringBuilder();
    }

    private void WriteIdentifier(string id, object data)
    {
        if (Keywords.Contains(id))
            OutputWrite(IdentifierEscapeBegin + IdentifierEscaper.Escape(id), data);
        else
            OutputWrite(IdentifierEscaper.Escape(id), data);
    }

    private void OutputWrite(string s, object data)
    {
        if (!_forceWrite)
        {
            if (_outputLengthExceeded)
                return;

            if (_lineLength + s.Length > TypeFormatterUtils.MaxOutputLen)
            {
                s = s[..(TypeFormatterUtils.MaxOutputLen - _lineLength)];
                s += "[...]";
                _outputLengthExceeded = true;
            }
        }

        _output.Write(data, s);
        _lineLength += s.Length;
    }

    private void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);

    private void WriteCommaSpace()
    {
        OutputWrite(",", BoxedTextColor.Punctuation);
        WriteSpace();
    }

    private void WritePeriod() => OutputWrite(".", BoxedTextColor.Operator);

    private void WriteError() => OutputWrite("???", BoxedTextColor.Error);

    private void WriteSystemTypeKeyword(string name, string keyword, bool isValueType)
    {
        if (ShowIntrinsicTypeKeywords)
            OutputWrite(keyword, BoxedTextColor.Keyword);
        else
            WriteSystemType(name, isValueType);
    }

    private void WriteSystemType(string name, bool isValueType)
    {
        if (ShowNamespaces)
        {
            OutputWrite("System", BoxedTextColor.Namespace);
            WritePeriod();
        }

        OutputWrite(name, isValueType ? BoxedTextColor.ValueType : BoxedTextColor.Type);
    }

    private void WriteToken(IMDTokenProvider tok)
    {
        if (!ShowTokens)
            return;

        Debug.Assert(tok is not null);

        if (tok is null)
            return;

        OutputWrite(CommentBegin + ToFormattedUInt32(tok.MDToken.Raw) + CommentEnd, BoxedTextColor.Comment);
    }

    public void WriteToolTip(IMemberRef? member)
    {
        switch (member)
        {
            case null:
                WriteError();
                return;
            case IMethod { MethodSig: { } } method:
                WriteToolTip(method);
                return;
            case IField { FieldSig: { } } field:
                WriteToolTip(field);
                return;
            case PropertyDef { PropertySig: { } } prop:
                WriteToolTip(prop);
                return;
            case EventDef { EventType: { } } evt:
                WriteToolTip(evt);
                return;
            case ITypeDefOrRef tdr:
                WriteToolTip(tdr);
                return;
            case GenericParam gp:
                WriteToolTip(gp);
                return;
            default:
                Debug.Fail("Unknown reference");
                break;
        }
    }

    public void Write(IMemberRef? member)
    {
        switch (member)
        {
            case null:
                WriteError();
                return;
            case IMethod { MethodSig: { } } method:
                Write(method);
                return;
            case IField { FieldSig: { } } field:
                Write(field);
                return;
            case PropertyDef { PropertySig: { } } prop:
                Write(prop);
                return;
            case EventDef { EventType: { } } evt:
                Write(evt);
                return;
            case ITypeDefOrRef tdr:
                Write(tdr, ShowModuleNames);
                return;
            case GenericParam gp:
                Write(gp);
                return;
            default:
                Debug.Fail("Unknown reference");
                break;
        }
    }

    private void WriteDeprecated(bool isDeprecated)
    {
        if (!isDeprecated) return;

        OutputWrite(DeprecatedParenOpen, BoxedTextColor.Punctuation);
        OutputWrite(dnSpy_Decompiler_Resources.CSharp_Deprecated_Member, BoxedTextColor.Text);
        OutputWrite(DeprecatedParenClose, BoxedTextColor.Punctuation);
        WriteSpace();
    }

    private void Write(MemberSpecialFlags flags)
    {
        if (flags == MemberSpecialFlags.None)
            return;

        OutputWrite(MemberSpecialParenOpen, BoxedTextColor.Punctuation);
        var comma = false;

        if ((flags & MemberSpecialFlags.Awaitable) != 0)
        {
            comma = true;
            OutputWrite(dnSpy_Decompiler_Resources.CSharp_Awaitable_Method, BoxedTextColor.Text);
        }

        if ((flags & MemberSpecialFlags.Extension) != 0)
        {
            if (comma)
                WriteCommaSpace();

            OutputWrite(dnSpy_Decompiler_Resources.CSharp_Extension_Method, BoxedTextColor.Text);
        }

        OutputWrite(MemberSpecialParenClose, BoxedTextColor.Punctuation);
        WriteSpace();
    }

    private void WriteToolTip(IMethod? method)
    {
        if (method is null)
        {
            WriteError();
            return;
        }

        WriteDeprecated(TypeFormatterUtils.IsDeprecated(method));
        Write(TypeFormatterUtils.GetMemberSpecialFlags(method));
        Write(method);

        var td = method.DeclaringType.ResolveTypeDef();

        if (td is not null)
        {
            var s = TypeFormatterUtils.GetNumberOfOverloadsString(td, method);
            if (s is not null)
                OutputWrite(s, BoxedTextColor.Text);
        }
    }

    private void WriteType(ITypeDefOrRef type, bool useNamespaces, bool useTypeKeywords)
    {
        var td = type as TypeDef;

        if (td is null && type is TypeRef typeRef)
            td = typeRef.Resolve();

        if (td is null ||
            td.GenericParameters.Count == 0 ||
            (td.DeclaringType is not null && td.DeclaringType.GenericParameters.Count >= td.GenericParameters.Count))
        {
            var oldFlags = _options;
            _options &= ~(FormatterOptions.ShowNamespaces | FormatterOptions.ShowIntrinsicTypeKeywords);

            if (useNamespaces)
                _options |= FormatterOptions.ShowNamespaces;

            if (useTypeKeywords)
                _options |= FormatterOptions.ShowIntrinsicTypeKeywords;

            Write(type);
            _options = oldFlags;
            return;
        }

        var numGenParams = td.GenericParameters.Count;

        if (type.DeclaringType is not null)
        {
            var oldFlags = _options;
            _options &= ~(FormatterOptions.ShowNamespaces | FormatterOptions.ShowIntrinsicTypeKeywords);

            if (useNamespaces)
                _options |= FormatterOptions.ShowNamespaces;

            Write(type.DeclaringType);
            _options = oldFlags;
            WritePeriod();
            numGenParams -= td.DeclaringType!.GenericParameters.Count;

            if (numGenParams < 0)
                numGenParams = 0;
        }
        else if (useNamespaces && !UTF8String.IsNullOrEmpty(td.Namespace))
            foreach (var ns in td.Namespace.String.Split('.'))
            {
                WriteIdentifier(ns, BoxedTextColor.Namespace);
                WritePeriod();
            }

        WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(td.Name), CSharpMetadataTextColorProvider.Instance.GetColor(td));
        WriteToken(type);
        var genParams = td.GenericParameters.Skip(td.GenericParameters.Count - numGenParams).ToArray();
        WriteGenerics(genParams, BoxedTextColor.TypeGenericParameter);
    }

    private bool WriteRefIfByRef(TypeSig? typeSig, ParamDef? pd, bool forceReadOnly)
    {
        if (typeSig.RemovePinnedAndModifiers() is ByRefSig)
        {
            if (pd is { IsIn: false, IsOut: true })
            {
                OutputWrite(Keyword_out, BoxedTextColor.Keyword);
                WriteSpace();
            }
            else if (pd is { IsIn: true, IsOut: false } && TypeFormatterUtils.IsReadOnlyParameter(pd))
            {
                OutputWrite(Keyword_in, BoxedTextColor.Keyword);
                WriteSpace();
            }
            else
            {
                OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
                WriteSpace();

                if (forceReadOnly)
                {
                    OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
                    WriteSpace();
                }
            }

            return true;
        }

        return false;
    }

    private void WriteAccessor(AccessorKind kind)
    {
        string keyword;

        switch (kind)
        {
            case AccessorKind.None:
            default:
                throw new InvalidOperationException();
            case AccessorKind.Getter:
                keyword = Keyword_get;
                break;
            case AccessorKind.Setter:
                keyword = Keyword_set;
                break;
            case AccessorKind.Adder:
                keyword = Keyword_add;
                break;
            case AccessorKind.Remover:
                keyword = Keyword_remove;
                break;
        }

        OutputWrite(".", BoxedTextColor.Operator);
        OutputWrite(keyword, BoxedTextColor.Keyword);
    }

    private void Write(IMethod? method)
    {
        if (method is null)
        {
            WriteError();
            return;
        }

        var propInfo = TypeFormatterUtils.TryGetProperty(method as MethodDef);

        if (propInfo.kind != AccessorKind.None)
        {
            Write(propInfo.property, writeAccessors: false);
            WriteAccessor(propInfo.kind);
            return;
        }

        var eventInfo = TypeFormatterUtils.TryGetEvent(method as MethodDef);

        if (eventInfo.kind != AccessorKind.None)
        {
            Write(eventInfo.@event, writeAccessors: false);
            WriteAccessor(eventInfo.kind);
            return;
        }

        var info = new FormatterMethodInfo(method);
        WriteModuleName(info);

        string[]? operatorInfo;

        if (info.MethodDef is not null && info.MethodDef.IsConstructor && method.DeclaringType is not null)
            operatorInfo = null;
        else if (info.MethodDef is not null && info.MethodDef.Overrides.Count > 0)
        {
            var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
            operatorInfo = TryGetOperatorInfo(ovrMeth.Name);
        }
        else
            operatorInfo = TryGetOperatorInfo(method.Name);

        var isExplicitOrImplicit = operatorInfo is not null && (operatorInfo[0] == "explicit" || operatorInfo[0] == "implicit");

        if (!isExplicitOrImplicit)
            WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));

        if (ShowDeclaringTypes)
        {
            Write(method.DeclaringType);
            WritePeriod();
        }

        if (info.MethodDef is not null && info.MethodDef.IsConstructor && method.DeclaringType is not null)
            WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(method.DeclaringType.Name), CSharpMetadataTextColorProvider.Instance.GetColor(method));
        else if (info.MethodDef is not null && info.MethodDef.Overrides.Count > 0)
        {
            var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
            WriteMethodName(method, ovrMeth.Name, operatorInfo);
        }
        else
            WriteMethodName(method, method.Name, operatorInfo);

        if (isExplicitOrImplicit)
        {
            WriteToken(method);
            WriteSpace();
            ForceWriteReturnType(info, writeSpace: false, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));
        }
        else
            WriteToken(method);

        WriteGenericArguments(info);
        WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
    }

    private static string[]? TryGetOperatorInfo(string name)
    {
        NameToOperatorName.TryGetValue(name, out var list);
        return list;
    }

    private void WriteOperatorInfoString(string s) => OutputWrite(s, 'a' <= s[0] && s[0] <= 'z' ? BoxedTextColor.Keyword : BoxedTextColor.Operator);

    private void WriteMethodName(IMethod method, string name, string[]? operatorInfo)
    {
        if (operatorInfo is not null)
            for (var i = 0; i < operatorInfo.Length; i++)
            {
                if (i > 0)
                    WriteSpace();

                var s = operatorInfo[i];
                WriteOperatorInfoString(s);
            }
        else
            WriteIdentifier(name, CSharpMetadataTextColorProvider.Instance.GetColor(method));
    }

    private void WriteToolTip(IField field)
    {
        WriteDeprecated(TypeFormatterUtils.IsDeprecated(field));
        Write(field, true);
    }

    private void Write(IField field) => Write(field, false);

    private void Write(IField? field, bool isToolTip)
    {
        if (field is null)
        {
            WriteError();
            return;
        }

        var sig = field.FieldSig;
        var td = field.DeclaringType.ResolveTypeDef();
        var isEnumOwner = td is not null && td.IsEnum;

        var fd = field.ResolveFieldDef();
        object? constant = null;
        var isConstant = fd is not null &&
                         (fd.IsLiteral || fd is { IsStatic: true, IsInitOnly: true }) &&
                         TypeFormatterUtils.HasConstant(fd, out var constantAttribute) &&
                         TypeFormatterUtils.TryGetConstant(fd, constantAttribute, out constant);

        if (!isEnumOwner || (fd is not null && !fd.IsLiteral))
        {
            if (isToolTip)
            {
                OutputWrite(DescriptionParenOpen, BoxedTextColor.Punctuation);
                OutputWrite(isConstant ? dnSpy_Decompiler_Resources.ToolTip_Constant : dnSpy_Decompiler_Resources.ToolTip_Field, BoxedTextColor.Text);
                OutputWrite(DescriptionParenClose, BoxedTextColor.Punctuation);
                WriteSpace();
            }

            WriteModuleName(fd?.Module);
            Write(sig.Type, null, null, null, attributeProvider: fd);
            WriteSpace();
        }
        else
            WriteModuleName(fd?.Module);

        if (ShowDeclaringTypes)
        {
            Write(field.DeclaringType);
            WritePeriod();
        }

        WriteIdentifier(field.Name, CSharpMetadataTextColorProvider.Instance.GetColor(field));
        WriteToken(field);

        if (ShowFieldLiteralValues && isConstant)
        {
            WriteSpace();
            OutputWrite("=", BoxedTextColor.Operator);
            WriteSpace();
            WriteConstant(constant);
        }
    }

    private void WriteConstant(object? obj)
    {
        if (obj is null)
        {
            OutputWrite(Keyword_null, BoxedTextColor.Keyword);
            return;
        }

        switch (Type.GetTypeCode(obj.GetType()))
        {
            case TypeCode.Boolean:
                FormatBoolean((bool)obj);
                break;

            case TypeCode.Char:
                FormatChar((char)obj);
                break;

            case TypeCode.SByte:
                FormatSByte((sbyte)obj);
                break;

            case TypeCode.Byte:
                FormatByte((byte)obj);
                break;

            case TypeCode.Int16:
                FormatInt16((short)obj);
                break;

            case TypeCode.UInt16:
                FormatUInt16((ushort)obj);
                break;

            case TypeCode.Int32:
                FormatInt32((int)obj);
                break;

            case TypeCode.UInt32:
                FormatUInt32((uint)obj);
                break;

            case TypeCode.Int64:
                FormatInt64((long)obj);
                break;

            case TypeCode.UInt64:
                FormatUInt64((ulong)obj);
                break;

            case TypeCode.Single:
                FormatSingle((float)obj);
                break;

            case TypeCode.Double:
                FormatDouble((double)obj);
                break;

            case TypeCode.Decimal:
                FormatDecimal((decimal)obj);
                break;

            case TypeCode.String:
                FormatString((string)obj);
                break;

            default:
                Debug.Fail($"Unknown constant: '{obj}'");
                OutputWrite(obj.ToString() ?? "???", BoxedTextColor.Text);
                break;
        }
    }

    private void WriteToolTip(PropertyDef prop)
    {
        WriteDeprecated(TypeFormatterUtils.IsDeprecated(prop));
        Write(prop);
    }

    private void Write(PropertyDef? prop) => Write(prop, writeAccessors: true);

    private void Write(PropertyDef? prop, bool writeAccessors)
    {
        if (prop is null)
        {
            WriteError();
            return;
        }

        var getMethod = prop.GetMethods.FirstOrDefault();
        var setMethod = prop.SetMethods.FirstOrDefault();
        var md = getMethod ?? setMethod;

        if (md is null)
        {
            WriteError();
            return;
        }

        var info = new FormatterMethodInfo(md, md == setMethod);
        WriteModuleName(info);
        WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyProperty(prop));

        if (ShowDeclaringTypes)
        {
            Write(prop.DeclaringType);
            WritePeriod();
        }

        var ovrMeth = md.Overrides.Count == 0 ? null : md.Overrides[0].MethodDeclaration;

        if (prop.IsIndexer())
        {
            OutputWrite(Keyword_this, BoxedTextColor.Keyword);
            WriteGenericArguments(info);
            WriteMethodParameterList(info, IndexerParenOpen, IndexerParenClose);
        }
        else if (ovrMeth is not null && TypeFormatterUtils.GetPropertyName(ovrMeth) is { } ovrMethPropName)
            WriteIdentifier(ovrMethPropName, CSharpMetadataTextColorProvider.Instance.GetColor(prop));
        else
            WriteIdentifier(prop.Name, CSharpMetadataTextColorProvider.Instance.GetColor(prop));

        WriteToken(prop);

        if (writeAccessors)
        {
            WriteSpace();
            OutputWrite("{", BoxedTextColor.Punctuation);

            if (prop.GetMethods.Count > 0)
            {
                WriteSpace();
                OutputWrite(Keyword_get, BoxedTextColor.Keyword);
                OutputWrite(";", BoxedTextColor.Punctuation);
            }

            if (prop.SetMethods.Count > 0)
            {
                WriteSpace();
                OutputWrite(Keyword_set, BoxedTextColor.Keyword);
                OutputWrite(";", BoxedTextColor.Punctuation);
            }

            WriteSpace();
            OutputWrite("}", BoxedTextColor.Punctuation);
        }
    }

    private void WriteToolTip(EventDef? evt)
    {
        WriteDeprecated(TypeFormatterUtils.IsDeprecated(evt));
        Write(evt);
    }

    private void Write(EventDef? evt) => Write(evt, writeAccessors: true);

    private void Write(EventDef? evt, bool writeAccessors)
    {
        if (evt is null)
        {
            WriteError();
            return;
        }

        WriteModuleName(evt.Module);
        Write(evt.EventType);
        WriteSpace();

        if (ShowDeclaringTypes)
        {
            Write(evt.DeclaringType);
            WritePeriod();
        }

        WriteIdentifier(evt.Name, CSharpMetadataTextColorProvider.Instance.GetColor(evt));
        WriteToken(evt);

        if (writeAccessors)
        {
            WriteSpace();
            OutputWrite("{", BoxedTextColor.Punctuation);

            if (evt.AddMethod is not null)
            {
                WriteSpace();
                OutputWrite(Keyword_add, BoxedTextColor.Keyword);
                OutputWrite(";", BoxedTextColor.Punctuation);
            }

            if (evt.RemoveMethod is not null)
            {
                WriteSpace();
                OutputWrite(Keyword_remove, BoxedTextColor.Keyword);
                OutputWrite(";", BoxedTextColor.Punctuation);
            }

            WriteSpace();
            OutputWrite("}", BoxedTextColor.Punctuation);
        }
    }

    private void WriteToolTip(GenericParam? gp)
    {
        if (gp is null)
        {
            WriteError();
            return;
        }

        Write(gp);
        WriteSpace();
        OutputWrite(dnSpy_Decompiler_Resources.ToolTip_GenericParameterInTypeOrMethod, BoxedTextColor.Text);
        WriteSpace();

        if (gp.Owner is TypeDef td)
            WriteType(td, ShowNamespaces, ShowIntrinsicTypeKeywords);
        else
            Write(gp.Owner as MethodDef);
    }

    private void Write(GenericParam? gp)
    {
        if (gp is null)
        {
            WriteError();
            return;
        }

        WriteIdentifier(gp.Name, CSharpMetadataTextColorProvider.Instance.GetColor(gp));
        WriteToken(gp);
    }

    private void WriteToolTip(ITypeDefOrRef type)
    {
        var td = type.ResolveTypeDef();

        WriteDeprecated(TypeFormatterUtils.IsDeprecated(type));
        Write(TypeFormatterUtils.GetMemberSpecialFlags(type));

        MethodDef invoke;

        if (TypeFormatterUtils.IsDelegate(td) && (invoke = td.FindMethod("Invoke")) is not null && invoke.MethodSig is not null)
        {
            OutputWrite(Keyword_delegate, BoxedTextColor.Keyword);
            WriteSpace();

            var info = new FormatterMethodInfo(invoke);
            WriteModuleName(info);
            WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));

            // Always print the namespace here because that's what VS does
            WriteType(td, true, ShowIntrinsicTypeKeywords);

            WriteGenericArguments(info);
            WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
            return;
        }

        WriteModuleName(td?.Module);

        if (td is null)
        {
            Write(type);
            return;
        }

        string keyword;

        if (td.IsEnum)
            keyword = Keyword_enum;
        else if (td.IsValueType)
        {
            if (TypeFormatterUtils.IsReadOnlyType(td))
            {
                OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
                WriteSpace();
            }

            if (TypeFormatterUtils.IsByRefLike(td))
            {
                OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
                WriteSpace();
            }

            keyword = Keyword_struct;
        }
        else if (td.IsInterface)
            keyword = Keyword_interface;
        else
            keyword = Keyword_class;

        OutputWrite(keyword, BoxedTextColor.Keyword);
        WriteSpace();

        // Always print the namespace here because that's what VS does
        WriteType(type, true, false);
    }

    private void Write(ITypeDefOrRef? type, bool showModuleNames = false)
    {
        if (type is null)
        {
            WriteError();
            return;
        }

        if (_recursionCounter >= TypeFormatterUtils.MaxRecursion)
            return;

        _recursionCounter++;

        try
        {
            if (type is TypeSpec ts)
            {
                Write(ts.TypeSig, null, null, null);
                return;
            }

            if (type.DeclaringType is not null)
            {
                Write(type.DeclaringType);
                WritePeriod();
            }

            var keyword = GetTypeKeyword(type);

            if (keyword is not null)
                OutputWrite(keyword, BoxedTextColor.Keyword);
            else
            {
                if (showModuleNames)
                    WriteModuleName(type.ResolveTypeDef()?.Module);
                WriteNamespace(type.Namespace);
                WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(type.Name), CSharpMetadataTextColorProvider.Instance.GetColor(type));
            }

            WriteToken(type);
        }
        finally
        {
            _recursionCounter--;
        }
    }

    private void WriteNamespace(string ns)
    {
        if (!ShowNamespaces || string.IsNullOrEmpty(ns))
            return;

        var namespaces = ns.Split(NamespaceSeparators);

        foreach (var t in namespaces)
        {
            OutputWrite(IdentifierEscaper.Escape(t), BoxedTextColor.Namespace);
            WritePeriod();
        }
    }

    private string? GetTypeKeyword(ITypeDefOrRef? type)
    {
        if (!ShowIntrinsicTypeKeywords ||
            type is null ||
            type.DeclaringType is not null ||
            type.Namespace != "System" ||
            !type.DefinitionAssembly.IsCorLib())
            return null;

        switch (type.TypeName)
        {
            case "Void": return "void";
            case "Boolean": return "bool";
            case "Byte": return "byte";
            case "Char": return "char";
            case "Decimal": return "decimal";
            case "Double": return "double";
            case "Int16": return "short";
            case "Int32": return "int";
            case "Int64": return "long";
            case "Object": return "object";
            case "SByte": return "sbyte";
            case "Single": return "float";
            case "String": return "string";
            case "UInt16": return "ushort";
            case "UInt32": return "uint";
            case "UInt64": return "ulong";
            default: return null;
        }
    }

    private void Write(TypeSig? type, ParamDef? ownerParam, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methGenArgs, bool forceReadOnly = false,
        IHasCustomAttribute? attributeProvider = null)
    {
        WriteRefIfByRef(type, ownerParam, forceReadOnly);
        var dynamicTypeIndex = 0;

        if (type.RemovePinnedAndModifiers() is ByRefSig byRef)
        {
            type = byRef.Next;
            dynamicTypeIndex++;
        }

        Write(type, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
    }

    private void Write(TypeSig? type, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methGenArgs, ref int dynamicTypeIndex,
        IHasCustomAttribute? attributeProvider)
    {
        if (type is null)
        {
            WriteError();
            return;
        }

        if (_recursionCounter >= TypeFormatterUtils.MaxRecursion)
            return;

        _recursionCounter++;

        try
        {
            typeGenArgs ??= Array.Empty<TypeSig>();
            methGenArgs ??= Array.Empty<TypeSig>();

            List<ArraySigBase>? list = null;

            while (type is not null && (type.ElementType == ElementType.SZArray || type.ElementType == ElementType.Array))
            {
                list ??= new List<ArraySigBase>();
                list.Add((ArraySigBase)type);
                type = type.Next;
            }

            if (list is not null)
            {
                dynamicTypeIndex++;
                Write(list[^1].Next, typeGenArgs, Array.Empty<TypeSig>(), ref dynamicTypeIndex, attributeProvider);

                foreach (var aryType in list)
                {
                    if (aryType.ElementType == ElementType.Array)
                    {
                        OutputWrite(ArrayParenOpen, BoxedTextColor.Punctuation);
                        var rank = aryType.Rank;

                        if (rank == 0)
                            OutputWrite("<RANK0>", BoxedTextColor.Error);
                        else
                        {
                            var indexes = aryType.GetLowerBounds();
                            var dims = aryType.GetSizes();

                            if (ShowArrayValueSizes && (uint)indexes.Count == rank && (uint)dims.Count == rank)
                                for (var i = 0; (uint)i < rank; i++)
                                {
                                    if (i > 0)
                                        WriteCommaSpace();

                                    if (i < indexes.Count && indexes[i] == 0)
                                        FormatInt32((int)dims[i]);
                                    else if (i < indexes.Count && i < dims.Count)
                                    {
                                        FormatInt32(indexes[i]);
                                        OutputWrite("..", BoxedTextColor.Operator);
                                        FormatInt32((int)(indexes[i] + dims[i] - 1));
                                    }
                                }
                            else
                            {
                                if (rank == 1)
                                    OutputWrite("*", BoxedTextColor.Operator);

                                for (uint i = 1; i < rank; i++)
                                    OutputWrite(",", BoxedTextColor.Punctuation);
                            }
                        }

                        OutputWrite(ArrayParenClose, BoxedTextColor.Punctuation);
                    }
                    else
                    {
                        Debug.Assert(aryType.ElementType == ElementType.SZArray);
                        OutputWrite(ArrayParenOpen, BoxedTextColor.Punctuation);
                        OutputWrite(ArrayParenClose, BoxedTextColor.Punctuation);
                    }
                }

                return;
            }

            if (type is null)
                return;

            switch (type.ElementType)
            {
                case ElementType.Void:
                    WriteSystemTypeKeyword("Void", "void", true);
                    break;
                case ElementType.Boolean:
                    WriteSystemTypeKeyword("Boolean", "bool", true);
                    break;
                case ElementType.Char:
                    WriteSystemTypeKeyword("Char", "char", true);
                    break;
                case ElementType.I1:
                    WriteSystemTypeKeyword("SByte", "sbyte", true);
                    break;
                case ElementType.U1:
                    WriteSystemTypeKeyword("Byte", "byte", true);
                    break;
                case ElementType.I2:
                    WriteSystemTypeKeyword("Int16", "short", true);
                    break;
                case ElementType.U2:
                    WriteSystemTypeKeyword("UInt16", "ushort", true);
                    break;
                case ElementType.I4:
                    WriteSystemTypeKeyword("Int32", "int", true);
                    break;
                case ElementType.U4:
                    WriteSystemTypeKeyword("UInt32", "uint", true);
                    break;
                case ElementType.I8:
                    WriteSystemTypeKeyword("Int64", "long", true);
                    break;
                case ElementType.U8:
                    WriteSystemTypeKeyword("UInt64", "ulong", true);
                    break;
                case ElementType.R4:
                    WriteSystemTypeKeyword("Single", "float", true);
                    break;
                case ElementType.R8:
                    WriteSystemTypeKeyword("Double", "double", true);
                    break;
                case ElementType.String:
                    WriteSystemTypeKeyword("String", "string", false);
                    break;
                case ElementType.Object:
                    if (TypeFormatterUtils.HasDynamicAttribute(attributeProvider, dynamicTypeIndex))
                        OutputWrite("dynamic", BoxedTextColor.Keyword);
                    else
                        WriteSystemTypeKeyword("Object", "object", false);
                    break;

                case ElementType.TypedByRef:
                    WriteSystemType("TypedReference", true);
                    break;
                case ElementType.I:
                    WriteSystemType("IntPtr", true);
                    break;
                case ElementType.U:
                    WriteSystemType("UIntPtr", true);
                    break;

                case ElementType.Ptr:
                    dynamicTypeIndex++;
                    Write(type.Next, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    OutputWrite("*", BoxedTextColor.Operator);
                    break;

                case ElementType.ByRef:
                    dynamicTypeIndex++;
                    Write(type.Next, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    OutputWrite("&", BoxedTextColor.Operator);
                    break;

                case ElementType.ValueType:
                case ElementType.Class:
                    var cvt = (TypeDefOrRefSig)type;
                    Write(cvt.TypeDefOrRef);
                    break;

                case ElementType.Var:
                case ElementType.MVar:
                    var gsType = Read(type.ElementType == ElementType.Var ? typeGenArgs : methGenArgs, (int)((GenericSig)type).Number);

                    if (gsType is not null)
                        Write(gsType, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    else
                    {
                        var gp = ((GenericSig)type).GenericParam;

                        if (gp is not null)
                            Write(gp);
                        else
                        {
                            if (type.ElementType == ElementType.MVar)
                            {
                                OutputWrite("!!", BoxedTextColor.MethodGenericParameter);
                                OutputWrite(((GenericSig)type).Number.ToString(), BoxedTextColor.MethodGenericParameter);
                            }
                            else
                            {
                                OutputWrite("!", BoxedTextColor.TypeGenericParameter);
                                OutputWrite(((GenericSig)type).Number.ToString(), BoxedTextColor.TypeGenericParameter);
                            }
                        }
                    }

                    break;

                case ElementType.GenericInst:
                    var gis = (GenericInstSig?)type;
                    Debug2.Assert(gis is not null);

                    if (TypeFormatterUtils.IsSystemNullable(gis))
                    {
                        dynamicTypeIndex++;
                        Write(GenericArgumentResolver.Resolve(gis.GenericArguments[0], typeGenArgs, methGenArgs), null, null, ref dynamicTypeIndex,
                            attributeProvider);
                        OutputWrite("?", BoxedTextColor.Operator);
                    }
                    else if (TypeFormatterUtils.IsSystemValueTuple(gis))
                    {
                        OutputWrite(TupleParenOpen, BoxedTextColor.Punctuation);
                        var needComma = false;

                        for (var i = 0; i < 1000; i++)
                        {
                            for (var j = 0; j < gis.GenericArguments.Count && j < 7; j++)
                            {
                                if (needComma)
                                    WriteCommaSpace();

                                needComma = true;
                                dynamicTypeIndex++;
                                Write(GenericArgumentResolver.Resolve(gis.GenericArguments[j], typeGenArgs, methGenArgs), null, null,
                                    ref dynamicTypeIndex, attributeProvider);
                            }

                            if (gis.GenericArguments.Count != 8)
                                break;

                            gis = gis.GenericArguments[^1] as GenericInstSig;
                            dynamicTypeIndex++;

                            if (gis is null)
                            {
                                WriteError();
                                break;
                            }
                        }

                        OutputWrite(TupleParenClose, BoxedTextColor.Punctuation);
                    }
                    else
                    {
                        Write(gis.GenericType, null, null, ref dynamicTypeIndex, attributeProvider);
                        OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);

                        for (var i = 0; i < gis.GenericArguments.Count; i++)
                        {
                            if (i > 0)
                                WriteCommaSpace();

                            dynamicTypeIndex++;
                            Write(GenericArgumentResolver.Resolve(gis.GenericArguments[i], typeGenArgs, methGenArgs), null, null, ref dynamicTypeIndex,
                                attributeProvider);
                        }

                        OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
                    }

                    break;

                case ElementType.FnPtr:
                    var sig = ((FnPtrSig)type).MethodSig;

                    dynamicTypeIndex++;
                    _sb.Clear();
                    if (sig.RetType is CModReqdSig retModifier &&
                        FullNameFactory.FullName(retModifier.Modifier, false, null, _sb) == "System.Runtime.InteropServices.InAttribute")
                        dynamicTypeIndex++;

                    Write(sig.RetType, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    WriteSpace();
                    OutputWrite(MethodParenOpen, BoxedTextColor.Punctuation);

                    for (var i = 0; i < sig.Params.Count; i++)
                    {
                        if (i > 0)
                            WriteCommaSpace();

                        var paramType = sig.Params[i];

                        if (paramType is CModReqdSig modReqdSig)
                        {
                            _sb.Clear();
                            var modifierFullName = FullNameFactory.FullName(modReqdSig.Modifier, false, null, _sb);
                            if (modifierFullName == "System.Runtime.InteropServices.InAttribute")
                                dynamicTypeIndex++;
                            else if (modifierFullName == "System.Runtime.InteropServices.OutAttribute")
                                dynamicTypeIndex++;
                        }

                        dynamicTypeIndex++;
                        Write(sig.Params[i], typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    }

                    if (sig.ParamsAfterSentinel is not null)
                    {
                        if (sig.Params.Count > 0)
                            WriteCommaSpace();
                        OutputWrite("...", BoxedTextColor.Punctuation);

                        foreach (var t in sig.ParamsAfterSentinel)
                        {
                            WriteCommaSpace();
                            Write(t, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                        }
                    }

                    OutputWrite(MethodParenClose, BoxedTextColor.Punctuation);
                    break;

                case ElementType.CModReqd:
                case ElementType.CModOpt:
                case ElementType.Pinned:
                    Write(type.Next, typeGenArgs, methGenArgs, ref dynamicTypeIndex, attributeProvider);
                    break;

                case ElementType.End:
                case ElementType.Array: // handled above
                case ElementType.ValueArray:
                case ElementType.R:
                case ElementType.SZArray: // handled above
                case ElementType.Internal:
                case ElementType.Module:
                case ElementType.Sentinel:
                default:
                    break;
            }
        }
        finally
        {
            _recursionCounter--;
        }
    }

    private static TypeSig? Read(IList<TypeSig> list, int index) => (uint)index < (uint)list.Count ? list[index] : null;

    public void WriteToolTip(ISourceVariable? variable)
    {
        if (variable is null)
        {
            WriteError();
            return;
        }

        var isLocal = variable.IsLocal;
        var pd = (variable.Variable as Parameter)?.ParamDef;
        OutputWrite(DescriptionParenOpen, BoxedTextColor.Punctuation);
        OutputWrite(isLocal ? dnSpy_Decompiler_Resources.ToolTip_Local : dnSpy_Decompiler_Resources.ToolTip_Parameter, BoxedTextColor.Text);
        OutputWrite(DescriptionParenClose, BoxedTextColor.Punctuation);
        WriteSpace();
        Write(variable.Type, !isLocal ? ((Parameter)variable.Variable!).ParamDef : null, null, null,
            forceReadOnly: (variable.Flags & SourceVariableFlags.ReadOnlyReference) != 0, attributeProvider: pd);
        WriteSpace();
        WriteIdentifier(TypeFormatterUtils.GetName(variable), isLocal ? BoxedTextColor.Local : BoxedTextColor.Parameter);

        if (pd is not null)
            WriteToken(pd);
    }

    public void WriteNamespaceToolTip(string? @namespace)
    {
        if (@namespace is null)
        {
            WriteError();
            return;
        }

        OutputWrite(Keyword_namespace, BoxedTextColor.Keyword);
        WriteSpace();
        var parts = @namespace.Split(NamespaceSeparators);

        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                OutputWrite(".", BoxedTextColor.Operator);

            OutputWrite(IdentifierEscaper.Escape(parts[i]), BoxedTextColor.Namespace);
        }
    }

    private void Write(ModuleDef? module)
    {
        try
        {
            if (_recursionCounter++ >= TypeFormatterUtils.MaxRecursion)
                return;

            if (module is null)
            {
                OutputWrite("null module", BoxedTextColor.Error);
                return;
            }

            var name = TypeFormatterUtils.GetFileName(module.Location);
            OutputWrite(TypeFormatterUtils.FilterName(name), BoxedTextColor.AssemblyModule);
        }
        finally
        {
            _recursionCounter--;
        }
    }

    private void WriteModuleName(in FormatterMethodInfo info)
    {
        if (!ShowModuleNames)
            return;

        Write(info.ModuleDef);
        OutputWrite(ModuleNameSeparator, BoxedTextColor.Operator);
        return;
    }

    private void WriteModuleName(ModuleDef? module)
    {
        if (module is null)
            return;
        if (!ShowModuleNames)
            return;

        Write(module);
        OutputWrite(ModuleNameSeparator, BoxedTextColor.Operator);
        return;
    }

    private void WriteReturnType(in FormatterMethodInfo info, bool writeSpace, bool isReadOnly)
    {
        if (!ShowReturnTypes)
            return;
        if (info.MethodDef?.IsConstructor == true)
            return;

        ForceWriteReturnType(info, writeSpace, isReadOnly);
    }

    private void ForceWriteReturnType(in FormatterMethodInfo info, bool writeSpace, bool isReadOnly)
    {
        if (info.MethodDef is not null && info.MethodDef.IsConstructor) return;

        TypeSig? retType;
        ParamDef? retParamDef;

        if (info.RetTypeIsLastArgType)
        {
            retType = info.MethodSig.Params.LastOrDefault();

            if (info.MethodDef is null)
                retParamDef = null;
            else
            {
                var l = info.MethodDef.Parameters.LastOrDefault();
                retParamDef = l?.ParamDef;
            }
        }
        else
        {
            retType = info.MethodSig.RetType;
            retParamDef = info.MethodDef?.Parameters.ReturnParameter.ParamDef;
        }

        if (retType.RemovePinnedAndModifiers() is ByRefSig && isReadOnly)
        {
            retType = retType.RemovePinnedAndModifiers().Next;
            OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
            WriteSpace();
            OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
            WriteSpace();
        }

        Write(retType, retParamDef, info.TypeGenericParams, info.MethodGenericParams, attributeProvider: retParamDef);
        if (writeSpace)
            WriteSpace();
    }

    private void WriteGenericArguments(in FormatterMethodInfo info)
    {
        if (info.MethodSig.GenParamCount == 0) return;

        if (info.MethodGenericParams is not null)
            WriteGenerics(info.MethodGenericParams);
        else if (info.MethodDef is not null)
            WriteGenerics(info.MethodDef.GenericParameters, BoxedTextColor.MethodGenericParameter);
    }

    private void WriteMethodParameterList(in FormatterMethodInfo info, string lparen, string rparen)
    {
        if (!ShowParameterTypes && !ShowParameterNames)
            return;

        OutputWrite(lparen, BoxedTextColor.Punctuation);
        var baseIndex = info.MethodSig.HasThis ? 1 : 0;
        var count = info.MethodSig.Params.Count;

        if (info.RetTypeIsLastArgType)
            count--;

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                WriteCommaSpace();
            ParamDef? pd;
            if (info.MethodDef is not null && baseIndex + i < info.MethodDef.Parameters.Count)
                pd = info.MethodDef.Parameters[baseIndex + i].ParamDef;
            else
                pd = null;

            var isDefault = TypeFormatterUtils.HasConstant(pd, out var constantAttribute);

            if (isDefault)
                OutputWrite(DefaultParamValueParenOpen, BoxedTextColor.Punctuation);

            var needSpace = false;

            if (ShowParameterTypes)
            {
                needSpace = true;

                if (pd is not null && pd.CustomAttributes.IsDefined("System.ParamArrayAttribute"))
                {
                    OutputWrite(Keyword_params, BoxedTextColor.Keyword);
                    WriteSpace();
                }

                var paramType = info.MethodSig.Params[i];
                Write(paramType, pd, info.TypeGenericParams, info.MethodGenericParams, attributeProvider: pd);
            }

            if (ShowParameterNames)
            {
                if (needSpace)
                    WriteSpace();
                needSpace = true;

                if (pd is not null)
                {
                    WriteIdentifier(pd.Name, BoxedTextColor.Parameter);
                    WriteToken(pd);
                }
                else
                    WriteIdentifier("A_" + (baseIndex + i).ToString(), BoxedTextColor.Parameter);
            }

            if (ShowParameterLiteralValues && isDefault && TypeFormatterUtils.TryGetConstant(pd, constantAttribute, out var constant))
            {
                if (needSpace)
                    WriteSpace();
                needSpace = true;

                WriteSpace();
                OutputWrite("=", BoxedTextColor.Operator);
                WriteSpace();

                var t = info.MethodSig.Params[i].RemovePinnedAndModifiers();
                if (t.GetElementType() == ElementType.ByRef)
                    t = t.Next;
                if (constant is null && t is not null && t.IsValueType)
                    OutputWrite(Keyword_default, BoxedTextColor.Keyword);
                else
                    WriteConstant(constant);
            }

            if (isDefault)
                OutputWrite(DefaultParamValueParenClose, BoxedTextColor.Punctuation);
        }

        if (info.MethodSig.IsVarArg)
        {
            if (count > 0)
                WriteCommaSpace();
            OutputWrite(Keyword_arglist, BoxedTextColor.Keyword);
        }

        OutputWrite(rparen, BoxedTextColor.Punctuation);
    }

    private void WriteGenerics(IList<GenericParam>? gps, object gpTokenType)
    {
        if (gps is null || gps.Count == 0)
            return;

        OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);

        for (var i = 0; i < gps.Count; i++)
        {
            if (i > 0)
                WriteCommaSpace();

            var gp = gps[i];

            if (gp.IsCovariant)
            {
                OutputWrite(Keyword_out, BoxedTextColor.Keyword);
                WriteSpace();
            }
            else if (gp.IsContravariant)
            {
                OutputWrite(Keyword_in, BoxedTextColor.Keyword);
                WriteSpace();
            }

            WriteIdentifier(gp.Name, gpTokenType);
            WriteToken(gp);
        }

        OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
    }

    private void WriteGenerics(IList<TypeSig>? gps)
    {
        if (gps is null || gps.Count == 0)
            return;

        OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);

        for (var i = 0; i < gps.Count; i++)
        {
            if (i > 0)
                WriteCommaSpace();
            Write(gps[i], null, null, null);
        }

        OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
    }

    private void FormatBoolean(bool value)
    {
        if (value)
            OutputWrite(Keyword_true, BoxedTextColor.Keyword);
        else
            OutputWrite(Keyword_false, BoxedTextColor.Keyword);
    }

    private void FormatChar(char value) => OutputWrite(ToFormattedChar(value), BoxedTextColor.Char);

    private string ToFormattedChar(char value)
    {
        _sb.Clear();

        _sb.Append('\'');

        switch (value)
        {
            case '\a':
                _sb.Append(@"\a");
                break;
            case '\b':
                _sb.Append(@"\b");
                break;
            case '\f':
                _sb.Append(@"\f");
                break;
            case '\n':
                _sb.Append(@"\n");
                break;
            case '\r':
                _sb.Append(@"\r");
                break;
            case '\t':
                _sb.Append(@"\t");
                break;
            case '\v':
                _sb.Append(@"\v");
                break;
            case '\\':
                _sb.Append(@"\\");
                break;
            case '\0':
                _sb.Append(@"\0");
                break;
            case '\'':
                _sb.Append(@"\'");
                break;
            default:
                if (char.IsControl(value))
                {
                    _sb.Append(@"\u");
                    _sb.Append(((ushort)value).ToString("X4"));
                }
                else
                    _sb.Append(value);

                break;
        }

        _sb.Append('\'');

        return _sb.ToString();
    }

    private static bool CanUseVerbatimString(string s)
    {
        var foundBackslash = false;

        foreach (var c in s)
        {
            switch (c)
            {
                case '"':
                    break;

                case '\\':
                    foundBackslash = true;
                    break;

                case '\a':
                case '\b':
                case '\f':
                case '\n':
                case '\r':
                case '\t':
                case '\v':
                case '\0':
                // More newline chars
                case '\u0085':
                case '\u2028':
                case '\u2029':
                    return false;

                default:
                    if (char.IsControl(c))
                        return false;

                    break;
            }
        }

        return foundBackslash;
    }

    private void FormatString(string value)
    {
        var s = ToFormattedString(value, out bool isVerbatim);
        OutputWrite(s, isVerbatim ? BoxedTextColor.VerbatimString : BoxedTextColor.String);
    }

    private string ToFormattedString(string value, out bool isVerbatim)
    {
        if (CanUseVerbatimString(value))
        {
            isVerbatim = true;
            return GetFormattedVerbatimString(value);
        }

        isVerbatim = false;
        return GetFormattedString(value);
    }

    private string GetFormattedString(string value)
    {
        _sb.Clear();

        _sb.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '\a':
                    _sb.Append(@"\a");
                    break;
                case '\b':
                    _sb.Append(@"\b");
                    break;
                case '\f':
                    _sb.Append(@"\f");
                    break;
                case '\n':
                    _sb.Append(@"\n");
                    break;
                case '\r':
                    _sb.Append(@"\r");
                    break;
                case '\t':
                    _sb.Append(@"\t");
                    break;
                case '\v':
                    _sb.Append(@"\v");
                    break;
                case '\\':
                    _sb.Append(@"\\");
                    break;
                case '\0':
                    _sb.Append(@"\0");
                    break;
                case '"':
                    _sb.Append("\\\"");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        _sb.Append(@"\u");
                        _sb.Append(((ushort)c).ToString("X4"));
                    }
                    else
                        _sb.Append(c);

                    break;
            }
        }

        _sb.Append('"');

        return _sb.ToString();
    }

    private string GetFormattedVerbatimString(string value)
    {
        _sb.Clear();

        _sb.Append(VerbatimStringPrefix + "\"");

        foreach (var c in value)
        {
            if (c == '"')
                _sb.Append("\"\"");
            else
                _sb.Append(c);
        }

        _sb.Append('"');

        return _sb.ToString();
    }

    private string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, TypeFormatterUtils.DigitGroupSizeDecimal);

    private string ToFormattedHexNumber(string number) => ToFormattedNumber(HexPrefix, number, TypeFormatterUtils.DigitGroupSizeHex);

    private string ToFormattedNumber(string prefix, string number, int digitGroupSize) =>
        TypeFormatterUtils.ToFormattedNumber(DigitSeparators, prefix, number, digitGroupSize);

    private void WriteNumber(string number) => OutputWrite(number, BoxedTextColor.Number);

    private string ToFormattedSByte(sbyte value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X2"));

    private string ToFormattedByte(byte value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X2"));

    private string ToFormattedInt16(short value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X4"));

    private string ToFormattedUInt16(ushort value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X4"));

    private string ToFormattedInt32(int value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X8"));

    private string ToFormattedUInt32(uint value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X8"));

    private string ToFormattedInt64(long value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X16"));

    private string ToFormattedUInt64(ulong value) =>
        UseDecimal ? ToFormattedDecimalNumber(value.ToString(_cultureInfo)) : ToFormattedHexNumber(value.ToString("X16"));

    private void FormatSingle(float value)
    {
        if (float.IsNaN(value))
            OutputWrite(TypeFormatterUtils.NaN, BoxedTextColor.Number);
        else if (float.IsNegativeInfinity(value))
            OutputWrite(TypeFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
        else if (float.IsPositiveInfinity(value))
            OutputWrite(TypeFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
        else
            OutputWrite(value.ToString(_cultureInfo), BoxedTextColor.Number);
    }

    private void FormatDouble(double value)
    {
        if (double.IsNaN(value))
            OutputWrite(TypeFormatterUtils.NaN, BoxedTextColor.Number);
        else if (double.IsNegativeInfinity(value))
            OutputWrite(TypeFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
        else if (double.IsPositiveInfinity(value))
            OutputWrite(TypeFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
        else
            OutputWrite(value.ToString(_cultureInfo), BoxedTextColor.Number);
    }

    private void FormatSByte(sbyte value) => WriteNumber(ToFormattedSByte(value));

    private void FormatByte(byte value) => WriteNumber(ToFormattedByte(value));

    private void FormatInt16(short value) => WriteNumber(ToFormattedInt16(value));

    private void FormatUInt16(ushort value) => WriteNumber(ToFormattedUInt16(value));

    private void FormatInt32(int value) => WriteNumber(ToFormattedInt32(value));

    private void FormatUInt32(uint value) => WriteNumber(ToFormattedUInt32(value));

    private void FormatInt64(long value) => WriteNumber(ToFormattedInt64(value));

    private void FormatUInt64(ulong value) => WriteNumber(ToFormattedUInt64(value));

    private void FormatDecimal(decimal value) => OutputWrite(value.ToString(_cultureInfo), BoxedTextColor.Number);
}