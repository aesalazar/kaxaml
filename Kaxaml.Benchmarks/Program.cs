using BenchmarkDotNet.Running;
using Kaxaml.Benchmarks.TestHelpers;
using Kaxaml.Benchmarks.Utilities;
using Kaxaml.Benchmarks.XamlScrubberPlugin;

BenchmarkRunner.Run([
    typeof(BatchWrapBenchmarks),
    //typeof(WrapLongLinesBenchmarks),
    //typeof(XmlFoldingBenchmarks),
    //typeof(XmlAuditTagsBenchmarks),
    //typeof(XmlAssemblyCommentBenchmark)
]);