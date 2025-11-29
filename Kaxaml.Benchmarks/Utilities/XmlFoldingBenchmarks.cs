using System.Text.RegularExpressions;
using System.Xml;
using BenchmarkDotNet.Attributes;
using KaxamlPlugins.Utilities.XmlComponents;
using TurboXml;

namespace Kaxaml.Benchmarks.Utilities;

[MemoryDiagnoser]
[SimpleJob]
public class XmlFoldingBenchmarks
{
    #region Setup

    [Params(false, true)] public bool IsShowingAttributesWhenFolded { get; set; }

    #endregion

    #region XmlTextReader

    /// <summary>
    /// Version prior to creating the CalculateXmlFolds method. 
    /// </summary>
    [Benchmark(Baseline = true)]
    public IList<XmlFoldData> CalculateXmlFolds_XmlTextReader()
    {
        var folds = new List<XmlFoldData>();
        var stack = new Stack<XmlFoldData>();

        using var reader = new XmlTextReader(new StringReader(XmlSamples.StarTrekXml));
        reader.WhitespaceHandling = WhitespaceHandling.All;

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                {
                    var fold = new XmlFoldData(reader.Prefix, reader.LocalName, reader.LineNumber, reader.LinePosition)
                    {
                        FoldText = IsShowingAttributesWhenFolded
                            ? $"<{reader.Name}{(reader.HasAttributes ? " ..." : string.Empty)}>"
                            : $"<{reader.LocalName}>"
                    };

                    if (reader.IsEmptyElement)
                    {
                        fold.EndLine = reader.LineNumber;
                        fold.EndColumn = reader.LinePosition + reader.Name.Length + 2;
                        folds.Add(fold);
                    }
                    else
                    {
                        stack.Push(fold);
                    }

                    break;
                }
                case XmlNodeType.EndElement when stack.Count > 0:
                {
                    var fold = stack.Pop();
                    fold.EndLine = reader.LineNumber;
                    fold.EndColumn = reader.LinePosition + reader.Name.Length + 3;
                    folds.Add(fold);
                    break;
                }
                case XmlNodeType.Comment:
                {
                    var fold = new XmlFoldData(string.Empty, "comment", reader.LineNumber, reader.LinePosition)
                    {
                        FoldText = IsShowingAttributesWhenFolded ? $"<!-- {reader.Value} -->" : "<!--...-->",
                        EndLine = reader.LineNumber,
                        EndColumn = reader.LinePosition + reader.Value.Length + 7
                    };
                    folds.Add(fold);
                    break;
                }
            }
        }

        return folds;
    }

    #endregion

    #region TurboXml

    [Benchmark]
    public IList<XmlFoldData> CalculateXmlFolds_TurboXml()
    {
        var results = new List<XmlFoldData>();
        var stack = new Stack<XmlFoldData>();
        var handler = new XmlFoldReadHandler(results, stack, IsShowingAttributesWhenFolded, XmlSamples.StarTrekXml);
        XmlParser.Parse(XmlSamples.StarTrekXml, ref handler);

        return results;
    }

    #endregion

    #region RegEx

    /// <summary>
    /// Updated version of CalculateXmlFolds which performs it own parsing.
    /// </summary>
    [Benchmark]
    public IList<XmlFoldData> CalculateXmlFolds_RegExParsing()
    {
        var results = new List<XmlFoldData>();
        var stack = new Stack<XmlFoldData>();

        // Precompute line breaks once
        var lineBreaks = PrecomputeLineBreaks(XmlSamples.StarTrekXml);
        foreach (Match match in TagPattern.Matches(XmlSamples.StarTrekXml))
        {
            var isEnd = match.Groups[1].Value == "/";
            var rawName = match.Groups[2].Value;
            var prefix = string.Empty;
            var name = rawName;

            if (rawName.Contains(":"))
            {
                var parts = rawName.Split(':');
                prefix = parts[0];
                name = parts[1];
            }

            var isSelfClosing = match.Groups[4].Value == "/";
            var (line, col) = CalculateLineColumn(match.Index, lineBreaks);

            if (!isEnd)
            {
                var newFoldStart = new XmlFoldData(prefix, name, line, col);
                newFoldStart.FoldText = IsShowingAttributesWhenFolded
                    ? Regex.Replace(match.Value, @"\s+", " ")
                    : $"<{newFoldStart.Name}>";

                if (isSelfClosing)
                {
                    var (endLine, endCol) = CalculateLineColumn(match.Index + match.Length, lineBreaks);

                    // Only fold if the tag spans multiple lines
                    if (endLine > newFoldStart.StartLine)
                    {
                        newFoldStart.EndLine = endLine;
                        newFoldStart.EndColumn = endCol;
                        results.Add(newFoldStart);
                    }
                }
                else
                {
                    stack.Push(newFoldStart);
                }
            }
            else if (stack.Count > 0)
            {
                var foldStart = stack.Pop();
                var (endLine, endCol) = CalculateLineColumn(match.Index + match.Length, lineBreaks);
                foldStart.EndLine = endLine;
                foldStart.EndColumn = endCol;
                results.Add(foldStart);
            }
        }

        foreach (Match match in CommentPattern.Matches(XmlSamples.StarTrekXml))
        {
            var comment = match.Groups[1].Value.Replace("\r\n", "\n");
            var commentLines = comment.Split('\n');
            if (commentLines.Length <= 1) continue;

            var (line, col) = CalculateLineColumn(match.Index, lineBreaks);
            var endLine = line + commentLines.Length - 1;
            var endCol = commentLines[^1].Length + 3;

            var foldText = IsShowingAttributesWhenFolded
                ? Regex.Replace(match.Value, @"\s+", " ")
                : "<!--...-->";

            var fs = new XmlFoldData(string.Empty, "comment", line, col)
            {
                FoldText = foldText,
                EndLine = endLine,
                EndColumn = endCol
            };
            results.Add(fs);
        }

        return results;
    }

    /// <summary>
    /// Match open to closings, ignoring self-closing XML.
    /// </summary>
    private static readonly Regex TagPattern = new(
        @"<\s*(/?)([\w:.]+)([^<>]*?)(/?)\s*>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Match XAML Comment.
    /// </summary>
    private static readonly Regex CommentPattern = new(
        @"<!--(.*?)-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    #endregion

    #region Helpers

    /// <summary>
    /// Precompute line break positions for efficient line/column lookup.
    /// </summary>
    private static int[] PrecomputeLineBreaks(string text) => text
        .Select((c, i) => (c, i))
        .Where(x => x.c == '\n')
        .Select(x => x.i)
        .ToArray();

    /// <summary>
    /// Compute line/column from character offset using precomputed line breaks.
    /// </summary>
    private static (int line, int col) CalculateLineColumn(int index, int[] lineBreaks)
    {
        var line = Array.BinarySearch(lineBreaks, index);
        if (line < 0) line = ~line;
        var col = line == 0 ? index : index - lineBreaks[line - 1] - 1;
        return (line, col);
    }

    #endregion
}