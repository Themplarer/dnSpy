using System;
using System.ComponentModel;

namespace MyApp.Decompiler;

internal interface IDecompilerServiceSettings : INotifyPropertyChanged
{
    Guid LanguageGuid { get; }
}