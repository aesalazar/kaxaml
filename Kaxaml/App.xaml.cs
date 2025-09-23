using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Kaxaml.Plugins.Default;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kaxaml;

public partial class App
{
    private static string? _startupPath;

    private static readonly IEnumerable<Type> DiTypes =
    [
        typeof(MainWindow),
        typeof(App)
    ];

    private ILogger<App> _logger = NullLogger<App>.Instance;

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