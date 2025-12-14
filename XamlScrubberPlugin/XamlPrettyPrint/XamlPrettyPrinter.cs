using System.IO;
using System.Text;
using System.Xml;

namespace Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;

/// <summary>
/// Provides formatting logic for XAML content based on a defined configuration.
/// </summary>
public class XamlPrettyPrinter(XamlPrettyPrintConfig config)
{
    private static readonly string[] NewLineStrings = ["\r\n", "\r", "\n"];

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
                    sb.AppendLine(CalculateIndent(xamlNodeData.Depth) + BuildElement(xamlNodeData));
                    break;

                case XmlNodeType.Text:
                    var splits = xamlNodeData
                        .Value
                        .Split(NewLineStrings, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    foreach (var split in splits)
                        sb.AppendLine(CalculateIndent(xamlNodeData.Depth) + EscapeText(split));
                    break;

                case XmlNodeType.EndElement:
                    sb.AppendLine(CalculateIndent(xamlNodeData.Depth) + $"</{xamlNodeData.Name}>");
                    break;

                case XmlNodeType.Comment:
                    sb.AppendLine(CalculateIndent(xamlNodeData.Depth) + $"<!--{xamlNodeData.Value}-->");
                    break;

                case XmlNodeType.ProcessingInstruction:
                    sb.AppendLine(CalculateIndent(xamlNodeData.Depth) + $"<?Mapping {xamlNodeData.Value} ?>");
                    break;
            }
        }

        return sb.ToString();
    }

    private string CalculateIndent(int depth) =>
        config.ConvertTabsToSpaces
            ? new string(' ', depth * config.IndentWidth)
            : new string('\t', depth);

    private static string EscapeText(string input) =>
        input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private string BuildElement(XamlNodeData xamlNodeData)
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
                if (config.RemoveCommonDefaultValues is false ||
                    XamlAttributeValuePair.IsCommonDefault(xmlReaderAttributeValue) is false)
                    attributes.Add(xmlReaderAttributeValue);
            }

            if (config.ReorderAttributes) attributes.Sort();

            foreach (var a in attributes)
            {
                if (attributes.Count > config.AttributeCountTolerance &&
                    !XamlAttributeValuePair.ForceNoLineBreaks(elementName))
                {
                    sb
                        .AppendLine()
                        .Append(CalculateIndent(xamlNodeData.Depth + 1))
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