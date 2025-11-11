using System;
using System.Collections.Generic;
using System.Windows;
using Kaxaml.Plugins.Default;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kaxaml;

public partial class App
{
    private static readonly IEnumerable<Type> DiTypes =
    [
        typeof(MainWindow),
        typeof(App),
        typeof(AssemblyCacheManager),
        typeof(References),
    ];

    private ILogger<App> _logger = NullLogger<App>.Instance;

    public Snippets? Snippets { get; set; }

    public static string[] StartupArgs { get; private set; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupArgs = e.Args;

        ApplicationDiServiceProvider.Initialize(DiTypes);
        _logger = ApplicationDiServiceProvider
            .Services
            .GetRequiredService<ILogger<App>>();

        _logger.LogInformation("***** STARTUP  *****");
        _logger.LogInformation(
            "Application is starting with Main Window at {Stamp}...",
            DateTime.Now);

        ApplicationDiServiceProvider
            .Services
            .GetRequiredService<MainWindow>()
            .Show();
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