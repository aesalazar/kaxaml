using System.IO;
using System.Text;
using System.Xml;

namespace Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;

/// <summary>
/// Provides formatting logic for XAML content based on a defined configuration.
/// </summary>
public class XamlPrettyPrinter(XamlPrettyPrintConfig config)
{
    /// <summary>
    /// Reduce numeric precision in the input string based on the configuration.
    /// </summary>
    /// <param name="input">Raw XAML string.</param>
    /// <returns>Original XAML with any numeric values adjusted.</returns>
    public string ReducePrecision(string input)
    {
        if (!config.ReducePrecision) return input;

        var old = input;
        var begin = 0;

        while (true)
        {
            begin = old.IndexOf('.', begin);
            if (begin == -1) break;

            begin++; // skip the period

            for (var i = 0; i < config.Precision && begin < old.Length; i++)
            {
                if (char.IsDigit(old[begin]))
                    begin++;
            }

            var end = begin;
            while (end < old.Length && char.IsDigit(old[end])) end++;

            old = old[..begin] + old[end..];
            begin++;
        }

        return old;
    }

    /// <summary>
    /// Traverses the input XAML string and applies indentation and formatting.
    /// </summary>
    /// <param name="input">Raw XAML string.</param>
    /// <returns>Reformatted XAML based on <see cref="config"/>.</returns>
    public string Indent(string input)
    {
        using var xmlReader = XmlReader.Create(new StringReader(input));
        var xamlNodes = XamlNodeData.ReadAllNodes(xmlReader);
        var sb = new StringBuilder();

        foreach (var xamlNodeData in xamlNodes)
        {
            switch (xamlNodeData.NodeType)
            {
                case XmlNodeType.Element:
                    sb.AppendLine(
                        CalculateIndent(
                            xamlNodeData.Depth,
                            config.IndentWidth,
                            config.ConvertTabsToSpaces)
                        + BuildElement(
                            xamlNodeData,
                            config.ReorderAttributes,
                            config.RemoveCommonDefaultValues,
                            config.AttributeCountTolerance,
                            config.IndentWidth,
                            config.ConvertTabsToSpaces));
                    break;

                case XmlNodeType.Text:
                    sb.Append(xamlNodeData.Value
                        .Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;"));
                    break;

                case XmlNodeType.EndElement:
                    sb.AppendLine(
                        CalculateIndent(
                            xamlNodeData.Depth,
                            config.IndentWidth,
                            config.ConvertTabsToSpaces)
                        + $"</{xamlNodeData.Name}>");
                    break;

                case XmlNodeType.Comment:
                    sb.AppendLine(
                        CalculateIndent(
                            xamlNodeData.Depth,
                            config.IndentWidth,
                            config.ConvertTabsToSpaces)
                        + $"<!--{xamlNodeData.Value}-->");
                    break;

                case XmlNodeType.ProcessingInstruction:
                    sb.AppendLine(
                        CalculateIndent(
                            xamlNodeData.Depth,
                            config.IndentWidth,
                            config.ConvertTabsToSpaces)
                        + $"<?Mapping {xamlNodeData.Value} ?>");
                    break;
            }
        }

        return sb.ToString();
    }

    private static string CalculateIndent(int depth, int indentWidth, bool convertTabsToSpaces) =>
        convertTabsToSpaces
            ? new string(' ', depth * indentWidth)
            : new string('\t', depth);

    private static string BuildElement(
        XamlNodeData xamlNodeData,
        bool reorderAttributes,
        bool removeCommonDefaults,
        int attributeCountTolerance,
        int indentWidth,
        bool convertTabsToSpaces)
    {
        var elementName = xamlNodeData.Name;
        var isElementEmpty = xamlNodeData.IsEmptyElement;
        var sb = new StringBuilder();
        sb.Append("<").Append(elementName);

        if (xamlNodeData.AttributeValues.Any())
        {
            var attributes = new List<XamlAttributeValuePair>();
            foreach (var xmlReaderAttributeValue in xamlNodeData.AttributeValues)
            {
                if (removeCommonDefaults is false ||
                    XamlAttributeValuePair.IsCommonDefault(xmlReaderAttributeValue) is false)
                    attributes.Add(xmlReaderAttributeValue);
            }

            if (reorderAttributes) attributes.Sort();

            foreach (var a in attributes)
            {
                if (attributes.Count > attributeCountTolerance &&
                    !XamlAttributeValuePair.ForceNoLineBreaks(elementName))
                {
                    sb
                        .AppendLine()
                        .Append(CalculateIndent(
                            xamlNodeData.Depth, 
                            indentWidth, 
                            convertTabsToSpaces))
                        .Append($"{a.Name}=\"{a.Value}\"");
                }
                else
                {
                    sb.Append($" {a.Name}=\"{a.Value}\"");
                }
            }
        }

        sb.Append(isElementEmpty ? " />" : ">");
        return sb.ToString();
    }
}