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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Extension;

namespace MyApp.Extension;

sealed class ExtensionService : IExtensionService
{
    private readonly Lazy<IAutoLoaded, IAutoLoadedMetadata>[] _mefAutoLoaded;
    private readonly Lazy<IExtension, IExtensionMetadata>[] _extensions;
    private LoadedExtension[]? _loadedExtensions;

    public ExtensionService(IEnumerable<Lazy<IAutoLoaded, IAutoLoadedMetadata>> mefAutoLoaded,
        IEnumerable<Lazy<IExtension, IExtensionMetadata>> extensions)
    {
        _mefAutoLoaded = mefAutoLoaded.OrderBy(a => a.Metadata.Order).ToArray();
        _extensions = extensions.OrderBy(a => a.Metadata.Order).ToArray();
    }

    public IEnumerable<IExtension> Extensions => _extensions.Select(a => a.Value);

    public IEnumerable<LoadedExtension> LoadedExtensions
    {
        get
        {
            Debug.Assert(_loadedExtensions is not null, "Called too early");
            return _loadedExtensions ?? Array.Empty<LoadedExtension>();
        }
        internal set
        {
            Debug.Assert(_loadedExtensions is null);

            if (_loadedExtensions is not null)
                throw new InvalidOperationException();

            _loadedExtensions = value.ToArray();
        }
    }

    public void LoadExtensions(Collection<IResourceDictionary> mergedDictionaries)
    {
        LoadAutoLoaded(AutoLoadedLoadType.BeforeExtensions);
        // It's not an extension but it needs to show stuff in the options dialog box
        AddMergedDictionary(mergedDictionaries);

        foreach (var m in _extensions)
        foreach (var _ in m.Value.MergedResourceDictionaries)
            AddMergedDictionary(mergedDictionaries);

        LoadAutoLoaded(AutoLoadedLoadType.AfterExtensions);
        NotifyExtensions(ExtensionEvent.Loaded, null);
        LoadAutoLoaded(AutoLoadedLoadType.AfterExtensionsLoaded);
    }

    void AddMergedDictionary(Collection<IResourceDictionary> mergedDictionaries) => mergedDictionaries.Add(new ResourceDictionary());

    void LoadAutoLoaded(AutoLoadedLoadType loadType)
    {
    }

    void NotifyExtensions(ExtensionEvent @event, object? obj)
    {
        foreach (var m in _extensions)
            m.Value.OnEvent(@event, obj);
    }

    public void OnAppLoaded()
    {
        NotifyExtensions(ExtensionEvent.AppLoaded, null);
        LoadAutoLoaded(AutoLoadedLoadType.AppLoaded);
    }

    public void OnAppExit() => NotifyExtensions(ExtensionEvent.AppExit, null);
}