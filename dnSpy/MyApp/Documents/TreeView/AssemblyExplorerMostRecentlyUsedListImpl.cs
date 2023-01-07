using System;
using System.Collections.Generic;
using dnSpy.Contracts.Settings;

namespace MyApp.Documents.TreeView;

internal sealed class AssemblyExplorerMostRecentlyUsedListImpl : AssemblyExplorerMostRecentlyUsedList
{
    private static readonly Guid SettingsGuid = new("642B9276-3C9A-4EFE-9B3B-D62046824B18");
    private const int MaxFileCount = 30;
    private const string FilenameSectionName = "file";
    private const string FilenameAttributeName = "name";

    private readonly ISettingsSection _fileSection;
    private readonly List<string> _fileList;

    public AssemblyExplorerMostRecentlyUsedListImpl(ISettingsService settingsService)
    {
        _fileList = new List<string>(MaxFileCount);
        _fileSection = settingsService.GetOrCreateSection(SettingsGuid);

        foreach (var fileSect in _fileSection.SectionsWithName(FilenameSectionName))
        {
            if (_fileList.Count == MaxFileCount)
                break;

            if (fileSect.Attribute<string?>(FilenameAttributeName) is { } name)
                _fileList.Add(name);
        }
    }

    public override string[] RecentFiles => _fileList.ToArray();

    public override void Add(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return;

        if (GetIndexOf(filename) is { } index)
            _fileList.RemoveAt(index);

        if (_fileList.Count == MaxFileCount)
            _fileList.RemoveAt(_fileList.Count - 1);

        _fileList.Insert(0, filename);
        Save();
    }

    private int? GetIndexOf(string filename)
    {
        for (var i = 0; i < _fileList.Count; i++)
            if (StringComparer.OrdinalIgnoreCase.Equals(filename, _fileList[i]))
                return i;

        return null;
    }

    private void Save()
    {
        _fileSection.RemoveSection(FilenameSectionName);

        foreach (var file in _fileList)
        {
            var fileSect = _fileSection.CreateSection(FilenameSectionName);
            fileSect.Attribute(FilenameAttributeName, file);
        }
    }
}