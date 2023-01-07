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
using System.Linq;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Documents;
using MyApp.Documents.TreeView;

namespace MyApp.Documents.Tabs.Dialogs;

internal static class OpenDocumentsHelper
{
    internal static IDsDocument[] OpenDocuments(IDocumentTreeView documentTreeView, AssemblyExplorerMostRecentlyUsedList mruList,
        IEnumerable<string> filenames)
    {
        var documentLoader = new DsDocumentLoader(documentTreeView.DocumentService, mruList);
        return documentLoader.Load(filenames.Select(a => new DocumentToLoad(DsDocumentInfo.CreateDocument(a))));
    }
}