using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using AvaloniaDebuggerUI.Models;
using ReactiveUI;

namespace AvaloniaDebuggerUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	public ObservableCollection<FileNode> Items { get; }

	public ReactiveCommand<Unit, Unit> Save { get; }

	public MainWindowViewModel()
	{
		Items = new()
		{
			CreateRootNode(@"C:\Test"),
			CreateRootNode(@"C:\Test1")
		};

		Save = ReactiveCommand.Create(() => Console.WriteLine("сохранил"));
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