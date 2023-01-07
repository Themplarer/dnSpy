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
using System.Diagnostics;
using System.Reflection;
using Avalonia.Threading;

namespace dnSpy.Debugger.DotNet.UI;

sealed class UIDispatcher
{
    static readonly FieldInfo? _disableProcessingCountFieldInfo;

    static UIDispatcher()
    {
        _disableProcessingCountFieldInfo = typeof(Dispatcher).GetField("_disableProcessingCount", BindingFlags.NonPublic | BindingFlags.Instance);
        Debug2.Assert(_disableProcessingCountFieldInfo is not null && _disableProcessingCountFieldInfo.FieldType == typeof(int));
    }

    private UIDispatcher() => Dispatcher = Dispatcher.UIThread;

    Dispatcher Dispatcher { get; }

    public void VerifyAccess() => Dispatcher.VerifyAccess();

    public bool CheckAccess() => Dispatcher.CheckAccess();

    public bool IsProcessingDisabled()
    {
        if (_disableProcessingCountFieldInfo is null || _disableProcessingCountFieldInfo.FieldType != typeof(int))
            return false;

        return (int)_disableProcessingCountFieldInfo.GetValue(Dispatcher)! > 0;
    }

    public void Invoke(Action callback)
    {
        System.Diagnostics.Debugger.NotifyOfCrossThreadDependency();
        Dispatcher.Post(callback, DispatcherPriority.Background);
    }

    public void UIBackground(Action callback) =>
        Dispatcher.Post(callback, DispatcherPriority.Background);

    public void UI(Action callback) =>
        Dispatcher.Post(callback, DispatcherPriority.Send);
}