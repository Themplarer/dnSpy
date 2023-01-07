// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Drawing;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ICSharpCode.TreeView;

public partial class SharpTreeNode : INotifyPropertyChanged
{
    internal SharpTreeNode ModelParent;
    private SharpTreeNodeCollection _modelChildren;
    private bool _isEditing;
    private bool _isHidden;
    private bool _isSelected;
    private bool _isExpanded;
    private bool _lazyLoading;
    private bool _canExpandRecursively = true;
    private bool? _isChecked;

    protected virtual void OnIsVisibleChanged()
    {
    }

    public SharpTreeNodeCollection Children => _modelChildren ??= new SharpTreeNodeCollection(this);

    public SharpTreeNode Parent => ModelParent;

    public virtual object Text => null;

    public virtual object Icon => null;

    public virtual object ToolTip => null;

    public int Level => Parent != null ? Parent.Level + 1 : 0;

    public bool IsRoot => Parent == null;

    public virtual bool SingleClickExpandsChildren => false;

    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            if (_isHidden == value) return;

            _isHidden = value;

            if (ModelParent != null)
                UpdateIsVisible(ModelParent.IsVisible && ModelParent._isExpanded, true);

            RaisePropertyChanged("IsHidden");
            Parent?.RaisePropertyChanged("ShowExpander");
        }
    }

    /// <summary>
    /// Return true when this node is not hidden and when all parent nodes are expanded and not hidden.
    /// </summary>
    public bool IsVisible { get; private set; } = true;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;

            _isSelected = value;
            RaisePropertyChanged("IsSelected");
        }
    }

    protected internal virtual void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
    {
        var flattener = GetListRoot().treeFlattener;

        if (e.OldItems != null)
            foreach (SharpTreeNode node in e.OldItems)
            {
                Debug.Assert(node.ModelParent == this);
                node.ModelParent = null;
                var removeEnd = node;

                while (removeEnd._modelChildren is { Count: > 0 })
                    removeEnd = removeEnd._modelChildren.Last();

                List<SharpTreeNode> removedNodes = null;
                var visibleIndexOfRemoval = 0;

                if (node.IsVisible)
                {
                    visibleIndexOfRemoval = GetVisibleIndexForNode(node);
                    removedNodes = node.VisibleDescendantsAndSelf().ToList();
                }

                RemoveNodes(node, removeEnd);

                if (removedNodes != null && flattener != null) flattener.NodesRemoved(visibleIndexOfRemoval, removedNodes);
            }

        if (e.NewItems != null)
        {
            var insertionPos = e.NewStartingIndex == 0
                ? null
                : _modelChildren[e.NewStartingIndex - 1];

            foreach (SharpTreeNode node in e.NewItems)
            {
                Debug.Assert(node.ModelParent == null);
                node.ModelParent = this;
                node.UpdateIsVisible(IsVisible && _isExpanded, false);

                while (insertionPos is { _modelChildren.Count: > 0 })
                    insertionPos = insertionPos._modelChildren.Last();

                InsertNodeAfter(insertionPos ?? this, node);
                insertionPos = node;

                if (node.IsVisible && flattener != null) flattener.NodesInserted(GetVisibleIndexForNode(node), node.VisibleDescendantsAndSelf());
            }
        }

        RaisePropertyChanged("ShowExpander");
        RaiseIsLastChangedIfNeeded(e);
    }

    public virtual object ExpandedIcon => Icon;

    public virtual bool ShowExpander => LazyLoading || Children.Any(c => !c._isHidden);

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;

            _isExpanded = value;

            if (_isExpanded)
            {
                EnsureLazyChildren();
                OnExpanding();
            }
            else
                OnCollapsing();

            UpdateChildIsVisible(true);
            RaisePropertyChanged("IsExpanded");
        }
    }

    protected virtual void OnExpanding()
    {
    }

    protected virtual void OnCollapsing()
    {
    }

    public bool LazyLoading
    {
        get => _lazyLoading;
        set
        {
            _lazyLoading = value;

            if (_lazyLoading)
            {
                IsExpanded = false;

                if (_canExpandRecursively)
                {
                    _canExpandRecursively = false;
                    RaisePropertyChanged("CanExpandRecursively");
                }
            }

            RaisePropertyChanged("LazyLoading");
            RaisePropertyChanged("ShowExpander");
        }
    }

    /// <summary>
    /// Gets whether this node can be expanded recursively.
    /// If not overridden, this property returns false if the node is using lazy-loading, and true otherwise.
    /// </summary>
    public virtual bool CanExpandRecursively => _canExpandRecursively;

    public virtual bool ShowIcon => Icon != null;

    protected virtual void LoadChildren() => throw new NotSupportedException(GetType().Name + " does not support lazy loading");

    /// <summary>
    /// Ensures the children were initialized (loads children if lazy loading is enabled)
    /// </summary>
    public void EnsureLazyChildren()
    {
        if (!LazyLoading) return;

        LazyLoading = false;
        LoadChildren();
    }

    public IEnumerable<SharpTreeNode> Descendants() => TreeTraversal.PreOrder(Children, n => n.Children);

    public IEnumerable<SharpTreeNode> DescendantsAndSelf() => TreeTraversal.PreOrder(this, n => n.Children);

    internal IEnumerable<SharpTreeNode> VisibleDescendants() =>
        TreeTraversal.PreOrder(Children.Where(c => c.IsVisible), n => n.Children.Where(c => c.IsVisible));

    internal IEnumerable<SharpTreeNode> VisibleDescendantsAndSelf() => TreeTraversal.PreOrder(this, n => n.Children.Where(c => c.IsVisible));

    public IEnumerable<SharpTreeNode> Ancestors()
    {
        for (var n = Parent; n != null; n = n.Parent)
            yield return n;
    }

    public IEnumerable<SharpTreeNode> AncestorsAndSelf()
    {
        for (var n = this; n != null; n = n.Parent)
            yield return n;
    }

    public virtual bool IsEditable => false;

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing == value) return;

            _isEditing = value;
            RaisePropertyChanged("IsEditing");
        }
    }

    public virtual string LoadEditText() => null;

    public virtual bool SaveEditText(string value) => true;

    public virtual bool IsCheckable => false;

    public bool? IsChecked
    {
        get => _isChecked;
        set => SetIsChecked(value, true);
    }

    public bool IsCut => false;

    public virtual bool CanDelete() => false;

    public virtual void Delete() => throw new NotSupportedException(GetType().Name + " does not support deletion");

    public virtual void DeleteCore() => throw new NotSupportedException(GetType().Name + " does not support deletion");

    public virtual IDataObject Copy(SharpTreeNode[] nodes) =>
        throw new NotSupportedException(GetType().Name + " does not support copy/paste or drag'n'drop");

    public virtual bool CanDrag(SharpTreeNode[] nodes) => false;

    public virtual void StartDrag(AvaloniaObject dragSource, SharpTreeNode[] nodes)
    {
    }

    public virtual bool CanDrop(DragEventArgs e, int index) => false;

    internal void InternalDrop(DragEventArgs e, int index)
    {
        if (LazyLoading)
        {
            EnsureLazyChildren();
            index = Children.Count;
        }

        Drop(e, index);
    }

    public virtual void Drop(DragEventArgs e, int index) => throw new NotSupportedException(GetType().Name + " does not support Drop()");

    public bool IsLast => Parent == null || Parent.Children[^1] == this;

    public event PropertyChangedEventHandler PropertyChanged;

    protected bool HasPropertyChangedHandlers => PropertyChanged != null;

    public void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Gets called when the item is double-clicked.
    /// </summary>
    public virtual void ActivateItem(RoutedEventArgs e)
    {
    }

    public override string ToString() => Text != null ? Text.ToString() : string.Empty;

    private void RaiseIsLastChangedIfNeeded(NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewStartingIndex == Children.Count - 1)
                {
                    if (Children.Count > 1)
                        Children[^2].RaisePropertyChanged("IsLast");

                    Children[^1].RaisePropertyChanged("IsLast");
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldStartingIndex == Children.Count && Children.Count > 0)
                    Children[^1].RaisePropertyChanged("IsLast");

                break;
        }
    }

    private void SetIsChecked(bool? value, bool update)
    {
        if (_isChecked != value)
        {
            _isChecked = value;

            if (update)
            {
                if (IsChecked != null)
                    foreach (var child in Descendants())
                        if (child.IsCheckable)
                            child.SetIsChecked(IsChecked, false);

                foreach (var parent in Ancestors())
                    if (parent.IsCheckable && !parent.TryValueForIsChecked(true) && !parent.TryValueForIsChecked(false))
                        parent.SetIsChecked(null, false);
            }

            RaisePropertyChanged("IsChecked");
        }
    }

    private bool TryValueForIsChecked(bool? value)
    {
        if (Children.Where(n => n.IsCheckable).All(n => n.IsChecked == value))
        {
            SetIsChecked(value, false);
            return true;
        }

        return false;
    }

    private void UpdateIsVisible(bool parentIsVisible, bool updateFlattener)
    {
        var newIsVisible = parentIsVisible && !_isHidden;

        if (IsVisible != newIsVisible)
        {
            IsVisible = newIsVisible;

            // invalidate the augmented data
            var node = this;

            while (node is { totalListLength: >= 0 })
            {
                node.totalListLength = -1;
                node = node.listParent;
            }

            // Remember the removed nodes:
            List<SharpTreeNode> removedNodes = null;

            if (updateFlattener && !newIsVisible)
            {
                removedNodes = VisibleDescendantsAndSelf().ToList();
            }

            // also update the model children:
            UpdateChildIsVisible(false);

            // Validate our invariants:
            if (updateFlattener)
                CheckRootInvariants();

            // Tell the flattener about the removed nodes:
            if (removedNodes != null)
            {
                var flattener = GetListRoot().treeFlattener;

                if (flattener != null)
                {
                    flattener.NodesRemoved(GetVisibleIndexForNode(this), removedNodes);
                    foreach (var n in removedNodes)
                        n.OnIsVisibleChanged();
                }
            }

            // Tell the flattener about the new nodes:
            if (updateFlattener && newIsVisible)
            {
                var flattener = GetListRoot().treeFlattener;

                if (flattener != null)
                {
                    flattener.NodesInserted(GetVisibleIndexForNode(this), VisibleDescendantsAndSelf());
                    foreach (var n in VisibleDescendantsAndSelf())
                        n.OnIsVisibleChanged();
                }
            }
        }
    }

    private void UpdateChildIsVisible(bool updateFlattener)
    {
        if (_modelChildren is not { Count: > 0 }) return;

        var showChildren = IsVisible && _isExpanded;

        foreach (var child in _modelChildren) child.UpdateIsVisible(showChildren, updateFlattener);
    }
}