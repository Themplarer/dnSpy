// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

namespace ICSharpCode.TreeView;

sealed class TreeFlattener : IList, INotifyCollectionChanged
{
    private readonly bool _includeRoot;
    private readonly Dictionary<int, SharpTreeNode> _nodeCache = new();

    /// <summary>
    /// The root node of the flat list tree.
    /// Tjis is not necessarily the root of the model!
    /// </summary>
    internal SharpTreeNode root;

    public TreeFlattener(SharpTreeNode modelRoot, bool includeRoot)
    {
        root = modelRoot;

        while (root.listParent != null)
            root = root.listParent;

        root.treeFlattener = this;
        _includeRoot = includeRoot;
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _nodeCache.Clear();
        CollectionChanged?.Invoke(this, e);
    }

    public void NodesInserted(int index, IEnumerable<SharpTreeNode> nodes)
    {
        if (!_includeRoot) index--;

        foreach (var node in nodes)
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index++));
    }

    public void NodesRemoved(int index, IEnumerable<SharpTreeNode> nodes)
    {
        if (!_includeRoot) index--;

        foreach (var node in nodes)
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, index));
    }

    public void Stop()
    {
        Debug.Assert(root.treeFlattener == this);
        root.treeFlattener = null;
    }

    public object this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException();

            if (_nodeCache.TryGetValue(index, out var node))
                return node;

            node = SharpTreeNode.GetNodeByVisibleIndex(root, _includeRoot ? index : index + 1);
            _nodeCache[index] = node;
            return node;
        }
        set => throw new NotSupportedException();
    }

    public int Count => _includeRoot
            ? root.GetTotalListLength()
            : root.GetTotalListLength() - 1;

    public int IndexOf(object item)
    {
        if (item is SharpTreeNode { IsVisible: true } node && node.GetListRoot() == root)
            return _includeRoot
                ? SharpTreeNode.GetVisibleIndexForNode(node)
                : SharpTreeNode.GetVisibleIndexForNode(node) - 1;

        return -1;
    }

    bool IList.IsReadOnly => true;

    bool IList.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot { get; } = new();

    void IList.Insert(int index, object item) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    int IList.Add(object item) => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    public bool Contains(object item) => IndexOf(item) >= 0;

    public void CopyTo(Array array, int arrayIndex)
    {
        foreach (var item in this)
            array.SetValue(item, arrayIndex++);
    }

    void IList.Remove(object item) => throw new NotSupportedException();

    public IEnumerator GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }
}