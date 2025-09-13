using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Plugins;

public partial class About
{
    private readonly ILogger<About> _logger;

    public About()
    {
        InitializeComponent();
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<About>>();
        AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(HandleRequestNavigate), false);
        Loaded += About_Loaded;
        _logger.LogInformation("Initializing About Plugin complete.");
    }

    private void About_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= About_Loaded;
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionTextBlock.Text = version is not null
            ? $"v{version.Major}.{version.Minor}.{version.Build}"
            : "UNKNOWN";

        _logger.LogInformation(
            "Loaded About Plugin complete with Version: {Version}",
            VersionTextBlock.Text);
    }

    private void HandleRequestNavigate(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Hyperlink hl) return;
        var navigateUri = hl.NavigateUri.ToString();
        _logger.LogInformation("Launch URL: {Url}", navigateUri);

        try
        {
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open URL: {Url}", navigateUri);
            MessageBox.Show(
                $"Unable to open URL:\n{ex.Message}",
                "Open URL Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void TempFolderButton_Click(object _, RoutedEventArgs __)
    {
        OpenFolder(ApplicationDiServiceProvider.TempDirectory);
    }

    private void LogFilesButton_Click(object _, RoutedEventArgs __)
    {
        OpenFolder(ApplicationDiServiceProvider.LogDirectory);
    }

    private void OpenFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            _logger.LogWarning("Missing folder: {Folder}", folder);
            MessageBox.Show(
                $"The folder \"{folder}\" does not exist.",
                "Missing Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        else
        {
            _logger.LogInformation("Opening log folder: {Folder}", folder);

            try
            {
                Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to open log folder: {Folder}",
                    folder);

                MessageBox.Show(
                    $"Unable to open log folder:\n{ex.Message}",
                    "Open Folder Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}