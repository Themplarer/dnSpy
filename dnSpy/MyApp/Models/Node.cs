using System.Collections.ObjectModel;

namespace MyApp.Models;

public class Node
{
	public Node(string name)
	{
		Name = name;
		Children = new ObservableCollection<Node>();
	}

	public string Name { get; }

	public ObservableCollection<Node> Children { get; }

	public void AppendChild(Node childNode) => Children.Add(childNode);
}