using System;
using System.Windows;
using System.Data;
using System.Xml;
using System.Configuration;
using System.Collections;
using System.Text.RegularExpressions;
using Kaxaml.Properties;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.IO;
using Kaxaml.Plugins.Default;

namespace Kaxaml
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        public Snippets? Snippets { get; set; }

        private void app_Startup(object sender, StartupEventArgs e)
        {
            StartupArgs = e.Args;
        }


        [PreserveSig()]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetModuleFileName
        (
            [In]
            HandleRef module,

            [Out]
            StringBuilder buffer,

            [In]
            [MarshalAs(UnmanagedType.U4)]int capacity
        );

        public static string[] StartupArgs { get; private set; } = Array.Empty<string>();

        private static string? _startupPath;
        public static string? StartupPath
        {
            get
            {
                // Only retrieve startup path when
                // it wans’t known.
                if (_startupPath == null)
                {
                    var nullHandle =
                        new HandleRef(null, IntPtr.Zero);

                    var buffer =
                        new StringBuilder(260);

                    var lenght = GetModuleFileName(
                        nullHandle,
                        buffer,
                        buffer.Capacity);

                    if (lenght == 0)
                    {
                        // This ctor overload uses 
                        // GetLastWin32Error to
                        //get its error code.
                        throw new Win32Exception();
                    }

                    var moduleFilename =
                        buffer.ToString(0, lenght);
                    _startupPath =
                        Path.GetDirectoryName(moduleFilename);
                }

                return _startupPath;
            }
        }
    }
}