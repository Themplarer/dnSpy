using System;
using System.ComponentModel;
using dnSpy.Contracts.Settings;

namespace MyApp.Decompiler;

internal sealed class DecompilerServiceSettingsImpl : DecompilerServiceSettings
{
    private static readonly Guid SettingsGuid = new("6A7E565D-DC09-4AAE-A7C8-E86A835FCBFC");

    private readonly ISettingsService _settingsService;

    public DecompilerServiceSettingsImpl(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        var sect = settingsService.GetOrCreateSection(SettingsGuid);
        LanguageGuid = sect.Attribute<Guid?>(nameof(LanguageGuid)) ?? LanguageGuid;
        PropertyChanged += DecompilerServiceSettingsImpl_PropertyChanged;
    }

    void DecompilerServiceSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var sect = _settingsService.RecreateSection(SettingsGuid);
        sect.Attribute(nameof(LanguageGuid), LanguageGuid);
    }
}