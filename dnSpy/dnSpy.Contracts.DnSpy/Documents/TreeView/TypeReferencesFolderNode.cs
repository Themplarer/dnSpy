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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView;

/// <summary>
/// Type References node
/// </summary>
public abstract class TypeReferencesFolderNode : DocumentTreeNodeData
{
    protected TypeReferencesFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// TypeSpec node
/// </summary>
public abstract class TypeSpecsFolderNode : DocumentTreeNodeData
{
    protected TypeSpecsFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// Method References node
/// </summary>
public abstract class MethodReferencesFolderNode : DocumentTreeNodeData
{
    protected MethodReferencesFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// Field References node
/// </summary>
public abstract class FieldReferencesFolderNode : DocumentTreeNodeData
{
    protected FieldReferencesFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// Property References node
/// </summary>
public abstract class PropertyReferencesFolderNode : DocumentTreeNodeData
{
    protected PropertyReferencesFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// Event References node
/// </summary>
public abstract class EventReferencesFolderNode : DocumentTreeNodeData
{
    protected EventReferencesFolderNode(IDocumentTreeNodeDataContext context) : base(context)
    {
    }
}

/// <summary>
/// Type reference node
/// </summary>
public abstract class TypeReferenceNode : DocumentTreeNodeData, IMDTokenNode
{
    /// <summary>
    /// Gets the type reference
    /// </summary>
    public ITypeDefOrRef TypeRef { get; }

    IMDTokenProvider IMDTokenNode.Reference => TypeRef;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="type">Type ref or type spec</param>
    protected TypeReferenceNode(ITypeDefOrRef type, IDocumentTreeNodeDataContext context) : base(context)
    {
        Debug.Assert(type is dnlib.DotNet.TypeRef or TypeSpec);
        TypeRef = type ?? throw new ArgumentNullException(nameof(type));
    }
}

/// <summary>
/// Method reference node
/// </summary>
public abstract class MethodReferenceNode : DocumentTreeNodeData, IMDTokenNode
{
    /// <summary>
    /// Gets the method reference
    /// </summary>
    public IMethod MethodRef { get; }

    IMDTokenProvider IMDTokenNode.Reference => MethodRef;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">Method ref</param>
    protected MethodReferenceNode(IMethod method, IDocumentTreeNodeDataContext context) : base(context)
    {
        Debug.Assert(method is MemberRef { IsMethodRef: true } or MethodSpec or MethodDef);
        MethodRef = method ?? throw new ArgumentNullException(nameof(method));
    }
}

/// <summary>
/// Field reference node
/// </summary>
public abstract class FieldReferenceNode : DocumentTreeNodeData, IMDTokenNode
{
    /// <summary>
    /// Gets the field reference
    /// </summary>
    public MemberRef FieldRef { get; }

    IMDTokenProvider IMDTokenNode.Reference => FieldRef;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="field">Field ref</param>
    protected FieldReferenceNode(MemberRef field, IDocumentTreeNodeDataContext context) : base(context)
    {
        Debug.Assert(field.IsFieldRef);
        FieldRef = field ?? throw new ArgumentNullException(nameof(field));
    }
}

/// <summary>
/// Property reference node
/// </summary>
public abstract class PropertyReferenceNode : DocumentTreeNodeData, IMDTokenNode
{
    /// <summary>
    /// Gets the property reference
    /// </summary>
    public IMethod PropertyRef { get; }

    IMDTokenProvider IMDTokenNode.Reference => PropertyRef;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">Property ref</param>
    protected PropertyReferenceNode(IMethod method, IDocumentTreeNodeDataContext context) : base(context)
    {
        Debug.Assert(method is MemberRef { IsMethodRef: true } or MethodSpec or MethodDef);
        PropertyRef = method ?? throw new ArgumentNullException(nameof(method));
    }
}

/// <summary>
/// Event reference node
/// </summary>
public abstract class EventReferenceNode : DocumentTreeNodeData, IMDTokenNode
{
    /// <summary>
    /// Gets the event reference
    /// </summary>
    public IMethod EventRef { get; }

    IMDTokenProvider IMDTokenNode.Reference => EventRef;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="method">Event ref</param>
    protected EventReferenceNode(IMethod method, IDocumentTreeNodeDataContext context) : base(context)
    {
        Debug.Assert(method is MemberRef { IsMethodRef: true } or MethodSpec or MethodDef);
        EventRef = method ?? throw new ArgumentNullException(nameof(method));
    }
}