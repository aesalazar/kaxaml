using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Kaxaml.Controls;
using KaxamlPlugins;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Kaxaml.Plugins.Default;

/// <summary>
/// Manages a collection of externally referenced Assemblies.
/// </summary>
public partial class References
{
    private readonly HashSet<Reference> _addedReferences = [];
    private readonly AssemblyCacheManager _assemblyCacheManager;
    private readonly ILogger<About> _logger;

    private OpenFileDialog? _openReferencesDialog;

    public References()
    {
        InitializeComponent();
        _assemblyCacheManager = ApplicationDiServiceProvider.Services.GetRequiredService<AssemblyCacheManager>();
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<About>>();
        _logger.LogInformation("Initializing References Plugin complete.");
    }

    /// <summary>
    /// Collection of Assembly referenced added by the user.
    /// </summary>
    public ObservableCollection<Reference> AllReferences { get; } = [];

    /// <summary>
    /// Prompts the user to select external Assembly files.
    /// </summary>
    private void AddReferencesButton_OnClick(object _, RoutedEventArgs __)
    {
        _logger.LogDebug("Showing File Open Dialog...");
        _openReferencesDialog ??= new OpenFileDialog
        {
            AddExtension = true,
            DefaultExt = ".dll",
            Filter = "Component files (*.dll;*.exe)|*.dll;*.exe|All files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
            CheckPathExists = true,
            RestoreDirectory = true
        };

        if (_openReferencesDialog.ShowDialog() is not true)
        {
            _logger.LogDebug("User canceled file open.");
            return;
        }

        var runtime = AssemblyUtilities.CurrentRuntimeVersion;
        _logger.LogInformation(
            "Loading file(s) under current runtime {Runtime}: {files}",
            runtime.ToString(),
            string.Join(" | ", _openReferencesDialog.FileNames));

        foreach (var s in _openReferencesDialog.FileNames)
        {
            var fileInfo = new FileInfo(s);
            Version? fileRuntime;

            try
            {
                fileRuntime = AssemblyUtilities.ExtractTargetRuntime(fileInfo);
            }
            catch (Exception ex)
            {
                fileRuntime = null;
                _logger.LogError(
                    "Could read runtime for file '{File}': {Error}",
                    fileInfo.Name,
                    ex);
            }

            MessageBoxResult result;
            if (fileRuntime is null)
            {
                result = MessageBox.Show(
                    $"Could not determine Target Runtime for '{fileInfo.Name
                    }'.{Environment.NewLine}{Environment.NewLine
                    }Do you still want to attempt loading?",
                    "Target Runtime Unavailable",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            }
            else if (fileRuntime > runtime)
            {
                var msg = $"Target Runtime for '{fileInfo.Name}' is .NET {fileRuntime.ToString()
                } which is higher than the current runtime {runtime.ToString()
                } and cannot be loaded.";
                _logger.LogError(msg);

                result = MessageBox.Show(
                    msg,
                    "Cannot Load File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                result = MessageBoxResult.Yes;
            }

            if (result is not MessageBoxResult.Yes) continue;
            if (AddNewReferences(s))
            {
                _logger.LogInformation("Reference loaded: {Name}", s);
            }
            else
            {
                _logger.LogError("Reference not loaded: {Name}", s);
            }
        }
    }

    /// <summary>
    /// Attempts to load an Assembly from file and add to the collection.
    /// </summary>
    /// <param name="filePath">Full file path to the Assembly.</param>
    /// <returns>Indication of Assembly being successfully loaded.</returns>
    /// <exception cref="FileLoadException">Throw if something goes wrong when loading.</exception>
    private bool AddNewReferences(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        var fileInfo = new FileInfo(filePath);

        try
        {
            var reference = new Reference(fileInfo, _assemblyCacheManager, _logger);

            if (!_addedReferences.Add(reference)) return false;
            AllReferences.Add(reference);
            _logger.LogDebug("Adding: {Ref}", reference.FullName);
        }
        catch (Exception ex)
        {
            if (ex.IsCriticalException())
            {
                _logger.LogCritical(
                    "Fatal exception loading file '{File}': {Ex}",
                    filePath, ex);
                throw new FileLoadException($"Could not load file {filePath}", ex);
            }

            MessageBox.Show(
                $"Error loading file '{fileInfo.Name}': {ex.Message}",
                "Cannot Load File",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _logger.LogError("Error loading file '{File}': {Ex}", filePath, ex);
        }

        return true;
    }
}