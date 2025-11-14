using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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
    private readonly Dictionary<string, Assembly> _assemblyCache;
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
            .Where(asm => !asm.IsDynamic)
            .ToDictionary(asm => GetAssemblyHash(asm.Location), asm => asm);

        AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_OnAssemblyLoad;
        _logger.LogInformation("Created with {Count} loaded Assemblies.", _assemblyCache.Count);
    }

    /// <summary>
    /// Loads an Assembly from file AND all of its dependant Assemblies if not already in the cache.
    /// </summary>
    /// <param name="fileInfo">File info of the root assembly.</param>
    /// <returns>Reference to the loaded root assembly.</returns>
    /// <exception cref="FileLoadException">Thrown if loading the root or any of its dependencies fail.</exception>
    public Assembly LoadAssembly(FileInfo fileInfo)
    {
        var hash = GetAssemblyHash(fileInfo.FullName);
        lock (_assemblyCacheLock)
        {
            if (_assemblyCache.TryGetValue(hash, out var assembly)) return assembly;
            _isLoadingAssembly = true;
        }

        IList<Assembly> loadedAssemblies;
        try
        {
            _logger.LogInformation("Loading root assembly: {FullName} ({Hash})", fileInfo.Name, hash);
            loadedAssemblies = AssemblyUtilities.LoadDependencyTree(fileInfo);
        }
        catch (Exception ex)
        {
            throw new FileLoadException($"Could not load file '{fileInfo.Name}': {ex.Message}", ex);
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
                _logger.LogDebug("Loaded assembly: {FullName}", loadedAssembly.FullName);
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
        List<Assembly> list;
        lock (_assemblyCacheLock)
        {
            list = _assemblyCache.Values.ToList();
        }

        _logger.LogDebug("Snapshot created with count: {Count}", list.Count);
        return list;
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
        var hash = GetAssemblyHash(args.LoadedAssembly.Location);

        lock (_assemblyCacheLock)
        {
            _assemblyCache[hash] = args.LoadedAssembly;
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

    private static string GetAssemblyHash(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}