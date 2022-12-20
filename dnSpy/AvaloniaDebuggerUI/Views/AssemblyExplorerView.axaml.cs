using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaloniaDebuggerUI.Views;

public partial class AssemblyExplorerView : UserControl
{
	public AssemblyExplorerView() => InitializeComponent();

	private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}