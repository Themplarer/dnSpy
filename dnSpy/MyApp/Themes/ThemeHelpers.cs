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

using dnSpy.Contracts.Themes;

namespace MyApp.Themes;

static class ThemeHelpers
{
    public static string GetMenuName(this ITheme theme)
    {
        var name = theme.MenuName;
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        name = theme.Name;
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return theme.Guid.ToString();
    }

    public static string GetName(this ITheme theme)
    {
        var name = theme.GetMenuName();
        const string underscoreTmpReplacement = "<<<<<<>>>>>>";
        return name.Replace("__", underscoreTmpReplacement).Replace("_", "").Replace(underscoreTmpReplacement, "_");
    }
}