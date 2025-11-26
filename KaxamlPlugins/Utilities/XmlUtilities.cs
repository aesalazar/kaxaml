using System.IO;
using System.Text.RegularExpressions;

namespace KaxamlPlugins.Utilities;

/// <summary>
/// Tools for working with XML (typically XAML).
/// </summary>
public static class XmlUtilities
{
    #region RegEx

    /// <summary>
    /// Match open to closings, ignoring self-closing XML.
    /// </summary>
    private static readonly Regex TagPattern = new(
        @"<\s*(/?)([\w:.]+)([^<>]*?)(/?)\s*>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Match comment sections that reference external assemblies.
    /// </summary>
    private static readonly Regex AssemblyReferencePattern = new(
        @"<!--\s*AssemblyReferences\s*(.*?)-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Match XAML Comment.
    /// </summary>
    private static readonly Regex CommentPattern = new(
        @"<!--(.*?)-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    #endregion

    #region Private Methods

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

    #region Public Types

    /// <summary>
    /// Holds information about the start and end of a fold in an xml string.
    /// </summary>
    public sealed record XmlFoldData
    {
        public XmlFoldData(string prefix, string name, int startLine, int startColumn)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            Name = string.IsNullOrEmpty(prefix) ? name : string.Concat(prefix, ":", name);
        }

        /// <summary>
        /// Staring line number of the fold.
        /// </summary>
        public int StartLine { get; }

        /// <summary>
        /// Started column number of the fold.
        /// </summary>
        public int StartColumn { get; }

        /// <summary>
        /// Ending line number of the fold.
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Ending column number of the fold.
        /// </summary>
        public int EndColumn { get; set; }

        /// <summary>
        /// XML Node Name including prefix if applicable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Text to show when folded.
        /// </summary>
        public string FoldText { get; set; } = string.Empty;
    }

    /// <summary>
    /// XML Tag metadata.
    /// </summary>
    /// <param name="Name">String within the tag.</param>
    /// <param name="IsOpening">Indicates if it is the start or end of the xml tag.</param>
    /// <param name="StartIndex">Absolute position of the opening carrot of the tag.</param>
    /// <param name="NameStartIndex">Absolute position of the first name string character.</param>
    /// <param name="NameEndIndex">Absolute position of the last name string character.</param>
    /// <param name="Depth">Level within the XML Tag nesting (Root is 0).</param>
    /// <remarks>
    /// Note that the <see cref="Depth"/> can go negative if more close tags than open.
    /// </remarks>
    public sealed record XmlTagInfo(
        string Name,
        bool IsOpening,
        int StartIndex,
        int NameStartIndex,
        int NameEndIndex,
        int Depth);

    #endregion

    #region Public Method

    /// <summary>
    /// Parses XML to look for problem Tag pairs based on their position within the string.
    /// </summary>
    /// <param name="xml">XML string to parse.</param>
    /// <param name="maxTagCount">(OPTIONAL) Maximum number of mismatched tags to return.</param>
    /// <returns>Collection of problem open/close tag pairs.</returns>
    /// <remarks>
    /// This returns the unmatched tags in the order of inner/top most to outer/bottom most.  If
    /// a different order is needed, the properties of <see cref="XmlTagInfo"/> can be used to sort,
    /// e.g. <see cref="XmlTagInfo.StartIndex"/>.
    /// 
    /// This ignores self-closing tags, e.g. <c>&lt;Name /&gt;</c>.  If it detects an opening tag
    /// without matching closed tag, the opening tag will be returned with a <see langword="null"/>
    /// close tag value.  If a close tag is detected without a matching open tag, it will be
    /// returned with a <see langword="null"/> open tag value.
    /// 
    /// If the maximum number of tags is set, this will return only that many in the order in which
    /// they are encountered in the XML.  If this is use, consideration should be taken about the
    /// order in which mismatches are logged, as mentioned above.
    /// </remarks>
    public static IList<(XmlTagInfo? openTag, XmlTagInfo? closeTag)> AuditXmlTags(string? xml, int? maxTagCount)
    {
        if (xml is null or [])
        {
            return [];
        }

        //Use a queue to track in case there is an odd number
        var tags = new Queue<XmlTagInfo>();
        var depth = 0;

        foreach (Match match in TagPattern.Matches(xml))
        {
            var isSelfClosing = match.Groups[4].Value.Contains('/');
            if (isSelfClosing) continue;

            var start = match.Groups[0].Index;
            var isOpening = match.Groups[1].Value != "/";

            var nameGroup = match.Groups[2];
            var name = nameGroup.Value;
            var nameStart = nameGroup.Index;
            var nameEnd = nameStart + nameGroup.Length;

            //Depth can go negative if fewer open than close
            tags.Enqueue(isOpening
                ? new XmlTagInfo(name, true, start, nameStart, nameEnd, depth++)
                : new XmlTagInfo(name, false, start, nameStart, nameEnd, --depth));
        }

        var unmatched = new List<(XmlTagInfo?, XmlTagInfo?)>();
        var openings = new Stack<XmlTagInfo>();
        bool IsMaxReached() => maxTagCount.HasValue && unmatched.Count >= maxTagCount.Value;

        while (tags.Any() && IsMaxReached() is false)
        {
            var tag = tags.Dequeue();
            if (tag.IsOpening)
            {
                openings.Push(tag);
            }
            else if (openings.Any())
            {
                if (openings.Peek().Name != tag.Name)
                {
                    unmatched.Add((openings.Peek(), tag));
                }

                openings.Pop();
            }
            else
            {
                //Handle orphaned closing tag
                unmatched.Add((null, tag));
            }
        }

        //Handle orphaned opening tags
        foreach (var opening in openings)
        {
            if (IsMaxReached()) break;
            unmatched.Add((opening, null));
        }

        return unmatched;
    }

    /// <summary>
    /// Scans the passed XML for well-formed XAML comments containing DLL file paths.
    /// </summary>
    /// <param name="xml">XAML to search.</param>
    /// <returns>Collection of found paths; empty if none.</returns>
    /// <remarks>
    /// This does not validate the presence of the listed files in any way.
    /// 
    /// Comments should be written as:
    ///<code>
    /// &lt;!--AssemblyReferences
    ///     c:\temp\SomeAssembly.dll
    ///     c:\temp\AnotherAssembly.dll
    /// --&gt;
    /// </code>
    /// </remarks>
    public static IList<FileInfo> FindCommentAssemblyReferences(string? xml)
    {
        if (xml is null or [])
        {
            return [];
        }

        var dllPaths = new List<FileInfo>();

        foreach (Match match in AssemblyReferencePattern.Matches(xml))
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

    /// <summary>
    /// Parses XML text and returns a collection of fold start nodes.
    /// </summary>
    public static IList<XmlFoldData> CalculateXmlFolds(string xml, bool showAttributes)
    {
        var results = new List<XmlFoldData>();
        var stack = new Stack<XmlFoldData>();

        // Precompute line breaks once
        var lineBreaks = PrecomputeLineBreaks(xml);
        foreach (Match match in TagPattern.Matches(xml))
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
                newFoldStart.FoldText = showAttributes
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

        foreach (Match match in CommentPattern.Matches(xml))
        {
            var comment = match.Groups[1].Value.Replace("\r\n", "\n");
            var commentLines = comment.Split('\n');
            if (commentLines.Length <= 1) continue;

            var (line, col) = CalculateLineColumn(match.Index, lineBreaks);
            var endLine = line + commentLines.Length - 1;
            var endCol = commentLines[^1].Length + 3;

            var foldText = showAttributes
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

    #endregion
}