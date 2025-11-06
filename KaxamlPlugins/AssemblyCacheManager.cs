using System.IO;
using System.Reflection;
using KaxamlPlugins.DependencyInjection;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KaxamlPlugins;

/// <summary>
/// Manages a collection assemblies currently in the AppDomain in a thread-safe manner. 
/// </summary>
/// <remarks>
/// This is optimized to not fire the <see cref="CacheUpdated"/> event repeatedly
/// when it is loading via <see cref="LoadAssembly"/>.  However, since it also
/// listens for Assemblies being loaded via the AppDomain, the event will fire
/// each time any single Assembly is loaded by external forces.
/// </remarks>
public sealed class AssemblyCacheManager
{
    private readonly HashSet<Assembly> _assemblyCache;
    private readonly object _assemblyCacheLock;

    private readonly ILogger _logger;
    private bool _isLoadingAssembly;

    /// <summary>
    /// Create a new instance of the cache with currently loaded assemblies.
    /// </summary>
    public AssemblyCacheManager()
    {
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<AssemblyCacheManager>>();
        _assemblyCacheLock = new object();
        _assemblyCache = AppDomain
            .CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToHashSet();

        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_OnAssemblyLoad;
        _logger.LogInformation("Created with {Count} loaded Assemblies.", _assemblyCache.Count);
    }

    /// <summary>
    /// Loads an Assembly from file AND all of its dependant Assemblies.
    /// </summary>
    /// <param name="fileInfo">File info of the root assembly.</param>
    /// <returns>Reference to the loaded root assembly.</returns>
    /// <exception cref="FileLoadException">Thrown if loading the root or any of its dependencies fail.</exception>
    public Assembly LoadAssembly(FileInfo fileInfo)
    {
        var fullName = fileInfo.FullName;
        IList<Assembly> loadedAssemblies;

        lock (_assemblyCacheLock)
        {
            _isLoadingAssembly = true;
        }

        try
        {
            _logger.LogInformation("Loading root assembly: {FullName}", fullName);
            loadedAssemblies = AssemblyUtilities.LoadDependencyTree(fileInfo);
        }
        catch (Exception ex)
        {
            throw new FileLoadException($"Could not load file: {fullName}", ex);
        }
        finally
        {
            lock (_assemblyCacheLock)
            {
                _isLoadingAssembly = false;
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var loadedAssembly in loadedAssemblies)
            {
                _logger.LogDebug("Loading dependent assembly: {FullName}", loadedAssembly.FullName);
            }
        }

        CacheUpdated?.Invoke(this, EventArgs.Empty);
        return loadedAssemblies.First();
    }

    /// <summary>
    /// Generates a static copy of the current cache.
    /// </summary>
    /// <returns>Detached copy of the internal cache.</returns>
    public IList<Assembly> SnapshotCurrentAssemblies()
    {
        lock (_assemblyCacheLock)
        {
            return _assemblyCache.ToList();
        }
    }

    /// <summary>
    /// Fires when the cache of Assemblies changes.
    /// </summary>
    public event EventHandler? CacheUpdated;

    /// <summary>
    /// Adds newly loaded assemblies to the internal cache.
    /// </summary>
    private void CurrentDomain_OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        bool isInvoke;
        lock (_assemblyCacheLock)
        {
            _assemblyCache.Add(args.LoadedAssembly);
            isInvoke = !_isLoadingAssembly;
        }

        if (isInvoke)
            CacheUpdated?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation(
            "{Sender} added assembly (event {WasInvoked}): {Name}",
            sender?.GetType().Name,
            isInvoke ? "invoked" : "suppressed",
            args.LoadedAssembly.FullName);
    }
}