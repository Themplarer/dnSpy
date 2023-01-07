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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml.Linq;
using Avalonia;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Themes;
using dnSpy.Events;
using dnSpy.Themes;

namespace MyApp.Themes;

internal sealed class ThemeService : IThemeServiceImpl
{
    private static readonly Guid DefaultThemeGuid = ThemeConstants.THEME_DARK_GUID;
    private static readonly Guid DefaultHighContrastThemeGuid = ThemeConstants.THEME_HIGHCONTRAST_GUID;

    private readonly Dictionary<Guid, Theme> _themes;
    private ITheme? _theme;
    private bool _isHighContrast;
    private readonly WeakEventList<ThemeChangedEventArgs> _themeChangedHighPriority;
    private readonly WeakEventList<ThemeChangedEventArgs> _themeChanged;
    private readonly WeakEventList<ThemeChangedEventArgs> _themeChangedLowPriority;

    private ThemeService(ThemeSettings themeSettings)
    {
        Settings = themeSettings;
        _themeChangedHighPriority = new WeakEventList<ThemeChangedEventArgs>();
        _themeChanged = new WeakEventList<ThemeChangedEventArgs>();
        _themeChangedLowPriority = new WeakEventList<ThemeChangedEventArgs>();
        _themes = new Dictionary<Guid, Theme>();
        Load();
        Debug.Assert(_themes.Count != 0);
        // SystemEvents.UserPreferenceChanged += (s, e) => IsHighContrast = SystemParameters.HighContrast;
        // IsHighContrast = SystemParameters.HighContrast;
        Initialize(themeSettings.ThemeGuid ?? DefaultThemeGuid);
    }

    public ITheme Theme
    {
        get => _theme!;
        set
        {
            if (_theme == value) return;

            _theme = value;
            Settings.ThemeGuid = value.Guid;
            InitializeResources();
            _themeChangedHighPriority.Raise(this, new ThemeChangedEventArgs());
            _themeChanged.Raise(this, new ThemeChangedEventArgs());
            _themeChangedLowPriority.Raise(this, new ThemeChangedEventArgs());
        }
    }

    public IEnumerable<ITheme> AllThemes => _themes.Values.OrderBy(x => x.Order);

    public IEnumerable<ITheme> VisibleThemes => AllThemes.Where(theme => Settings.ShowAllThemes || IsHighContrast || !theme.IsHighContrast);

    public bool IsHighContrast
    {
        get => _isHighContrast;
        set
        {
            if (_isHighContrast == value) return;

            _isHighContrast = value;
            SwitchThemeIfNecessary();
        }
    }

    public event EventHandler<ThemeChangedEventArgs> ThemeChangedHighPriority
    {
        add => _themeChangedHighPriority.Add(value);
        remove => _themeChangedHighPriority.Remove(value);
    }

    public event EventHandler<ThemeChangedEventArgs> ThemeChanged
    {
        add => _themeChanged.Add(value);
        remove => _themeChanged.Remove(value);
    }

    public event EventHandler<ThemeChangedEventArgs> ThemeChangedLowPriority
    {
        add => _themeChangedLowPriority.Add(value);
        remove => _themeChangedLowPriority.Remove(value);
    }

    public ThemeSettings Settings { get; }

    private void InitializeResources()
    {
        var app = Application.Current;

        Debug2.Assert(app is not null);

        if (app is not null)
            ((Theme)Theme).UpdateResources(app.Resources);
    }

    private Guid CurrentDefaultThemeGuid => IsHighContrast ? DefaultHighContrastThemeGuid : DefaultThemeGuid;

    private void SwitchThemeIfNecessary()
    {
        if (_theme is null || _theme.IsHighContrast != IsHighContrast)
            Theme = GetThemeOrDefault(CurrentDefaultThemeGuid);
    }

    private void Load()
    {
        foreach (var basePath in GetDnthemePaths())
        {
            string[] files;

            try
            {
                if (!Directory.Exists(basePath))
                    continue;

                files = Directory.GetFiles(basePath, "*.dntheme", SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (SecurityException)
            {
                continue;
            }

            foreach (var filename in files)
                Load(filename);
        }
    }

    private IEnumerable<string> GetDnthemePaths() => AppDirectories.GetDirectories("Themes");

    private Theme? Load(string filename)
    {
        try
        {
            var root = XDocument.Load(filename).Root;
            if (root?.Name != "theme")
                return null;

            var theme = new Theme(root);
            if (string.IsNullOrEmpty(theme.MenuName))
                return null;

            _themes[theme.Guid] = theme;
            return theme;
        }
        catch (Exception)
        {
            Debug.Fail($"Failed to load file '{filename}'");
        }

        return null;
    }

    private void Initialize(Guid themeGuid)
    {
        var theme = GetThemeOrDefault(themeGuid);

        if (theme.IsHighContrast != IsHighContrast)
            theme = GetThemeOrDefault(CurrentDefaultThemeGuid) ?? theme;

        Theme = theme;
    }

    private ITheme GetThemeOrDefault(Guid guid) =>
        _themes.TryGetValue(guid, out var theme) || _themes.TryGetValue(DefaultThemeGuid, out theme)
            ? theme
            : AllThemes.First();
}