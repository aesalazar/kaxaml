using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using KaxamlPlugins;
using KaxamlPlugins.Utilities;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Controls;

/// <summary>
/// Individual Asseembly file reference.
/// </summary>
public class Reference
{
    public Reference(
        FileInfo fileInfo,
        AssemblyCacheManager assemblyCacheManager,
        ILogger logger)
    {
        Name = fileInfo.Name;
        FullName = fileInfo.FullName;
        var assembly = assemblyCacheManager.LoadAssembly(fileInfo);

        AssemblyFileVersion =
            AssemblyUtilities.ExtractAssemblyVersion(assembly, fileInfo)
            ?? throw new FileLoadException($"Could not determine assembly version for file: {FullName}");

        TargetFramework =
            assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName
            ?? throw new FileLoadException($"Could not determine target frameworok for file: {FullName}");
    }

    public string? Name { get; }

    public string FullName { get; }

    public string? AssemblyFileVersion { get; }

    public string? TargetFramework { get; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Reference)obj);
    }

    protected bool Equals(Reference other) =>
        string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) &&
        string.Equals(AssemblyFileVersion, other.AssemblyFileVersion, StringComparison.InvariantCultureIgnoreCase);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Name != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Name) : 0) * 397
                   ^ (AssemblyFileVersion != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(AssemblyFileVersion) : 0);
        }
    }
}