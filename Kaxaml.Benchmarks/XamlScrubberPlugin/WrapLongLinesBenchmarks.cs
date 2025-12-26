using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using Kaxaml.Benchmarks.TestHelpers;

namespace Kaxaml.Benchmarks.XamlScrubberPlugin;

[MemoryDiagnoser]
[SimpleJob]
public class WrapLongLinesBenchmarks
{
    private const int MaxLength = 50;
    private const string Indent = "";
    private const string SecondaryIndent = "    ";

    [Benchmark]
    public string[] ReplaceSplit() =>
        XmlSamples.StarTrekXml
            .Replace("><", "> <")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [Benchmark]
    public string[] RegexSplit() => Regex.Split(XmlSamples.StarTrekXml, @"\s+|><", RegexOptions.Compiled);

    [Benchmark]
    public List<ReadOnlyMemory<char>> SpanParseToReadOnlyMemories()
    {
        var tokens = new List<ReadOnlyMemory<char>>();
        var start = 0;
        for (var i = 0; i < XmlSamples.StarTrekXml.Length; i++)
        {
            var isBreak = XmlSamples.StarTrekXml[i] == ' ' || (XmlSamples.StarTrekXml[i] == '>' && i + 1 < XmlSamples.StarTrekXml.Length && XmlSamples.StarTrekXml[i + 1] == '<');
            if (isBreak)
            {
                if (i > start)
                    tokens.Add(XmlSamples.StarTrekXml.AsMemory(start, i - start));
                start = i + 1;
            }
        }

        if (start < XmlSamples.StarTrekXml.Length)
            tokens.Add(XmlSamples.StarTrekXml.AsMemory(start, XmlSamples.StarTrekXml.Length - start));
        return tokens;
    }

    [Benchmark]
    public List<string> SpanParseToStrings()
    {
        var tokens = new List<string>();
        const string text = XmlSamples.StarTrekXml;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var isBreak = text[i] == ' ' || (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '<');
            if (isBreak)
            {
                if (i > start)
                    tokens.Add(text.Substring(start, i - start)); // materialize string
                start = i + 1;
            }
        }

        if (start < text.Length)
            tokens.Add(text.Substring(start, text.Length - start));
        return tokens;
    }

    [Benchmark]
    public List<string> WrapLongLineWithStrings()
    {
        const string text = XmlSamples.StarTrekXml;
        var lines = new List<string>();
        var sb = new StringBuilder();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var isBreak = text[i] == ' ' || (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '<');
            if (isBreak)
            {
                var tokenLen = i - start;
                if (sb.Length + tokenLen + 1 > MaxLength)
                {
                    lines.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                    sb.Append(SecondaryIndent);
                }

                sb.Append(text.AsSpan(start, tokenLen));
                sb.Append(' ');
                start = i + 1;
            }
        }

        if (start < text.Length)
        {
            var token = text.AsSpan(start);
            if (sb.Length + token.Length > MaxLength)
            {
                lines.Add(sb.ToString().TrimEnd());
                sb.Clear();
                sb.Append(SecondaryIndent);
            }

            sb.Append(token);
        }

        if (sb.Length > 0)
            lines.Add(sb.ToString().TrimEnd());

        return lines;
    }

    [Benchmark]
    public string WrapLongLineIntoBuilderWithSpans()
    {
        const string text = XmlSamples.StarTrekXml;
        var sb = new StringBuilder();
        sb.Append(Indent);
        var lineLen = Indent.Length;
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var isBreak = text[i] == ' ' || (text[i] == '>' && i + 1 < text.Length && text[i + 1] == '<');
            if (isBreak)
            {
                var tokenLen = i - start;
                if (lineLen + tokenLen + 1 > MaxLength)
                {
                    sb.AppendLine();
                    sb.Append(SecondaryIndent);
                    lineLen = SecondaryIndent.Length;
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
            if (lineLen + tokenLen > MaxLength)
            {
                sb.AppendLine();
                sb.Append(SecondaryIndent);
            }

            sb.Append(text.AsSpan(start, tokenLen));
        }

        sb.AppendLine();
        return sb.ToString();
    }
}