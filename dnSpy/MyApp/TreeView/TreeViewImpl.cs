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
using Avalonia.Controls;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;

namespace dnSpy.TreeView;

sealed class TreeViewImpl : ITreeView
{
    public ITreeNode Root => root;

    readonly TreeNodeImpl root;

    public Guid Guid { get; }

    public Control UIObject => null!;
    
    public TreeNodeData? SelectedItem => null;

    public TreeNodeData[] SelectedItems => Array.Empty<TreeNodeData>();
    // Convert(sharpTreeView.SelectedItems);

    public TreeNodeData[] TopLevelSelection => Array.Empty<TreeNodeData>();
    // Convert(sharpTreeView.GetTopLevelSelection());

    readonly ITreeViewServiceImpl treeViewService;
    readonly ITreeViewListener? treeViewListener;
    readonly object foregroundBrushResourceKey;

    public event EventHandler<TreeViewSelectionChangedEventArgs>? SelectionChanged;

    public event EventHandler<TreeViewNodeRemovedEventArgs>? NodeRemoved;

    public TreeViewImpl(ITreeViewServiceImpl treeViewService, Guid guid, TreeViewOptions options)
    {
        Guid = guid;
        this.treeViewService = treeViewService;
        treeViewListener = options.TreeViewListener;
        foregroundBrushResourceKey = options.ForegroundBrushResourceKey ?? "TreeViewForeground";

        // Add the root at the end since Create() requires some stuff to have been initialized
        root = Create(options.RootNode ?? new TreeNodeDataImpl(new Guid(DocumentTreeViewConstants.ROOT_NODE_GUID)));
    }

    void ClassificationFormatMap_ClassificationFormatMappingChanged(object? sender, EventArgs e) => RefreshAllNodes();

    void IDisposable.Dispose()
    {
    }

    void SharpTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        SelectionChanged?.Invoke(this, Convert(e));

    static TreeViewSelectionChangedEventArgs Convert(SelectionChangedEventArgs e)
    {
        TreeNodeData[]? added = null, removed = null;
        if (e.AddedItems is not null)
            added = Convert(e.AddedItems);
        if (e.RemovedItems is not null)
            removed = Convert(e.RemovedItems);
        return new TreeViewSelectionChangedEventArgs(added, removed);
    }

    static TreeNodeData[] Convert(System.Collections.IEnumerable list) =>
        list.Cast<DsSharpTreeNode>().Select(a => a.TreeNodeImpl.Data).ToArray();

    ITreeNode ITreeView.Create(TreeNodeData data) => Create(data);

    TreeNodeImpl Create(TreeNodeData data)
    {
        Debug2.Assert(data.TreeNode is null);
        var impl = new TreeNodeImpl(this, data);
        if (treeViewListener is not null)
            treeViewListener.OnEvent(this, new TreeViewListenerEventArgs(TreeViewListenerEvent.NodeCreated, impl));
        data.Initialize();
        if (!impl.LazyLoading)
            AddChildren(impl);
        return impl;
    }

    internal void AddChildren(TreeNodeImpl impl)
    {
        foreach (var data in impl.Data.CreateChildren())
            AddSorted(impl, Create(data));

        foreach (var provider in treeViewService.GetProviders(impl.Data.Guid))
        {
            var context = new TreeNodeDataProviderContext(impl);
            foreach (var data in provider.Create(context))
                AddSorted(impl, Create(data));
        }
    }

    internal void AddSorted(TreeNodeImpl owner, ITreeNode node)
    {
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        if (node.TreeView != this)
            throw new InvalidOperationException("You can only add a ITreeNode to a treeview that created it");

        AddSorted(owner, (TreeNodeImpl)node);
    }

    internal void AddSorted(TreeNodeImpl owner, TreeNodeImpl impl)
    {
        var group = impl.Data.TreeNodeGroup;

        if (group is null)
            owner.Children.Add(impl);
        else
        {
            int index = GetInsertIndex(owner, impl, group);
            owner.Children.Insert(index, impl);
        }
    }

    int GetInsertIndex(TreeNodeImpl owner, TreeNodeImpl impl, ITreeNodeGroup group)
    {
        var children = owner.Children;

        // At creation time, it's most likely inserted last, so check that position first.
        if (children.Count >= 1)
        {
            var lastData = children[children.Count - 1].Data;
            var lastGroup = lastData.TreeNodeGroup;

            if (lastGroup is not null)
            {
                int x = Compare(impl.Data, lastData, group, lastGroup);
                if (x > 0)
                    return children.Count;
            }
        }

        int lo = 0, hi = children.Count - 1;

        while (lo <= hi)
        {
            int i = (lo + hi) / 2;

            var otherData = children[i].Data;
            var otherGroup = otherData.TreeNodeGroup;
            int x;
            if (otherGroup is null)
                x = -1;
            else
                x = Compare(impl.Data, otherData, group, otherGroup);

            if (x == 0)
                return i;

            if (x < 0)
                hi = i - 1;
            else
                lo = i + 1;
        }

        return hi + 1;
    }

    int Compare(TreeNodeData a, TreeNodeData b, ITreeNodeGroup ga, ITreeNodeGroup gb)
    {
        if (ga.Order < gb.Order)
            return -1;
        if (ga.Order > gb.Order)
            return 1;

        if (ga.GetType() != gb.GetType())
        {
            Debug.Fail($"Two different groups have identical order: {ga.GetType()} vs {gb.GetType()}");
            return ga.GetType().GetHashCode().CompareTo(gb.GetType().GetHashCode());
        }

        return ga.Compare(a, b);
    }

    public void SelectItems(IEnumerable<TreeNodeData> items)
    {
    }

    public void SelectAll()
    {
    }

    public void Focus()
    {
    }

    public void ScrollIntoView()
    {
    }

    public void RefreshAllNodes()
    {
        foreach (var node in Root.Descendants())
            node.RefreshUI();
    }

    public TreeNodeData? FromImplNode(object? selectedItem)
    {
        var node = selectedItem as DsSharpTreeNode;
        return node?.TreeNodeImpl.Data;
    }

    public object? ToImplNode(TreeNodeData node)
    {
        if (node is null)
            return null;

        var impl = node.TreeNode as TreeNodeImpl;
        Debug2.Assert(impl is not null);
        return impl?.Node;
    }

    public void OnRemoved(TreeNodeData node) => NodeRemoved?.Invoke(this, new TreeViewNodeRemovedEventArgs(node, true));

    public void CollapseUnusedNodes()
    {
        var usedNodes = new HashSet<TreeNodeData>(TopLevelSelection);
        CollapseUnusedNodes(Root.DataChildren, usedNodes);
        // Make sure the selected node is visible
        ScrollIntoView();
    }

    bool CollapseUnusedNodes(IEnumerable<TreeNodeData> nodes, HashSet<TreeNodeData> usedNodes)
    {
        bool isExpanded = false;

        foreach (var node in nodes)
        {
            var tn = node.TreeNode;
            if (usedNodes.Contains(node))
                isExpanded = true;
            if (!tn.IsExpanded)
                continue;

            if (CollapseUnusedNodes(tn.DataChildren, usedNodes))
            {
                isExpanded = true;
                tn.IsExpanded = true;
            }
            else
                tn.IsExpanded = false;
        }

        return isExpanded;
    }
}