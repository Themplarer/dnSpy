using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Contracts.TreeView;
using MyApp.ViewModels;

namespace MyApp.Models;

public class Node
{
    private readonly TreeNodeData _nodeData;
    private readonly MainWindowViewModel _mainWindowViewModel;

    public Node(string name, TreeNodeData treeNodeData, MainWindowViewModel mainWindowViewModel)
    {
        Name = name;
        _nodeData = treeNodeData;
        _mainWindowViewModel = mainWindowViewModel;
        _mainWindowViewModel.AppendNode(name, treeNodeData);
        Children = new ObservableCollection<Node>();
    }

    public string Name { get; }

    public ObservableCollection<Node> Children { get; }

    public bool Loaded { get; private set; }

    public bool LoadChildren()
    {
        if (Loaded) return false;

        Children.Clear();
        Loaded = true;
        FillChildren(this);
        return Children.Count > 0;
    }

    private static void FillChildren(Node node)
    {
        var children = node._nodeData.CreateChildren().ToArray();
        var queue = new Queue<Node>();

        if (children.Length == 0) return;

        foreach (var child in children)
        {
            var childNode = new Node(child.ToString()!, child, node._mainWindowViewModel);
            node.Children.Add(childNode);
            queue.Enqueue(childNode);
        }

        while (queue.TryDequeue(out var n)) n.LoadChildren();
    }
}