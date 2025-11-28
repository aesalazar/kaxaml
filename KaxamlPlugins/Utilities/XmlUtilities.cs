using System.IO;
using System.Text.RegularExpressions;
using KaxamlPlugins.Utilities.XmlComponents;
using TurboXml;

namespace KaxamlPlugins.Utilities;

/// <summary>
/// Tools for working with XML (typically XAML).
/// </summary>
public static partial class XmlUtilities
{
    #region RegEx

    /// <summary>
    /// Match open to closings, ignoring self-closing XML.
    /// </summary>
    [GeneratedRegex(
        @"<\s*(/?)([\w:.]+)[^<>]*?(/?)\s*>",
        RegexOptions.Compiled)]
    public static partial Regex TagPattern();

    [GeneratedRegex(
        @"<!--\s*AssemblyReferences\s*(.*?)-->", 
        RegexOptions.Singleline | RegexOptions.NonBacktracking | RegexOptions.Compiled)]
    public static partial Regex AssemblyReferencePattern();

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
        if (string.IsNullOrEmpty(xml))
            return [];

        var unmatched = new List<(XmlTagInfo?, XmlTagInfo?)>();
        var openings = new Stack<XmlTagInfo>();
        var depth = 0;

        bool IsMaxReached() => maxTagCount.HasValue && unmatched.Count >= maxTagCount.Value;
        var match = TagPattern().Match(xml);

        while (match.Success)
        {
            var isSelfClosing = match.Groups[3].Value.Length > 0;
            if (isSelfClosing)
            {
                match = match.NextMatch();
                continue;
            }

            var start = match.Index;
            var isOpening = match.Groups[1].Value.Length == 0;

            var nameGroup = match.Groups[2];
            var name = nameGroup.Value;
            var nameStart = nameGroup.Index;
            var nameEnd = nameStart + nameGroup.Length;

            if (isOpening)
            {
                var tag = new XmlTagInfo(name, true, start, nameStart, nameEnd, depth);
                openings.Push(tag);
                depth++;
            }
            else
            {
                depth--;
                var tag = new XmlTagInfo(name, false, start, nameStart, nameEnd, depth);

                if (openings.Count > 0)
                {
                    var open = openings.Peek();
                    if (open.Name != tag.Name)
                    {
                        unmatched.Add((open, tag));
                    }

                    openings.Pop();
                }
                else
                {
                    unmatched.Add((null, tag));
                }
            }

            if (IsMaxReached()) break;
            match = match.NextMatch();
        }

        while (openings.Count > 0 && !IsMaxReached())
        {
            unmatched.Add((openings.Pop(), null));
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
        if (string.IsNullOrEmpty(xml))
            return [];

        var dllPaths = new List<FileInfo>();
        var match = AssemblyReferencePattern().Match(xml);
        while (match.Success)
        {
            var blockContent = match.Groups[1].Value;
            var lines = blockContent
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Select(line => new FileInfo(line));

            dllPaths.AddRange(lines);
            match = match.NextMatch();
        }

        return dllPaths;
    }

    /// <summary>
    /// Parses XML text and returns a collection of fold start nodes.
    /// </summary>
    public static IList<XmlFoldData> CalculateXmlFolds(string xml, bool isShowingAttributes)
    {
        var results = new List<XmlFoldData>();
        var stack = new Stack<XmlFoldData>();
        var handler = new XmlFoldReadHandler(results, stack, isShowingAttributes, xml);
        XmlParser.Parse(xml, ref handler);

        return results;
    }

    #endregion
}