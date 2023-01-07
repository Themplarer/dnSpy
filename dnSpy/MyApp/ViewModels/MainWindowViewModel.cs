using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.MSBuild;
using Microsoft.VisualStudio.Utilities;
using MyApp.Documents.Tabs.Dialogs;
using MyApp.Documents.TreeView;
using MyApp.Models;

namespace MyApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDocumentTreeView _documentTreeView;
    private readonly AssemblyExplorerMostRecentlyUsedList _mruList;

    public ObservableCollection<Node> Items { get; }

    public MainWindowViewModel(IDocumentTreeView documentTreeView, AssemblyExplorerMostRecentlyUsedList mruList)
    {
        _documentTreeView = documentTreeView;
        _mruList = mruList;

        Items = new ObservableCollection<Node>();
    }

    public async Task OpenFilesAsync()
    {
        var window = GetWindow();
        var files = await Dispatcher.UIThread.InvokeAsync(async () => await window.StorageProvider.OpenFilePickerAsync(CreateFileDialogOptions()));

        if (files is null or []) return;

        var openDocuments = OpenDocumentsHelper.OpenDocuments(_documentTreeView, _mruList, files.Select(f => f.Name));

        foreach (var openDocument in openDocuments)
        {
            var documentNode = _documentTreeView.CreateNode(null, openDocument);
            var node = new Node(openDocument.AssemblyDef?.Name + " (" + openDocument.AssemblyDef?.Version + ")");
            Items.Add(node);
            var childNode = new Node(openDocument.Filename);
            node.AppendChild(childNode);
            FillChildren(documentNode, childNode);
        }
    }

    private Window GetWindow() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.Windows
        .First(x => ReferenceEquals(this, x.DataContext));

    private static FilePickerOpenOptions CreateFileDialogOptions() =>
        new()
        {
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(".NET Executables")
                {
                    Patterns = new[]
                    {
                        "*.exe",
                        "*.dll",
                        "*.netmodule",
                        "*.winmd"
                    },
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[]
                    {
                        "*.*"
                    }
                }
            }
        };

    private static void FillChildren(TreeNodeData treeNodeData, Node node)
    {
        var children = treeNodeData.CreateChildren().ToArray();

        if (children.Length == 0) return;

        foreach (var child in children)
        {
            var childNode = new Node((child.Text as string)!);
            node.AppendChild(childNode);
            FillChildren(child, childNode);
        }
    }
}