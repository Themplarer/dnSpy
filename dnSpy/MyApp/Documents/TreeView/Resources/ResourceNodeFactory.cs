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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView;
using dnSpy.Documents.TreeView.Resources;

namespace MyApp.Documents.TreeView.Resources;

internal sealed class ResourceNodeFactory : IResourceNodeFactory
{
    private readonly Lazy<IResourceNodeProvider, IResourceNodeProviderMetadata>[] _resourceNodeProviders;

    public ResourceNodeFactory(IEnumerable<Lazy<IResourceNodeProvider, IResourceNodeProviderMetadata>> resourceNodeProviders) =>
        _resourceNodeProviders = resourceNodeProviders.OrderBy(a => a.Metadata.Order).ToArray();

    public DocumentTreeNodeData Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup, IDocumentTreeNodeDataContext context)
    {
        if (module is null)
            throw new ArgumentNullException(nameof(module));

        if (resource is null)
            throw new ArgumentNullException(nameof(resource));

        if (treeNodeGroup is null)
            throw new ArgumentNullException(nameof(treeNodeGroup));

        var node = CreateNode(module, resource, treeNodeGroup, context);
        ResourceNode.AddResource(node, resource);
        return node;
    }

    public DocumentTreeNodeData Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup,
        IDocumentTreeNodeDataContext context)
    {
        if (module is null)
            throw new ArgumentNullException(nameof(module));

        if (resourceElement is null)
            throw new ArgumentNullException(nameof(resourceElement));

        if (treeNodeGroup is null)
            throw new ArgumentNullException(nameof(treeNodeGroup));

        var node = CreateNode(module, resourceElement, treeNodeGroup, context);
        ResourceElementNode.AddResourceElement(node, resourceElement);
        return node;
    }

    private DocumentTreeNodeData CreateNode(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup, IDocumentTreeNodeDataContext context)
    {
        foreach (var provider in _resourceNodeProviders)
            try
            {
                if (provider.Value.Create(module, resource, treeNodeGroup) is { } node)
                    return node;
            }
            catch
            {
                // ignored
            }

        return new UnknownResourceNodeImpl(treeNodeGroup, resource, context);
    }

    private DocumentTreeNodeData CreateNode(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup,
        IDocumentTreeNodeDataContext context)
    {
        foreach (var provider in _resourceNodeProviders)
            try
            {
                if (provider.Value.Create(module, resourceElement, treeNodeGroup) is { } node)
                    return node;
            }
            catch
            {
                // ignored
            }

        return new BuiltInResourceElementNodeImpl(treeNodeGroup, resourceElement, context);
    }
}