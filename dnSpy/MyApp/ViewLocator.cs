using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyApp.ViewModels;

namespace MyApp;

public class ViewLocator : IDataTemplate
{
    public IControl Build(object? data) =>
        data?.GetType().FullName!.Replace("ViewModel", "View") is var name && name is { } && Type.GetType(name) is { } type
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = "Not Found: " + name };

    public bool Match(object? data) => data is ViewModelBase;
}