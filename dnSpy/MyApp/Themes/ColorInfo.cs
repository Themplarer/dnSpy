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
using Avalonia;
using Avalonia.Media;
using dnSpy.Contracts.Themes;

namespace MyApp.Themes;

[DebuggerDisplay("{ColorType}, Children={Children.Length}")]
abstract class ColorInfo
{
    private ColorInfo[] _children = Array.Empty<ColorInfo>();

    public readonly ColorType ColorType;
    public readonly string Description;
    public string? DefaultForeground;
    public string? DefaultBackground;
    public string? DefaultColor3;
    public string? DefaultColor4;
    public ColorInfo? Parent;

    public ColorInfo[] Children
    {
        get => _children;
        set
        {
            _children = value;

            foreach (var child in _children)
                child.Parent = this;
        }
    }

    public abstract IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor);

    protected ColorInfo(ColorType colorType, string description)
    {
        ColorType = colorType;
        Description = description;
    }
}

sealed class ColorColorInfo : ColorInfo
{
    public object? BackgroundResourceKey;
    public object? ForegroundResourceKey;

    public ColorColorInfo(ColorType colorType, string description)
        : base(colorType, description) => ForegroundResourceKey = null;

    public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor)
    {
        if (ForegroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Foreground is not null);
            yield return (ForegroundResourceKey, ((SolidColorBrush)hlColor.Foreground).Color);
        }

        if (BackgroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Background is not null);
            yield return (BackgroundResourceKey, ((SolidColorBrush)hlColor.Background).Color);
        }
    }
}

sealed class BrushColorInfo : ColorInfo
{
    public object? BackgroundResourceKey;
    public object? ForegroundResourceKey;

    public BrushColorInfo(ColorType colorType, string description)
        : base(colorType, description)
    {
    }

    public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor)
    {
        if (ForegroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Foreground is not null);
            yield return (ForegroundResourceKey, hlColor.Foreground);
        }

        if (BackgroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Background is not null);
            yield return (BackgroundResourceKey, hlColor.Background);
        }
    }
}

sealed class DrawingBrushColorInfo : ColorInfo
{
    public object? BackgroundResourceKey;
    public object? ForegroundResourceKey;
    public bool IsHorizontal;

    public DrawingBrushColorInfo(ColorType colorType, string description)
        : base(colorType, description) => ForegroundResourceKey = null;

    public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor)
    {
        if (ForegroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Foreground is not null);
            var brush = hlColor.Foreground;
            yield return (ForegroundResourceKey, CreateDrawingBrush(brush));
        }

        if (BackgroundResourceKey is not null)
        {
            Debug2.Assert(hlColor.Background is not null);
            var brush = hlColor.Background;
            yield return (BackgroundResourceKey, CreateDrawingBrush(brush));
        }
    }

    Brush CreateDrawingBrush(Brush brush) => brush;
}

sealed class LinearGradientColorInfo : ColorInfo
{
    public object? ResourceKey;
    public Point StartPoint;
    public Point EndPoint;
    public double[] GradientOffsets;

    public LinearGradientColorInfo(ColorType colorType, Point endPoint, string description, params double[] gradientOffsets)
        : this(colorType, new Point(0, 0), endPoint, description, gradientOffsets)
    {
    }

    public LinearGradientColorInfo(ColorType colorType, Point startPoint, Point endPoint, string description, params double[] gradientOffsets)
        : base(colorType, description)
    {
        StartPoint = startPoint;
        EndPoint = endPoint;
        GradientOffsets = gradientOffsets;
    }

    public override IEnumerable<(object?, object)> GetResourceKeyValues(ThemeColor hlColor)
    {
        var br = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(StartPoint, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(EndPoint, RelativeUnit.Absolute),
        };

        for (var i = 0; i < GradientOffsets.Length; i++)
        {
            var gs = new GradientStop(((SolidColorBrush)hlColor.GetBrushByIndex(i)!).Color, GradientOffsets[i]);
            br.GradientStops.Add(gs);
        }

        yield return (ResourceKey, br);
    }
}