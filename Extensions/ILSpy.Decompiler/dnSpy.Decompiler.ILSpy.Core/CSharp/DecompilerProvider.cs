using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Decompiler.ILSpy.Core.Settings;

namespace dnSpy.Decompiler.ILSpy.Core.CSharp;

internal sealed class DecompilerProvider : IDecompilerProvider
{
    private readonly DecompilerSettingsService _decompilerSettingsService;

    // Keep the default ctor. It's used by dnSpy.Console.exe
    public DecompilerProvider() : this(DecompilerSettingsService.__Instance_DONT_USE)
    {
    }

    public DecompilerProvider(DecompilerSettingsService decompilerSettingsService)
    {
        Debug2.Assert(decompilerSettingsService is not null);
        _decompilerSettingsService = decompilerSettingsService ?? throw new ArgumentNullException(nameof(decompilerSettingsService));
    }

    public IEnumerable<IDecompiler> Create()
    {
        yield return new CSharpDecompiler(_decompilerSettingsService.CSharpVBDecompilerSettings, DecompilerConstants.CSHARP_ILSPY_ORDERUI);
#if DEBUG
        foreach (var l in CSharpDecompiler.GetDebugDecompilers(_decompilerSettingsService.CSharpVBDecompilerSettings))
            yield return l;
#endif
    }
}