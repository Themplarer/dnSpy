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
using System.Drawing;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using dnSpy.Contracts.Themes;
using Brush = Avalonia.Media.Brush;
using Color = dnSpy.Themes.Color;
using FontStyle = Avalonia.Media.FontStyle;

namespace MyApp.Themes;

internal sealed class Theme : ITheme
{
    private static readonly Dictionary<string, ColorType> NameToColorType = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly ColorInfo[] ColorInfos = new ColorInfo[ColorType.LastNR - ColorType.FirstNR + (ColorType.LastUI - ColorType.FirstUI)];
    private static readonly ColorConverter ColorConverter = new();

    private readonly Color[] _hlColors = new Color[ColorInfos.Length];

    static Theme()
    {
        foreach (var fi in typeof(ColorType).GetFields())
            if (fi.IsLiteral && (ColorType)fi.GetValue(null)! is var val and not (ColorType.LastNR or ColorType.LastUI))
                NameToColorType[fi.Name] = val;

        InitColorInfos(dnSpy.Themes.ColorInfos.RootColorInfos);

        for (var i = 0; i < ColorInfos.Length; i++)
        {
            var colorType = ToColorType(i);

            if (ColorInfos[i] is null)
            {
                Debug.Fail($"Missing info: {colorType}");
                throw new Exception($"Missing info: {colorType}");
            }
        }
    }

    public Theme(XElement root)
    {
        var guid = root.Attribute("guid");

        if (guid is null || string.IsNullOrEmpty(guid.Value))
            throw new Exception("Missing or empty guid attribute");

        Guid = new Guid(guid.Value);

        Name = (string?)root.Attribute("name") ?? string.Empty;

        var menuName = root.Attribute("menu-name");
        if (menuName is null || string.IsNullOrEmpty(menuName.Value))
            throw new Exception("Missing or empty menu-name attribute");

        MenuName = menuName.Value;

        var hcName = root.Attribute("is-high-contrast");
        IsHighContrast = hcName is not null && (bool)hcName;

        var darkThemeName = root.Attribute("is-dark");
        IsDark = darkThemeName is not null && (bool)darkThemeName;

        var lightThemeName = root.Attribute("is-light");
        IsLight = lightThemeName is not null && (bool)lightThemeName;

        var sort = root.Attribute("order");
        Order = sort is null ? 1 : (double)sort;

        for (int i = 0; i < _hlColors.Length; i++)
            _hlColors[i] = new Color(ColorInfos[i]);

        var colors = root.Element("colors");

        if (colors is not null)
        {
            foreach (var color in colors.Elements("color"))
            {
                ColorType colorType = 0;
                var hl = ReadColor(color, ref colorType);
                if (hl is null)
                    continue;

                _hlColors[ToIndex(colorType)].OriginalColor = hl;
            }
        }

        for (int i = 0; i < _hlColors.Length; i++)
        {
            if (_hlColors[i].OriginalColor is null)
                _hlColors[i].OriginalColor = CreateThemeColor(ToColorType(i));
            _hlColors[i].TextInheritedColor = new ThemeColor { Name = _hlColors[i].OriginalColor.Name };
            _hlColors[i].InheritedColor = new ThemeColor { Name = _hlColors[i].OriginalColor.Name };
        }

        RecalculateInheritedColorProperties();
    }

    public Guid Guid { get; }

    public string Name { get; }

    public string MenuName { get; }

    public bool IsHighContrast { get; }

    public bool IsDark { get; }

    public bool IsLight { get; }

    public double Order { get; }

    public IThemeColor GetExplicitColor(ColorType colorType) => GetColorInternal(colorType).OriginalColor;

    public IThemeColor GetTextColor(ColorType colorType) => GetColorInternal(colorType).TextInheritedColor;

    public IThemeColor GetColor(ColorType colorType) => GetColorInternal(colorType).InheritedColor;

