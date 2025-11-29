namespace KaxamlPlugins.Utilities.XmlComponents;

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

    /// <summary>
    /// Offset from start of entire XML.
    /// </summary>
    public int StartOffset { get; set; }
}