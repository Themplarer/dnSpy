using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using MyApp.Models;

namespace MyApp.Views;

public class TreeNodeControl : TemplatedControl
{
    public TreeNodeControl()
    {
        var a = 100000;
    }
    
    static TreeNodeControl()
    {
        TappedEvent.AddClassHandler<TreeView>((c, _) =>
        {
            var a = 100;
        }, handledEventsToo: false);

        TappedEvent.AddClassHandler<TextBlock>((block, args) =>
        {
            var b = 10000;
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public static readonly StyledProperty<Node> NodeProperty = AvaloniaProperty.Register<TreeNodeControl, Node>(nameof(Node));

    public Node Node
    {
        get => GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }

    public static readonly StyledProperty<string> NodeNameProperty = AvaloniaProperty.Register<TreeNodeControl, string>(nameof(Name));

    public string NodeName
    {
        get => GetValue(NodeNameProperty);
        set => SetValue(NodeNameProperty, value);
    }
}