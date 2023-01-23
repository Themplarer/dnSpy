using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class AssemblyExplorerView : UserControl
{
    public AssemblyExplorerView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var mainWindowViewModel = (MainWindowViewModel)DataContext!;
        var content = ((AccessText)e.Source!).Text!;
        var treeNodeData = mainWindowViewModel.GetCodeNode(content);
        var decompilationHandler = mainWindowViewModel.DecompilationHandler;
        decompilationHandler.Decompile(treeNodeData);
        var a = 100;
    }
}