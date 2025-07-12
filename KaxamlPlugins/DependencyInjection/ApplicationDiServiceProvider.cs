using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace KaxamlPlugins.DependencyInjection;

/// <summary>
/// Provides static references to the DI Container.
/// </summary>
/// <remarks>
/// This somewhat violates the DI principal, but it is the best that can
/// be done for now given the way plugins are created and loaded.
/// </remarks>
public static class ApplicationDiServiceProvider
{
    private static IHost? _host;

    /// <summary>
    /// Logging folder for the application.
    /// </summary>
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Kaxaml", "Logs");

    /// <summary>
    /// Allows reference to DI container from UI Controls.
    /// </summary>
    /// <remarks>
    /// Slightly violates DI principles but best that can be done for now.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if <see cref="Initialize"/> has not been called.</exception>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Builds the DI Container and the Logger.
    /// </summary>
    /// <param name="typesToRegister">Types to register as singletons.</param>
    /// <exception cref="Exception">Thrown if already called.</exception>
    public static void Initialize(IEnumerable<Type> typesToRegister)
    {
        if (_host is not null)
            throw new Exception("DI Host has already been initialized");
        if (LogManager.Configuration is null)
            throw new Exception("Log Configuration has not been loaded");

        LogManager.Configuration.Variables["logDir"] = LogDirectory;

        _host = Host
            .CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddNLog();
            })
            .ConfigureServices((_, services) =>
            {
                foreach (var type in typesToRegister) services.AddSingleton(type);
            })
            .Build();

        Services = _host.Services;
    }

    /// <summary>
    /// Stops the DI Container.
    /// </summary>
    public static async Task Shutdown()
    {
        if (_host is null) return;
        await _host.StopAsync();
        _host.Dispose();
    }
}