using System.Reflection;
using KaxamlPlugins.Utilities;
using Xunit.Abstractions;

namespace Kaxaml.Tests.Utilities;

public sealed class AssemblyUtilitiesTests(ITestOutputHelper outputHelper) : IDisposable
{
    private readonly List<FileInfo> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (file.Exists)
                    file.Delete();
            }
            catch (Exception ex)
            {
                outputHelper.WriteLine($"Failed to delete temp file {file.FullName}: {ex.Message}");
            }
        }
    }

    [Fact]
    public void ExtractTargetRuntime_WithNet9Assembly_ReturnsVersion9()
    {
        var file = ExtractResourceToTempFile("WpfSpinnerDemo.9.0.dll");
        outputHelper.WriteLine($"Testing file: {file.FullName}");
        var version = AssemblyUtilities.ExtractTargetRuntime(file);

        Assert.NotNull(version);
        Assert.Equal(new Version(9, 0, 0, 0), version);
    }

    [Fact]
    public void ExtractTargetRuntime_WithNet10Assembly_ReturnsVersion10()
    {
        var file = ExtractResourceToTempFile("WpfSpinnerDemo.10.0.dll");
        outputHelper.WriteLine($"Testing file: {file.FullName}");
        var version = AssemblyUtilities.ExtractTargetRuntime(file);

        Assert.NotNull(version);
        Assert.Equal(new Version(10, 0, 0, 0), version);
    }

    [Fact]
    public void ExtractTargetRuntime_WithBadDll_ReturnsNull()
    {
        var file = ExtractResourceToTempFile("Bad.dll");
        outputHelper.WriteLine($"Testing file: {file.FullName}");
        Assert.Throws<BadImageFormatException>(() => AssemblyUtilities.ExtractTargetRuntime(file));
    }

    private FileInfo ExtractResourceToTempFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{resourceName}");

        using var resourceStream = assembly.GetManifestResourceStream($"Kaxaml.Tests.TestAssemblies.{resourceName}");
        if (resourceStream == null)
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

        using var fileStream = File.Create(tempPath);
        resourceStream.CopyTo(fileStream);

        var fileInfo = new FileInfo(tempPath);
        _tempFiles.Add(fileInfo);
        return fileInfo;
    }
}