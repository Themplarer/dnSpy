using System.Collections.ObjectModel;
using System.IO;

namespace AvaloniaDebuggerUI.Models;

public class FileNode
{
	public FileNode(string fullPath)
	{
		FullPath = fullPath;
		FileName = Path.GetFileName(fullPath);
		Subfolders = new();
	}

	public string FullPath { get; }

	public string FileName { get; }

	public ObservableCollection<FileNode> Subfolders { get; }

	public void AppendSubfolder(FileNode childNode) => Subfolders.Add(childNode);
}