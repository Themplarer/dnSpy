using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using MyApp.Models;
using ReactiveUI;

namespace MyApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<FileNode> Items { get; }

    public ICommand Save { get; } = null!;

    public ICommand Open { get; }

    public MainWindowViewModel()
    {
        Items = new ObservableCollection<FileNode>();

        Open = ReactiveCommand.CreateFromTask(NewMethod);
    }

    public async Task NewMethod()
    {
        var windows = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows;
        var window = windows!.FirstOrDefault(x => ReferenceEquals(this, x.DataContext));

        await new AboutAvaloniaDialog().ShowDialog(window!);

        var openFileDialog = new OpenFileDialog
        {
            AllowMultiple = true,
            Filters = new List<FileDialogFilter>
            {
                new()
                {
                    Extensions = new List<string>
                    {
                        "exe",
                        "dll",
                        "netmodule",
                        "winmd"
                    },
                    Name = ".NET Executables"
                },
                new()
                {
                    Extensions = new List<string>
                    {
                        "*"
                    },
                    Name = "All Files"
                }
            }
        };
        var strings = await openFileDialog.ShowAsync(window!);

        if (strings is null or []) return;

        // var openDocuments = OpenDocumentsHelper.OpenDocuments(_dsDocumentService, strings);

        // foreach (var openDocument in openDocuments) Items.Add(new FileNode(openDocument.Filename));
    }

    private static FileNode CreateRootNode(string path)
    {
        var rootNode = new FileNode(path);

        foreach (var subfolder in GetSubfolders(path))
            rootNode.AppendSubfolder(subfolder);

        return rootNode;
    }

    private static IEnumerable<FileNode> GetSubfolders(string path) =>
        GetSubfolders(Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly));

    private static IEnumerable<FileNode> GetSubfolders(IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            var node = new FileNode(directory);

            foreach (var subfolder in GetSubfolders(directory))
                node.AppendSubfolder(subfolder);

            yield return node;
        }
    }
}