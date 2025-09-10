using System.Text.RegularExpressions;

namespace KaxamlPlugins.Utilities;

public static class XmlUtilities
{
    /// <summary>
    /// Match open to closings, ignoring self-closing XML.
    /// </summary>
    private static readonly Regex TagPattern = new(@"<\s*(/?)([\w:.]+)([^<>]*?)(/?)\s*>", RegexOptions.Singleline);

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
    public record XmlTagInfo(
        string Name,
        bool IsOpening,
        int StartIndex,
        int NameStartIndex,
        int NameEndIndex,
        int Depth);
}