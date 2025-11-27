namespace KaxamlPlugins.Utilities.XmlComponents;

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