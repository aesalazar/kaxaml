using System.Text;
using BenchmarkDotNet.Attributes;

namespace Kaxaml.Benchmarks.TestHelpers;

[MemoryDiagnoser]
public class BatchWrapBenchmarks
{
    private const int MaxLength = 80;
    private const string Indent = "";
    private const string SecondaryIndent = "    ";
    private const string LongXml = "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\" xmlns:d=\"http://schemas.microsoft.com/expression/blend/2008\" mc:Ignorable=\"d\" d:DesignHeight=\"300\" d:DesignWidth=\"400\">";
    private readonly List<string> _longLines = Enumerable.Repeat(LongXml, 1000).ToList();

    [Benchmark]
    public string OriginalWrapManyLines()
    {
        var sb = new StringBuilder();
        foreach (var line in _longLines)
        {
            var wrapped = OriginalWrapLongLine(line, MaxLength, Indent, SecondaryIndent);
            foreach (var w in wrapped)
                sb.AppendLine(w);
        }

        return sb.ToString();
    }

    [Benchmark]
    public string StreamingWrapManyLines()
    {
        var sb = new StringBuilder();
        foreach (var line in _longLines)
        {
            StreamingWrapLongLine(line, MaxLength, Indent, SecondaryIndent, sb);
        }

        return sb.ToString();
    }

    private List<string> OriginalWrapLongLine(string text, int maxLength, string primaryIndent, string secondaryIndent)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        sb.Append(primaryIndent);
        var lineLen = primaryIndent.Length;
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var isBreak = text[i] == ' ' || (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '<');
            if (isBreak)
            {
                var tokenLen = i - start;
                if (lineLen + tokenLen + 1 > maxLength)
                {
                    lines.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                    sb.Append(secondaryIndent);
                    lineLen = secondaryIndent.Length;
                }

                sb.Append(text.AsSpan(start, tokenLen));
                sb.Append(' ');
                lineLen += tokenLen + 1;
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            var tokenLen = text.Length - start;
            if (lineLen + tokenLen > maxLength)
            {
                lines.Add(sb.ToString().TrimEnd());
                sb.Clear();
                sb.Append(secondaryIndent);
            }

            sb.Append(text.AsSpan(start, tokenLen));
        }

        if (sb.Length > 0)
            lines.Add(sb.ToString().TrimEnd());

        return lines;
    }

    private void StreamingWrapLongLine(string text, int maxLength, string primaryIndent, string secondaryIndent, StringBuilder sb)
    {
        sb.Append(primaryIndent);
        var lineLen = primaryIndent.Length;
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var isBreak = text[i] == ' ' || (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '<');
            if (isBreak)
            {
                var tokenLen = i - start;
                if (lineLen + tokenLen + 1 > maxLength)
                {
                    sb.AppendLine();
                    sb.Append(secondaryIndent);
                    lineLen = secondaryIndent.Length;
                }

                sb.Append(text.AsSpan(start, tokenLen));
                sb.Append(' ');
                lineLen += tokenLen + 1;
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            var tokenLen = text.Length - start;
            if (lineLen + tokenLen > maxLength)
            {
                sb.AppendLine();
                sb.Append(secondaryIndent);
            }

            sb.Append(text.AsSpan(start, tokenLen));
        }

        sb.AppendLine();
    }
}