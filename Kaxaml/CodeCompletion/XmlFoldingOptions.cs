namespace Kaxaml.CodeCompletion;

/// <summary>
/// Options that can be passed via parseInformation to control folding behavior.
/// </summary>
public class XmlFoldingOptions
{
    /// <summary>
    /// Whether to show attributes in the folded text.
    /// </summary>
    public bool IsShowingAttributesWhenFolded { get; set; } = true;
}