    internal void UpdateResources(IResourceDictionary resources)
    {
        foreach (var color in _hlColors)
        {
            foreach (var kv in color.ColorInfo.GetResourceKeyValues(color.InheritedColor))
                resources[kv.Item1] = kv.Item2;
        }
    }

    private static int ToIndex(ColorType colorType) =>
        colorType switch
        {
            >= ColorType.FirstNR and < ColorType.LastNR => (int)(colorType - ColorType.FirstNR),
            >= ColorType.FirstUI and < ColorType.LastUI => (int)(colorType - ColorType.FirstUI + ColorType.LastNR - ColorType.FirstNR),
            _ => ReturnInvalidColorIndex(colorType)
        };

    private static int ReturnInvalidColorIndex(ColorType colorType)
    {
        Debug.Fail($"Invalid color: {colorType}");
        return 0;
    }

    private static ColorType ToColorType(int i)
    {
        if (0 <= i && i < ColorType.LastNR - ColorType.FirstNR)
            return ColorType.FirstNR + (uint)i;

        if (i is >= (int)(ColorType.LastNR - ColorType.FirstNR)
            and < (int)(ColorType.LastNR - ColorType.FirstNR + (ColorType.LastUI - ColorType.FirstUI)))
            return ColorType.FirstUI + ((uint)i - (ColorType.LastNR - ColorType.FirstNR));

        Debug.Fail($"Invalid color index: {i}");
        return 0;
    }

    private static void InitColorInfos(ColorInfo[] infos)
    {
        foreach (var info in infos)
        {
            var i = ToIndex(info.ColorType);

            if (ColorInfos[i] is not null)
            {
                Debug.Fail("Duplicate");
                throw new Exception("Duplicate");
            }

            ColorInfos[i] = info;
            InitColorInfos(info.Children);
        }
    }

    /// <summary>
    /// Recalculates the inherited color properties and should be called whenever any of the
    /// color properties have been modified.
    /// </summary>
    private void RecalculateInheritedColorProperties()
    {
        for (int i = 0; i < _hlColors.Length; i++)
        {
            var info = ColorInfos[i];
            var textColor = _hlColors[i].TextInheritedColor;
            var color = _hlColors[i].InheritedColor;

            if (info.ColorType == ColorType.DefaultText)
            {
                color.Foreground = textColor.Foreground = _hlColors[ToIndex(info.ColorType)].OriginalColor.Foreground;
                color.Background = textColor.Background = _hlColors[ToIndex(info.ColorType)].OriginalColor.Background;
                color.Color3 = textColor.Color3 = _hlColors[ToIndex(info.ColorType)].OriginalColor.Color3;
                color.Color4 = textColor.Color4 = _hlColors[ToIndex(info.ColorType)].OriginalColor.Color4;
                color.FontStyle = textColor.FontStyle = _hlColors[ToIndex(info.ColorType)].OriginalColor.FontStyle;
                color.FontWeight = textColor.FontWeight = _hlColors[ToIndex(info.ColorType)].OriginalColor.FontWeight;
            }
            else
            {
                textColor.Foreground = GetForeground(info, false);
                textColor.Background = GetBackground(info, false);
                textColor.Color3 = GetColor3(info, false);
                textColor.Color4 = GetColor4(info, false);
                textColor.FontStyle = GetFontStyle(info, false);
                textColor.FontWeight = GetFontWeight(info, false);

                color.Foreground = GetForeground(info, true);
                color.Background = GetBackground(info, true);
                color.Color3 = GetColor3(info, true);
                color.Color4 = GetColor4(info, true);
                color.FontStyle = GetFontStyle(info, true);
                color.FontWeight = GetFontWeight(info, true);
            }
        }
    }

