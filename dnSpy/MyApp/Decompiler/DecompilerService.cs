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
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Events;

namespace MyApp.Decompiler;

internal sealed class DecompilerService : IDecompilerService
{
    private readonly DecompilerServiceSettingsImpl _decompilerServiceSettings;
    private readonly IDecompiler[] _decompilers;
    private IDecompiler _decompiler;
    private readonly WeakEventList<EventArgs> _decompilerChanged;

    public DecompilerService(DecompilerServiceSettingsImpl decompilerServiceSettings, IEnumerable<IDecompiler> languages,
        IEnumerable<IDecompilerCreator> creators)
    {
        _decompilerServiceSettings = decompilerServiceSettings;
        var decompilers = new List<IDecompiler>(languages);

        foreach (var creator in creators)
            decompilers.AddRange(creator.Create());

        if (decompilers.Count == 0)
            decompilers.Add(new DummyDecompiler());

        _decompilers = decompilers.OrderBy(a => a.OrderUI).ToArray();
        _decompiler = FindOrDefault(decompilerServiceSettings.LanguageGuid) ?? throw new InvalidOperationException();
        _decompilerChanged = new WeakEventList<EventArgs>();
    }

    public IDecompiler Decompiler
    {
        get => _decompiler;
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (Array.IndexOf(_decompilers, value) < 0)
                throw new InvalidOperationException("Can't set a language that isn't part of this instance's language collection");

            if (_decompiler == value) return;

            _decompiler = value;
            _decompilerServiceSettings.LanguageGuid = value.UniqueGuid;
            _decompilerChanged.Raise(this, EventArgs.Empty);
        }
    }

    public event EventHandler<EventArgs> DecompilerChanged
    {
        add => _decompilerChanged.Add(value);
        remove => _decompilerChanged.Remove(value);
    }

    public IEnumerable<IDecompiler> AllDecompilers => _decompilers;

    public IDecompiler FindOrDefault(Guid guid) =>
        Find(guid) ?? AllDecompilers.FirstOrDefault()!;

    public IDecompiler? Find(Guid guid) =>
        AllDecompilers.FirstOrDefault(a => a.GenericGuid == guid || a.UniqueGuid == guid);
}