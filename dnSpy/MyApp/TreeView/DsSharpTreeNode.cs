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

using System.Diagnostics;
using System.Linq;
using Avalonia.Interactivity;
using dnSpy.Contracts.TreeView;
using ICSharpCode.TreeView;

namespace dnSpy.TreeView;

sealed class DsSharpTreeNode : SharpTreeNode
{
    public TreeNodeImpl TreeNodeImpl => treeNodeImpl;

    readonly TreeNodeImpl treeNodeImpl;

    public DsSharpTreeNode(TreeNodeImpl treeNodeImpl) => this.treeNodeImpl = treeNodeImpl;

    // Needed by XAML
    public TreeNodeData Data => treeNodeImpl.Data;

    public override object ExpandedIcon => treeNodeImpl.Data.ExpandedIcon ?? treeNodeImpl.Data.Icon;

    public override object Icon => treeNodeImpl.Data.Icon;

    public override bool SingleClickExpandsChildren => treeNodeImpl.Data.SingleClickExpandsChildren;

    public override object? Text => treeNodeImpl.Data.Text;

    public override object? ToolTip => treeNodeImpl.Data.ToolTip;

    protected override void LoadChildren() => treeNodeImpl.TreeView.AddChildren(treeNodeImpl);

    public override bool ShowExpander => treeNodeImpl.Data.ShowExpander(base.ShowExpander);

    // public override Brush Foreground => treeNodeImpl.TreeView.GetNodeForegroundBrush();

    public void RefreshUI()
    {
        RaisePropertyChanged(nameof(Icon));
        RaisePropertyChanged(nameof(ExpandedIcon));
        RaisePropertyChanged(nameof(ToolTip));
        RaisePropertyChanged(nameof(Text));
        // RaisePropertyChanged(nameof(Foreground));
    }

    public override sealed bool IsCheckable => false;

    public override sealed bool IsEditable => false;

    public override void ActivateItem(RoutedEventArgs e) => e.Handled = treeNodeImpl.Data.Activate();

    protected override void OnIsVisibleChanged()
    {
        base.OnIsVisibleChanged();
        treeNodeImpl.Data.OnIsVisibleChanged();
    }

    protected override void OnExpanding()
    {
        base.OnExpanding();
        treeNodeImpl.Data.OnIsExpandedChanged(true);
    }

    protected override void OnCollapsing()
    {
        base.OnCollapsing();
        Debug.Assert(!IsExpanded);
        treeNodeImpl.Data.OnIsExpandedChanged(false);
    }

    public override bool CanDrag(SharpTreeNode[] nodes) =>
        treeNodeImpl.Data.CanDrag(nodes.OfType<DsSharpTreeNode>().Select(a => a.TreeNodeImpl.Data).ToArray());
}