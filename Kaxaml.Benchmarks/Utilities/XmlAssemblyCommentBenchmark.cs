using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Kaxaml.Benchmarks.TestHelpers;
using KaxamlPlugins.Utilities;

namespace Kaxaml.Benchmarks.Utilities;

[MemoryDiagnoser]
[SimpleJob]
public class XmlAssemblyCommentBenchmark
{
    #region Setup

    private const string CommentXml = @"
<!--AssemblyReference
    C:\Temp\SomeAssembly.dll
-->
";

    private string _testXml = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _testXml = XmlSamples.StarTrekXml + CommentXml;
        Console.WriteLine(_testXml);
    }

    #endregion

    #region Initial

    private static readonly Regex AssemblyReferencePatternInitial = new(
        @"<!--\s*AssemblyReferences\s*(.*?)-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static IList<FileInfo> FindCommentAssemblyReferences_Initial(string? xml)
    {
        if (xml is null or [])
        {
            return [];
        }

        var dllPaths = new List<FileInfo>();

        foreach (Match match in AssemblyReferencePatternInitial.Matches(xml))
        {
            var blockContent = match.Groups[1].Value;
            var lines = blockContent
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Select(line => new FileInfo(line));

            dllPaths.AddRange(lines);
        }

        return dllPaths;
    }

    #endregion

    #region Benchmarks

    [Benchmark(Baseline = true)]
    public IList<FileInfo> InitialVersion() => FindCommentAssemblyReferences_Initial(_testXml);

    [Benchmark]
    public IList<FileInfo> CurrentVersion() => XmlUtilities.FindCommentAssemblyReferences(_testXml);

    #endregion
}