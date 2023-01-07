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
using System.ComponentModel;
using dnSpy.Contracts.Settings;

namespace MyApp.Themes;

internal sealed class ThemeSettings : INotifyPropertyChanged
{
    private static readonly Guid SettingsGuid = new("34CF0AF5-D265-4393-BC68-9B8C9B8EA622");

    private readonly ISettingsService _settingsService;
    private Guid? _themeGuid;
    private bool _showAllThemes;

    private ThemeSettings(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var sect = settingsService.GetOrCreateSection(SettingsGuid);
        ThemeGuid = sect.Attribute<Guid?>(nameof(ThemeGuid));
        ShowAllThemes = sect.Attribute<bool?>(nameof(ShowAllThemes)) ?? ShowAllThemes;
        PropertyChanged += ThemeSettings_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? ThemeGuid
    {
        get => _themeGuid;
        set
        {
            if (_themeGuid != value)
            {
                _themeGuid = value;
                OnPropertyChanged(nameof(ThemeGuid));
            }
        }
    }

    public bool ShowAllThemes
    {
        get => _showAllThemes;
        set
        {
            if (_showAllThemes != value)
            {
                _showAllThemes = value;
                OnPropertyChanged(nameof(ShowAllThemes));
            }
        }
    }

    private void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    private void ThemeSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var sect = _settingsService.RecreateSection(SettingsGuid);
        sect.Attribute(nameof(ThemeGuid), ThemeGuid);
        sect.Attribute(nameof(ShowAllThemes), ShowAllThemes);
    }
}