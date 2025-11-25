using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Kaxaml.Documents;
using Kaxaml.Plugins;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.DependencyInjection.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kaxaml;

public partial class App
{
    private static readonly IEnumerable<IDiRegistration> DiRegistrations =
    [
        new TypeDiRegistration<MainWindow>(),
        new TypeDiRegistration<App>(),
        new TypeDiRegistration<AssemblyCacheManager>(),
        new TypeDiRegistration<AssemblyReferences>(),
        new TypeDiRegistration<XamlDocumentManager>(),
        new TypeDiRegistration<About>(),
        new TypeDiRegistration<Find>(),
        new TypeDiRegistration<Settings>(),
        new TypeDiRegistration<Snippets>(),
        new TypeDiRegistration<SnippetEditor>()
    ];

    private ILogger<App> _logger = NullLogger<App>.Instance;

    public Snippets? Snippets { get; set; }

    public static string[] StartupArgs { get; private set; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupArgs = e.Args;

        ApplicationDiServiceProvider.Initialize(DiRegistrations);
        _logger = ApplicationDiServiceProvider
            .Services
            .GetRequiredService<ILogger<App>>();

        _logger.LogInformation("***** STARTUP  *****");
        _logger.LogInformation(
            "Application is starting with Main Window at {Stamp}...",
            DateTime.Now);

        //Show the main window after wiring for linking to the snippets editor
        var mainWindow = ApplicationDiServiceProvider
            .Services
            .GetRequiredService<MainWindow>();

        mainWindow.Closing += MainWindow_OnClosing;
        mainWindow.Show();
    }

    private void MainWindow_OnClosing(object? _, CancelEventArgs __)
    {
        _logger.LogInformation("Main Window closing...");

        //Manually call close in case the window is never shown
        ApplicationDiServiceProvider
            .Services
            .GetRequiredService<SnippetEditor>()
            .Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInformation("Application shutdown started...");
        ApplicationDiServiceProvider.Shutdown().GetAwaiter().GetResult();
        base.OnExit(e);

        _logger.LogInformation(
            "Application shutdown complete at {Stamp}.",
            DateTime.Now);
        _logger.LogInformation("***** SHUTDOWN  *****");
    }
}