namespace Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;

/// <summary>
/// Configuration settings for XAML pretty printing.
/// </summary>
public sealed record XamlPrettyPrintConfig(
    int AttributeCountTolerance,
    bool ReorderAttributes,
    bool ReducePrecision,
    int Precision,
    bool RemoveCommonDefaultValues,
    int IndentWidth,
    bool ConvertTabsToSpaces,
    bool IsEmptyNonSelfClosingSingleLine,
    bool IsLongLineWrapping,
    int LongLineWrappingThreshold
);