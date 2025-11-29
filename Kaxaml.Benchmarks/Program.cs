using BenchmarkDotNet.Running;
using Kaxaml.Benchmarks.Utilities;

BenchmarkRunner.Run([
    typeof(XmlFoldingBenchmarks),
    typeof(XmlAuditTagsBenchmarks),
    typeof(XmlAssemblyCommentBenchmark)
]);