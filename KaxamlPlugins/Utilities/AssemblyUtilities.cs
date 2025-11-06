using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace KaxamlPlugins.Utilities;

/// <summary>
/// Helper methods when working with Assemblies.
/// </summary>
public static class AssemblyUtilities
{
    /// <summary>
    /// Reads the Assembly version from in a preferred order.
    /// </summary>
    /// <param name="assembly">Source assembly file.</param>
    /// <param name="fileInfo">File info to get from IO as a last resort.</param>
    /// <returns>File version, if found, as a string.</returns>
    public static string? ExtractAssemblyVersion(Assembly assembly, FileInfo fileInfo)
    {
        var assemblyVersion = assembly.GetCustomAttribute<AssemblyVersionAttribute>();
        if (assemblyVersion is not null) return assemblyVersion.Version;

        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
        if (fileVersion is not null) return fileVersion.Version;

        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersion is not null) return infoVersion.InformationalVersion;

        return FileVersionInfo.GetVersionInfo(fileInfo.FullName).FileVersion;
    }

    /// <summary>
    /// Loads an Assembly file AND all dependent Assemblies.
    /// </summary>
    /// <param name="fileInfo">Root Assembly to load and analyze for dependants.</param>
    /// <returns>A collection of all successfully loaded Assemblies including the Root as the first.</returns>
    /// <remarks>
    /// Dynamic assemblies will not be in the file path and are therefore skipped.
    /// </remarks>
    public static IList<Assembly> LoadDependencyTree(FileInfo fileInfo)
    {
        var rootAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fileInfo.FullName);
        var loadedAssembliesLookup = new HashSet<Assembly> { rootAssembly };
        var loadedAssemblies = new List<Assembly> { rootAssembly };
        LoadDependentAssemblies(
            rootAssembly,
            fileInfo.DirectoryName.ShouldNotBeNull("Assembly was loaded via full path."),
            loadedAssembliesLookup,
            loadedAssemblies);

        return loadedAssemblies;
    }

    /// <summary>
    /// Recursively loaded assemblies and any dependent assemblies.
    /// </summary>
    /// <param name="rootAssembly">Parent assembly to analyze (but not load).</param>
    /// <param name="basePath">Path containing the assembly file.</param>
    /// <param name="loadedAssembliesLookup">Running list of loaded assemblies - hash-based for speed.</param>
    /// <param name="loadedAssemblies">Running list of loaded assemblies, in order.</param>
    private static void LoadDependentAssemblies(
        Assembly rootAssembly,
        string basePath,
        ISet<Assembly> loadedAssembliesLookup,
        IList<Assembly> loadedAssemblies)
    {
        var assemblyNames = rootAssembly
            .GetReferencedAssemblies()
            .Where(an => an.Name is not null)
            .ToList();

        foreach (var assemblyName in assemblyNames)
        {
            var dllPath = Path.Combine(basePath, assemblyName.Name + ".dll");
            if (!File.Exists(dllPath)) continue;

            //Same assembly will not be loaded more than once
            var loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
            if (loadedAssembliesLookup.Add(loadedAssembly))
                loadedAssemblies.Add(loadedAssembly);

            LoadDependentAssemblies(loadedAssembly, basePath, loadedAssembliesLookup, loadedAssemblies);
        }
    }
}