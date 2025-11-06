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
    private readonly MainWindow _mainWindow;

    private OpenFileDialog? _openReferencesDialog;

    public References()
    {
        InitializeComponent();
        _mainWindow = ((MainWindow?)KaxamlInfo.MainWindow)!;
        _assemblyCacheManager = ApplicationDiServiceProvider.Services.GetRequiredService<AssemblyCacheManager>();
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<About>>();
        _logger.LogInformation("Initializing References Plugin complete.");
    }

    /// <summary>
    /// Collection of Assembly referenced added by the user.
    /// </summary>
    public ObservableCollection<Reference> AllReferences { get; } = [];

    /// <summary>
    /// Attempts to load an Assembly from file and add to the collection.
    /// </summary>
    /// <param name="filePath">Full file path to the Assembly.</param>
    /// <returns>Indication of Assembly being successfully loaded.</returns>
    /// <exception cref="FileLoadException">Throw if something goes wrong when loading.</exception>
    public bool AddNewReferences(string filePath)
    {
        if (!File.Exists(filePath)) return false;

        try
        {
            var fileInfo = new FileInfo(filePath);
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

            _mainWindow.DocumentsView?.SelectedView?.ReportError(ex);
            _logger.LogError("Error loading file '{File}': {Ex}", filePath, ex);
        }

        return true;
    }

    private void AddReferencesButton_OnClick(object _, RoutedEventArgs __)
    {
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

        if (_openReferencesDialog.ShowDialog() is not true) return;
        foreach (var s in _openReferencesDialog.FileNames)
        {
            AddNewReferences(s);
        }
    }
}