namespace MyApp.Models;

public class EditorTab
{
    public EditorTab(string fullName, string shortName, string contents)
    {
        FullName = fullName;
        ShortName = shortName;
        Contents = contents;
    }

    public string FullName { get; }

    public string ShortName { get; }

    public string Contents { get; }
}