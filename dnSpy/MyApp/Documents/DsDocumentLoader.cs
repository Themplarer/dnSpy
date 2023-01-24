/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using dnSpy.Contracts.Documents;
using dnSpy.Documents;
using MyApp.Documents.TreeView;

namespace MyApp.Documents;

internal sealed class DsDocumentLoader : IDsDocumentLoader
{
    private readonly IDsDocumentService _documentService;
    private readonly AssemblyExplorerMostRecentlyUsedList? _mruList;
    private readonly HashSet<IDsDocument> _hash;
    private readonly List<IDsDocument> _loadedDocuments;

    public DsDocumentLoader(IDsDocumentService documentService, AssemblyExplorerMostRecentlyUsedList? mruList)
    {
        _documentService = documentService;
        _mruList = mruList;
        _hash = new HashSet<IDsDocument>();
        _loadedDocuments = new List<IDsDocument>();
    }

    public IDsDocument[] Load(IEnumerable<DocumentToLoad> documents)
    {
        foreach (var f in documents)
            Load(f);

        return _loadedDocuments.ToArray();
    }

    private void Load(DocumentToLoad f)
    {
        if (f.Info.Type == DocumentConstants.DOCUMENTTYPE_FILE && string.IsNullOrEmpty(f.Info.Name))
            return;

        if (_documentService.TryGetOrCreate(f.Info, f.IsAutoLoaded) is { } document && !_hash.Contains(document))
        {
            _loadedDocuments.Add(document);
            _hash.Add(document);

            if (!f.IsAutoLoaded)
            {
                document.IsAutoLoaded = f.IsAutoLoaded;
                _mruList?.Add(document.Filename);
            }
        }
    }
}