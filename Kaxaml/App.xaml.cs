using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Kaxaml.Plugins.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaxaml;

public partial class App
{
    private static string? _startupPath;
    private IHost? _host;

    public Snippets? Snippets { get; set; }

    public static string[] StartupArgs { get; private set; } = [];

    public static string? StartupPath
    {
        get
        {
            // Only retrieve startup path when it wans’t known.
            if (_startupPath == null)
            {
                var nullHandle = new HandleRef(null, IntPtr.Zero);
                var buffer = new StringBuilder(260);
                var length = GetModuleFileName(
                    nullHandle,
                    buffer,
                    buffer.Capacity);

                if (length == 0)
                    // This ctor overload uses 
                    // GetLastWin32Error to
                    //get its error code.
                    throw new Win32Exception();

                var moduleFilename = buffer.ToString(0, length);
                _startupPath = Path.GetDirectoryName(moduleFilename);
            }

            return _startupPath;
        }
    }

    //TODO: See if we can use instead
    //var x = System.Reflection.Assembly.GetExecutingAssembly().Location;
    [PreserveSig]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GetModuleFileName
    (
        [In] HandleRef module,
        [Out] StringBuilder buffer,
        [In] [MarshalAs(UnmanagedType.U4)] int capacity
    );

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupArgs = e.Args;

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<MainWindow>();
                services.AddSingleton<App>();
            })
            .Build();

        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application is starting.");

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}