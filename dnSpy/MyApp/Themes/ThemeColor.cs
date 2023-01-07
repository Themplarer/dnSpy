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
using Avalonia.Media;
using dnSpy.Contracts.Themes;

namespace MyApp.Themes;

sealed class ThemeColor : IThemeColor
{
    public string Name { get; set; } = null!;

    public Brush? Foreground { get; set; }

    public Brush? Background { get; set; }

    public Brush? Color3 { get; set; }

    public Brush? Color4 { get; set; }

    public FontStyle? FontStyle { get; set; }

    public FontWeight? FontWeight { get; set; }

    public Brush? GetBrushByIndex(int index) =>
        index switch
        {
            0 => Foreground,
            1 => Background,
            2 => Color3,
            3 => Color4,
            _ => throw new ArgumentOutOfRangeException()
        };
}