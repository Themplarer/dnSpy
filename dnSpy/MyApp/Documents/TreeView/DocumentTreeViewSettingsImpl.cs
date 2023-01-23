using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.TreeView;

sealed class DocumentTreeViewSettingsImpl : DocumentTreeViewSettings
{
    static readonly Guid SETTINGS_GUID = new Guid("3E04ABE0-FD5E-4938-B40C-F86AA0FA377D");

    readonly ISettingsService settingsService;

    public DocumentTreeViewSettingsImpl(ISettingsService settingsService)
    {
        this.settingsService = settingsService;

        var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
        SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
        SingleClickExpandsTreeViewChildren = sect.Attribute<bool?>(nameof(SingleClickExpandsTreeViewChildren)) ?? SingleClickExpandsTreeViewChildren;
        ShowAssemblyVersion = sect.Attribute<bool?>(nameof(ShowAssemblyVersion)) ?? ShowAssemblyVersion;
        ShowAssemblyPublicKeyToken = sect.Attribute<bool?>(nameof(ShowAssemblyPublicKeyToken)) ?? ShowAssemblyPublicKeyToken;
        ShowToken = sect.Attribute<bool?>(nameof(ShowToken)) ?? ShowToken;
        DeserializeResources = sect.Attribute<bool?>(nameof(DeserializeResources)) ?? DeserializeResources;
        FilterDraggedItems = sect.Attribute<DocumentFilterType?>(nameof(FilterDraggedItems)) ?? FilterDraggedItems;
        MemberKind0 = sect.Attribute<MemberKind?>(nameof(MemberKind0)) ?? MemberKind0;
        MemberKind1 = sect.Attribute<MemberKind?>(nameof(MemberKind1)) ?? MemberKind1;
        MemberKind2 = sect.Attribute<MemberKind?>(nameof(MemberKind2)) ?? MemberKind2;
        MemberKind3 = sect.Attribute<MemberKind?>(nameof(MemberKind3)) ?? MemberKind3;
        MemberKind4 = sect.Attribute<MemberKind?>(nameof(MemberKind4)) ?? MemberKind4;
        PropertyChanged += DocumentTreeViewSettingsImpl_PropertyChanged;
    }

    void DocumentTreeViewSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var sect = settingsService.RecreateSection(SETTINGS_GUID);
        sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
        sect.Attribute(nameof(SingleClickExpandsTreeViewChildren), SingleClickExpandsTreeViewChildren);
        sect.Attribute(nameof(ShowAssemblyVersion), ShowAssemblyVersion);
        sect.Attribute(nameof(ShowAssemblyPublicKeyToken), ShowAssemblyPublicKeyToken);
        sect.Attribute(nameof(ShowToken), ShowToken);
        sect.Attribute(nameof(DeserializeResources), DeserializeResources);
        sect.Attribute(nameof(FilterDraggedItems), FilterDraggedItems);
        sect.Attribute(nameof(MemberKind0), MemberKind0);
        sect.Attribute(nameof(MemberKind1), MemberKind1);
        sect.Attribute(nameof(MemberKind2), MemberKind2);
        sect.Attribute(nameof(MemberKind3), MemberKind3);
        sect.Attribute(nameof(MemberKind4), MemberKind4);
    }
}