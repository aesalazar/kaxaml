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
        //Pre-cache and filter nodes to allow for forward and reverse peeking
        using var xmlReader = XmlReader.Create(new StringReader(input));
        var xamlNodes = XamlNodeData
            .ReadAllNodes(xmlReader)
            .Where(n => n.NodeType
                is XmlNodeType.Element
                or XmlNodeType.Text
                or XmlNodeType.EndElement
                or XmlNodeType.Comment
                or XmlNodeType.ProcessingInstruction)
            .ToList();

        //Store as a collection to allow for additional line manipulate
        var lines = new List<LineData>();
        for (var i = 0; i < xamlNodes.Count; i++)
        {
            var xamlNodeData = xamlNodes[i];
            switch (xamlNodeData.NodeType)
            {
                case XmlNodeType.Element:
                    var element = BuildElementPart(xamlNodeData);
                    lines.Add(LineData.Element(CalculateIndent(xamlNodeData.Depth), element));
                    break;

                case XmlNodeType.Text:
                    var indent = CalculateIndent(xamlNodeData.Depth);
                    lines.AddRange(xamlNodeData
                        .Value
                        .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(split => LineData.Text(indent, EscapeText(split))));
                    break;

                case XmlNodeType.EndElement:
                    var isClosingSameLine = IsEndElementIndented() is false;
                    if (isClosingSameLine)
                    {
                        var last = lines.Last();
                        lines[lines.Count - 1] = LineData.Element(
                            last.Indent,
                            last.Value + $"</{xamlNodeData.Name}>");
                    }
                    else
                    {
                        lines.Add(LineData.EndElement(
                            CalculateIndent(xamlNodeData.Depth),
                            $"</{xamlNodeData.Name}>"));
                    }

                    break;

                case XmlNodeType.Comment:
                    lines.Add(LineData.Comment(CalculateIndent(xamlNodeData.Depth), $"<!--{xamlNodeData.Value}-->"));
                    break;

                case XmlNodeType.ProcessingInstruction:
                    lines.Add(LineData.ProcessingInstruction(CalculateIndent(xamlNodeData.Depth), $"<?Mapping {xamlNodeData.Value.Trim()} ?>"));
                    break;
            }

            continue;

            bool IsEndElementIndented()
            {
                if (i < 1 || xamlNodeData.IsSelfClosingElement) return false;
                if (config.IsEmptyNonSelfClosingSingleLine is false) return true;
                var prior = xamlNodes[i - 1];
                return prior.NodeType is not XmlNodeType.Element || prior.Name != xamlNodeData.Name;
            }
        }

        return ReconstructAsPrettyString(lines);
    }

    /// <summary>
    /// Coalesces the indented lines.
    /// </summary>
    private string ReconstructAsPrettyString(List<LineData> lines)
    {
        var stringBuilder = new StringBuilder();
        var secondaryIndent = CalculateIndent(1);

        foreach (var line in lines)
        {
            //Wrap when configured but not on top-line compiler info
            if (config.IsLongLineWrapping &&
                line.NodeType is not (XmlNodeType.ProcessingInstruction or XmlNodeType.Comment))
            {
                //Break up where necessary
                var words = WrapLongLine(
                    line.Value,
                    config.LongLineWrappingThreshold - line.Indent.Length,
                    line.Indent,
                    line.NodeType is XmlNodeType.Text ? string.Empty : secondaryIndent);

                foreach (var word in words)
                    stringBuilder.AppendLine(word);
            }
            else
            {
                stringBuilder.AppendLine($"{line.Indent}{line.Value}");
            }
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Generates a string with the needed number of spaces or tabs based on <see cref="config"/>.
    /// </summary>
    /// <param name="depth">XAML Node Depth.</param>
    /// <returns>Spaces or Tabs string.</returns>
    private string CalculateIndent(int depth) =>
        config.ConvertTabsToSpaces
            ? new string(' ', depth * config.IndentWidth)
            : new string('\t', depth);

    /// <summary>
    /// Replaces XML characters that need to be escaped.
    /// </summary>
    private static string EscapeText(string input) =>
        input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    /// <summary>
    /// Builds the XAML Element, either partial or complete, based on the node data.
    /// </summary>
    private string BuildElementPart(XamlNodeData xamlNodeData)
    {
        var elementName = xamlNodeData.Name;
        var sb = new StringBuilder();
        sb.Append("<").Append(elementName);

        if (xamlNodeData.AttributeValues.Any())
        {
            var attributes = (List<XamlAttributeValuePair>)xamlNodeData.AttributeValues;
            if (config.RemoveCommonDefaultValues)
                attributes = attributes
                    .Where(avp => XamlAttributeValuePair.IsCommonDefault(avp) is false)
                    .ToList();

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

        sb.Append(xamlNodeData.IsSelfClosingElement ? " />" : ">");
        return sb.ToString();
    }

    /// <summary>
    /// Breaks up line text based on a max length.
    /// </summary>
    /// <param name="text">String text.</param>
    /// <param name="maxLength">Max length allowed.</param>
    /// <param name="primaryIndent">Indent at the first line.</param>
    /// <param name="secondaryIndents">Indent string to apply to overflowed lines.</param>
    /// <returns>Collection of lines generated; only one entry will be present if no overflow.</returns>
    private List<string> WrapLongLine(string text, int maxLength, string primaryIndent, string secondaryIndents)
    {
        var lines = new List<string>();

        //Look for short-circuit
        if (text.Length <= maxLength)
        {
            lines.Add(primaryIndent + text);
            return lines;
        }

        var words = SplitOutsideQuotes(text.Replace("><", "> <"));
        var stringBuilder = new StringBuilder();
        var isAttributeExceeded = words.Count(w => w.isAttribute) > config.AttributeCountTolerance;

        for (var i = 0; i < words.Count; i++)
        {
            var (word, isAttribute) = words[i];

            //See if this needs to be a new line
            if ((isAttributeExceeded && isAttribute) ||
                (stringBuilder.Length > 0 && stringBuilder.Length + word.Length + 1 > maxLength))
            {
                //Remove the extra space and add to the collection
                lines.Add(stringBuilder.ToString().TrimEnd());
                stringBuilder.Clear();

                //This is new line so indent
                stringBuilder.Append(primaryIndent);
                if (word.StartsWith("</") is false)
                    stringBuilder.Append(secondaryIndents);
            }
            else if (i == 0)
            {
                stringBuilder.Append(primaryIndent);
            }

            stringBuilder.Append($"{word} ");
        }

        //Remove the extra space at the very end
        if (stringBuilder.Length > 0)
            lines.Add(stringBuilder.ToString().TrimEnd());

        return lines;
    }

    /// <summary>
    /// Splits a string based on spaces or &gt; &lt; that are OUTSIDE quotes (double or single).
    /// </summary>
    public static List<(string token, bool isAttribute)> SplitOutsideQuotes(string input)
    {
        var result = new List<(string token, bool isAttribute)>();
        var span = input.AsSpan();

        var start = 0;
        var inDoubleQuotes = false;
        var inSingleQuotes = false;

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];

            switch (c)
            {
                case '"' when !inSingleQuotes && (i == 0 || span[i - 1] != '\\'):
                    inDoubleQuotes = !inDoubleQuotes;
                    break;
                case '\'' when !inDoubleQuotes && (i == 0 || span[i - 1] != '\\'):
                    inSingleQuotes = !inSingleQuotes;
                    break;
                default:
                {
                    if (!inDoubleQuotes && !inSingleQuotes)
                    {
                        switch (c)
                        {
                            // Break on space
                            case ' ':
                            {
                                if (i > start)
                                {
                                    var token = span.Slice(start, i - start).Trim();
                                    if (!token.IsEmpty)
                                        result.Add((token.ToString(), IsAttribute(token)));
                                }

                                start = i + 1;
                                break;
                            }
                            // Break on ><
                            case '>' when i + 1 < span.Length && span[i + 1] == '<':
                            {
                                var tokenLen = i - start + 1;
                                if (tokenLen > 0)
                                {
                                    var token = span.Slice(start, tokenLen).Trim();
                                    if (!token.IsEmpty)
                                        result.Add((token.ToString(), IsAttribute(token)));
                                }

                                start = i + 1;
                                i++; // skip '<'
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }

        if (start < span.Length)
        {
            var token = span.Slice(start).Trim();
            if (!token.IsEmpty)
                result.Add((token.ToString(), IsAttribute(token)));
        }

        return result;
    }

    /// <summary>
    /// Determines if the passed text is an XML attribute.
    /// </summary>
    private static bool IsAttribute(ReadOnlySpan<char> token)
    {
        var quoteCount = 0;
        for (var i = 0; i < token.Length; i++)
        {
            switch (token[i])
            {
                case '"' when i == 0 || token[i - 1] != '\\':
                case '\'' when i == 0 || token[i - 1] != '\\':
                    quoteCount++;
                    break;
                case '=' when quoteCount % 2 == 0:
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Metadata for a clean XAML line to reconstitute.
    /// </summary>
    /// <param name="Indent">Starting indent.</param>
    /// <param name="Value">String value.</param>
    /// <param name="NodeType">XML node type.</param>
    private record LineData(string Indent, string Value, XmlNodeType NodeType)
    {
        public static LineData Element(string indent, string value) => new(indent, value, XmlNodeType.Element);
        public static LineData Text(string indent, string value) => new(indent, value, XmlNodeType.Text);
        public static LineData EndElement(string indent, string value) => new(indent, value, XmlNodeType.EndElement);
        public static LineData Comment(string indent, string value) => new(indent, value, XmlNodeType.Comment);
        public static LineData ProcessingInstruction(string indent, string value) => new(indent, value, XmlNodeType.ProcessingInstruction);
    }
}