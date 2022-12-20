using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaloniaDebuggerUI.ViewModels;

namespace AvaloniaDebuggerUI;

public class ViewLocator : IDataTemplate
{
	public IControl Build(object data)
	{
		var name = data.GetType().FullName!.Replace("ViewModel", "View");

		if (Type.GetType(name) is { } type)
			return (Control)Activator.CreateInstance(type)!;

		return new TextBlock { Text = "Not Found: " + name };
	}

	public bool Match(object data) => data is ViewModelBase;
}