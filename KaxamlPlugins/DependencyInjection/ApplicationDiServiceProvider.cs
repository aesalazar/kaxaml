using System.Configuration;
using System.IO;
using System.Reflection;
using KaxamlPlugins.DependencyInjection.Registration;
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
    private const string ConfigurationKeyLogFolder = "Folder.AppData.Logs";
    private const string ConfigurationKeyTempFolder = "Folder.AppData.Temp";
    private static IHost? _host;

    /// <summary>
    /// Full path to where the application executable was launched from.
    /// </summary>
    public static string? StartupPath { get; } = Path.GetDirectoryName(
        Assembly.GetExecutingAssembly().Location);

    /// <summary>
    /// Full path to the Temp folder for the application.
    /// </summary>
    public static string TempDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ConfigurationManager.AppSettings[ConfigurationKeyTempFolder]
        ?? throw new ConfigurationErrorsException($"Missing app.config entry: {ConfigurationKeyTempFolder}"));

    /// <summary>
    /// Full path to the Logging folder for the application.
    /// </summary>
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ConfigurationManager.AppSettings[ConfigurationKeyLogFolder]
        ?? throw new ConfigurationErrorsException($"Missing app.config entry: {ConfigurationKeyLogFolder}"));

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
    public static void Initialize(IEnumerable<IDiRegistration> typesToRegister)
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
                foreach (var registration in typesToRegister)
                    registration.RegisterSingleton(services);
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