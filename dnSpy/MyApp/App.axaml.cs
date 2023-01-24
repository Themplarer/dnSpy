using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.DnSpy.Metadata;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.TreeView.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.CSharp;
using dnSpy.Documents.TreeView;
using dnSpy.TreeView;
using MyApp.Decompiler;
using MyApp.Documents;
using MyApp.Documents.TreeView;
using MyApp.Documents.TreeView.Resources;
using MyApp.Images;
using MyApp.Settings;
using MyApp.Themes;
using MyApp.ViewModels;
using MyApp.Views;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace MyApp;

public partial class App : Application
{
    public IHost AppHost { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        SetUpDIContainer();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = Locator.Current.GetService<MainWindow>()!;
            mainWindow.DataContext = Locator.Current.GetService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ReSharper disable once InconsistentNaming
    private void SetUpDIContainer() =>
        AppHost = Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.UseMicrosoftDependencyResolver();
                var resolver = Locator.CurrentMutable;
                resolver.InitializeSplat();
                resolver.InitializeReactiveUI();

                RegisterDependencies(services);
            })
            .Build();

    private static void RegisterDependencies(IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<AssemblyExplorerMostRecentlyUsedList, AssemblyExplorerMostRecentlyUsedListImpl>();
        services.AddTransient<ISettingsService, SettingsService2>();

        services.AddTransient<IDocumentTreeView, DocumentTreeView>();

        services.AddTransient<ITreeViewService, TreeViewService>();

        services.AddTransient<IThemeService, ThemeService>();
        services.AddTransient<ThemeSettings>();

        // services.AddTransient<ModulePETreeNodeDataProvider>();
        // services.AddTransient<PEFilePETreeNodeDataProvider>();
        // services.AddTransient<IHexBufferService, HexBufferService>();
        // services.AddTransient<PEStructureProviderFactory, PEStructureProviderFactoryImpl>();
        // services.AddTransient<HexBufferFileServiceFactory, HexBufferFileServiceFactoryImpl>();
        // services.AddTransient(p =>
        //     new ITreeNodeDataProvider[] { p.GetService<ModulePETreeNodeDataProvider>()!, p.GetService<PEFilePETreeNodeDataProvider>()! }
        //         .Select(t => new Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>(() => t,
        //             t.GetType().GetCustomAttribute<ExportTreeNodeDataProviderAttribute>()!)));
        services.AddTransient(_ => Enumerable.Empty<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>());

        services.AddTransient<IDecompilerService, DecompilerService>();
        services.AddTransient<IDocumentTreeViewSettings, DocumentTreeViewSettingsImpl>();
        services.AddTransient<DecompilerServiceSettingsImpl>();
        services.AddTransient(_ => Enumerable.Empty<IDecompiler>());
        services.AddTransient<IEnumerable<IDecompilerCreator>>(p => new[] { new MyDecompilerCreator(p.GetService<DecompilerSettingsService>()!) });

        services.AddTransient<IDsDocumentService, DsDocumentService>();
        services.AddTransient<IDsDocumentServiceSettings, DsDocumentServiceSettingsImpl>();
        services.AddTransient<IDsDocumentProvider, DefaultDsDocumentProvider>();
        services.AddTransient<RuntimeAssemblyResolver>();
        services.AddTransient(p => new[] { p.GetService<RuntimeAssemblyResolver>()! }
            .Select(r => new Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>(() => r,
                r.GetType().GetCustomAttribute<ExportRuntimeAssemblyResolverAttribute>()!)));
        services.AddTransient<DecompilerSettingsService>();

        services.AddTransient<IDotNetImageService, DotNetImageService>();
        services.AddTransient<IResourceNodeFactory, ResourceNodeFactory>();
        services.AddTransient<DefaultDsDocumentNodeProvider>();
        services.AddTransient(p => new[] { p.GetService<DefaultDsDocumentNodeProvider>()! }
            .Select(r => new Lazy<IDsDocumentNodeProvider, IDsDocumentNodeProviderMetadata>(() => r,
                r.GetType().GetCustomAttribute<ExportDsDocumentNodeProviderAttribute>()!)));

        // services.AddTransient<HexDocumentTreeNodeDataFinder>();
        // services.AddTransient(p => new[] { p.GetService<HexDocumentTreeNodeDataFinder>()! }
        //     .Select(r => new Lazy<IDocumentTreeNodeDataFinder, IDocumentTreeNodeDataFinderMetadata>(() => r,
        //         r.GetType().GetCustomAttribute<ExportDocumentTreeNodeDataFinderAttribute>()!)));
        services.AddTransient(_ => Enumerable.Empty<Lazy<IDocumentTreeNodeDataFinder, IDocumentTreeNodeDataFinderMetadata>>());
        services.AddTransient<ITreeViewNodeTextElementProvider>(_ => null!);

        services.AddTransient<DecompilationHandler>();
        services.AddTransient<DecompilationContext>();
        services.AddSingleton<IDecompilerOutput, StringBuilderDecompilerOutput>();
        services.AddTransient<IDecompiler>(p => new MyDecompilerCreator(p.GetService<DecompilerSettingsService>()!).Create().First());
    }
}