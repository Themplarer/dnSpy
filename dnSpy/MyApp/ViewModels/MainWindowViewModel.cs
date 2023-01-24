using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;
using dnSpy.Documents.TreeView;
using MyApp.Documents.Tabs.Dialogs;
using MyApp.Documents.TreeView;
using MyApp.Models;
using MyApp.Views;

namespace MyApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly AssemblyExplorerMostRecentlyUsedList _mruList;
    private readonly Dictionary<string, TreeNodeData> _nodes = new();

    private IDocumentTreeView DocumentTreeView { get; }

    public ObservableCollection<Node> Items { get; } = new();

    public DecompilationHandler DecompilationHandler { get; }

    public IDecompilerOutput DecompilerOutput { get; }

    public ObservableCollection<EditorTab> Tabs { get; } = new();

    public MainWindowViewModel(IDocumentTreeView documentTreeView, AssemblyExplorerMostRecentlyUsedList mruList,
        DecompilationHandler decompilationHandler, IDecompilerOutput decompilerOutput)
    {
        DocumentTreeView = documentTreeView;
        DecompilationHandler = decompilationHandler;
        DecompilerOutput = decompilerOutput;
        _mruList = mruList;
    }

    public async Task OpenFilesAsync()
    {
        var window = GetWindow();
        var files = await Dispatcher.UIThread.InvokeAsync(async () => await window.StorageProvider.OpenFilePickerAsync(CreateFileDialogOptions()));

        if (files is null or []) return;

        var filenames = files
            .Select(f => (GotUri: f.TryGetUri(out var uri), Uri: uri))
            .Where(t => t.GotUri)
            .Select(t => t.Uri!.AbsolutePath);
        var openDocuments = OpenDocumentsHelper.OpenDocuments(DocumentTreeView, _mruList, filenames);
        var docNode = DocumentTreeView.CreateNodeFromDocs(openDocuments);

        if (docNode is AssemblyDocumentNodeImpl documentNode)
        {
            var node = new Node(documentNode.ToString(), documentNode, this);
            Items.Add(node);
            node.LoadChildren();
        }
    }

    public void AppendNode(string name, TreeNodeData treeNodeData) => _nodes[name] = treeNodeData;

    public TreeNodeData GetCodeNode(string name) => _nodes[name];

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
}