    private Brush? GetForeground(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.Foreground;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private Brush? GetBackground(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.Background;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private Brush? GetColor3(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.Color3;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private Brush? GetColor4(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.Color4;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private FontStyle? GetFontStyle(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.FontStyle;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private FontWeight? GetFontWeight(ColorInfo? info, bool canIncludeDefault)
    {
        while (info is not null)
        {
            if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
                break;

            var color = _hlColors[ToIndex(info.ColorType)];
            var val = color.OriginalColor.FontWeight;
            if (val is not null)
                return val;

            info = info.Parent;
        }

        return null;
    }

    private Color GetColorInternal(ColorType colorType)
    {
        uint i = (uint)ToIndex(colorType);
        if (i >= (uint)_hlColors.Length)
            return _hlColors[ToIndex(ColorType.DefaultText)];

        return _hlColors[i];
    }

    private ThemeColor? ReadColor(XElement color, ref ColorType colorType)
    {
        var name = color.Attribute("name");
        if (name is null)
            return null;

        colorType = ToColorType(name.Value);
        if (colorType == ColorType.LastUI)
            return null;

        var colorInfo = ColorInfos[ToIndex(colorType)];

        var hl = new ThemeColor();
        hl.Name = colorType.ToString();

        var fg = GetAttribute(color, "fg", colorInfo.DefaultForeground);
        if (fg is not null)
            hl.Foreground = CreateColor(fg);

        var bg = GetAttribute(color, "bg", colorInfo.DefaultBackground);
        if (bg is not null)
            hl.Background = CreateColor(bg);

        var color3 = GetAttribute(color, "color3", colorInfo.DefaultColor3);
        if (color3 is not null)
            hl.Color3 = CreateColor(color3);

        var color4 = GetAttribute(color, "color4", colorInfo.DefaultColor4);
        if (color4 is not null)
            hl.Color4 = CreateColor(color4);

        var italics = color.Attribute("italics") ?? color.Attribute("italic");
        if (italics is not null)
            hl.FontStyle = (bool)italics ? FontStyle.Italic : FontStyle.Normal;

        var bold = color.Attribute("bold");
        if (bold is not null)
            hl.FontWeight = (bool)bold ? FontWeight.Bold : FontWeight.Normal;

        return hl;
    }

    private ThemeColor CreateThemeColor(ColorType colorType)
    {
        var hl = new ThemeColor { Name = colorType.ToString() };

        var colorInfo = ColorInfos[ToIndex(colorType)];

        if (colorInfo.DefaultForeground is not null)
            hl.Foreground = CreateColor(colorInfo.DefaultForeground);

        if (colorInfo.DefaultBackground is not null)
            hl.Background = CreateColor(colorInfo.DefaultBackground);

        if (colorInfo.DefaultColor3 is not null)
            hl.Color3 = CreateColor(colorInfo.DefaultColor3);

        if (colorInfo.DefaultColor4 is not null)
            hl.Color4 = CreateColor(colorInfo.DefaultColor4);

        return hl;
    }

    private static string? GetAttribute(XElement xml, string attr, string? defVal)
    {
        var a = xml.Attribute(attr);
        if (a is not null)
            return a.Value;

        return defVal;
    }

    private static Brush? CreateColor(string color)
    {
        if (color.StartsWith("SystemColors."))
        {
            string shortName = color[13..];
            var property = typeof(SystemColors).GetProperty(shortName + "Brush");
            Debug2.Assert(property is not null);
            if (property is null)
                return null;

            return (Brush)property.GetValue(null, null)!;
        }

        try
        {
            var clr = (Avalonia.Media.Color?)ColorConverter.ConvertFromInvariantString(color);
            return clr is null ? null : new SolidColorBrush(clr.Value);
        }
        catch
        {
            Debug.Fail($"Couldn't convert color '{color}'");
            throw;
        }
    }

    private static ColorType ToColorType(string name)
    {
        if (NameToColorType.TryGetValue(name, out var type))
            return type;

        Debug.Fail($"Invalid color found: {name}");
        return ColorType.LastUI;
    }

    public override string ToString() => $"Theme: {Guid} {MenuName}";
}