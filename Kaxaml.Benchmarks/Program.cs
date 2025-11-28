using BenchmarkDotNet.Running;
using Kaxaml.Benchmarks.Utilities;

BenchmarkRunner.Run<XmlFoldingBenchmarks>();
BenchmarkRunner.Run<XmlAuditTagsBenchmarks>();