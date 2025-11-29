using TurboXml;

namespace KaxamlPlugins.Utilities.XmlComponents;

/// <summary>
/// Implementation of TurboXml read handler.
/// </summary>
/// <remarks>
/// This overrides the needed methods to return the expected folding. Any malformed XML will be caught by
/// TurboXml resulting in an empty collection being returned.
/// </remarks>
public readonly struct XmlFoldReadHandler(
    IList<XmlFoldData> results,
    Stack<XmlFoldData> stack,
    bool isShowingAttributes,
    string sourceXml
) : IXmlReadHandler
{
    public void OnBeginTag(ReadOnlySpan<char> name, int line, int column)
    {
        var rawName = name.ToString();
        var prefix = string.Empty;
        var localName = rawName;

        if (rawName.Contains(':'))
        {
            var parts = rawName.Split(':');
            prefix = parts[0];
            localName = parts[1];
        }

        var fold = new XmlFoldData(prefix, localName, line, column - 1)
        {
            StartOffset = CalculateLineColumnOffset(line, column)
        };

        stack.Push(fold);

        if (isShowingAttributes)
        {
            _attributes[fold] = [];
        }
    }

    public void OnEndTagEmpty()
    {
        if (stack.Count <= 0) return;
        var fold = stack.Pop();

        // Build FoldText with attributes if requested
        if (isShowingAttributes && _attributes.TryGetValue(fold, out var attrs) && attrs.Count > 0)
        {
            fold.FoldText = $"<{fold.Name} {attrs.First()}.../>";
        }
        else
        {
            fold.FoldText = $"<{fold.Name} />";
        }

        // Find the actual closing delimiter relative to this tag’s start offset
        var closingIndex = sourceXml.IndexOf("/>", fold.StartOffset, StringComparison.Ordinal);
        var closingLine = CalculateLineBreaksBeforeIndex(closingIndex);
        var lineStart = closingLine == 0 ? 0 : _lineBreakOffsets[closingLine - 1] + 1;

        fold.EndLine = closingLine;
        fold.EndColumn = closingIndex - lineStart + 2; // +2 to include "/>"

        // Only fold if multi-line
        if (fold.EndLine > fold.StartLine) results.Add(fold);
    }

    public void OnEndTag(ReadOnlySpan<char> name, int line, int column)
    {
        if (stack.Count <= 0) return;
        var fold = stack.Pop();

        // Build FoldText with attributes if requested
        if (isShowingAttributes && _attributes.TryGetValue(fold, out var attrs) && attrs.Count > 0)
        {
            fold.FoldText = $"<{fold.Name} {attrs.First()}...>";
        }
        else
        {
            fold.FoldText = $"<{fold.Name}>";
        }

        fold.EndLine = line;
        fold.EndColumn = column + name.Length + 1;

        // Only fold if multi-line
        if (fold.EndLine > fold.StartLine) results.Add(fold);
    }

    public void OnAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int nameLine, int nameColumn, int valueLine, int valueColumn)
    {
        if (stack.Count <= 0 || !isShowingAttributes) return;
        var fold = stack.Peek();
        if (_attributes.TryGetValue(fold, out var attrs))
        {
            attrs.Add($"{name}=\"{value}\"");
        }
    }

    public void OnComment(ReadOnlySpan<char> text, int line, int column)
    {
        var comment = text.ToString().Replace("\r\n", "\n");
        var commentLines = comment.Split('\n');
        if (commentLines.Length <= 1) return;

        var startColumn = column - 4; // Account for <!--
        var endColumn = commentLines[^1].Length + 3; // Account for -->
        var foldText = $"<!--{(isShowingAttributes ? commentLines.First().Replace("\n", "").Trim() : string.Empty)}...";
        var fold = new XmlFoldData(string.Empty, "comment", line, startColumn)
        {
            FoldText = foldText,
            EndLine = line + commentLines.Length - 1,
            EndColumn = endColumn
        };
        results.Add(fold);
    }

    public void OnText(ReadOnlySpan<char> text, int line, int column)
    {
        //Ignore
    }

    public void OnError(string message, int line, int column)
    {
        // Swallow error and return empty collection
    }

    #region Helpers

    /// <summary>
    /// Track attributes per fold
    /// </summary>
    private readonly Dictionary<XmlFoldData, List<string>> _attributes = new();

    /// <summary>
    /// Precomputed newline offset cache for fast lookup.
    /// </summary>
    private readonly List<int> _lineBreakOffsets = BuildLineBreakOffsets(sourceXml);

    /// <summary>
    /// Calculate the position of all newlines.
    /// </summary>
    private static List<int> BuildLineBreakOffsets(string text)
    {
        var offsets = new List<int>();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') offsets.Add(i);
        }

        return offsets;
    }

    /// <summary>
    /// Number of line breaks before an index.
    /// </summary>
    private int CalculateLineBreaksBeforeIndex(int index)
    {
        var pos = _lineBreakOffsets.BinarySearch(index);
        if (pos < 0) pos = ~pos;
        return pos;
    }

    /// <summary>
    /// Calculates absolute position in the xml based on the line number and column in the line.
    /// </summary>
    private int CalculateLineColumnOffset(int line, int column)
    {
        var lineStart = line == 0 ? 0 : _lineBreakOffsets[line - 1] + 1;
        return lineStart + column;
    }

    #endregion
}