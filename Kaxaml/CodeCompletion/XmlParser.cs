using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Kaxaml.CodeCompletion;

/// <summary>
/// Utility class that contains xml parsing routines used to determine
/// the currently selected element so we can provide intellisense.
/// </summary>
/// <remarks>
/// All of the routines return <see cref="XmlElementPath"/> objects
/// since we are interested in the complete path or tree to the 
/// currently active element. 
/// </remarks>
public static class XmlParser
{
    #region Nested Classes

    /// <summary>
    /// Helper class.  Holds the namespace URI and the prefix currently
    /// in use for this namespace.
    /// </summary>
    private class NamespaceUri
    {
        #region Properties

        public string Namespace { get; set; } = string.Empty;

        public string Prefix { get; set; } = string.Empty;

        #endregion Properties
    }

    #endregion Nested Classes

    #region Static Methods

    /// <summary>
    /// Locates the index of the end tag character.
    /// </summary>
    /// <returns>
    /// Returns the index of the end tag character; otherwise
    /// -1 if no end tag character is found or a start tag
    /// character is found first.
    /// </returns>
    private static int GetActiveElementEndIndex(string xml, int index)
    {
        var elementEndIndex = index;

        for (var i = index; i < xml.Length; ++i)
        {
            var currentChar = xml[i];
            if (currentChar == '>')
            {
                elementEndIndex = i;
                break;
            }

            if (currentChar == '<')
            {
                elementEndIndex = -1;
                break;
            }
        }

        return elementEndIndex;
    }

    /// <summary>
    /// Locates the index of the start tag &lt; character.
    /// </summary>
    /// <returns>
    /// Returns the index of the start tag character; otherwise
    /// -1 if no start tag character is found or a end tag
    /// &gt; character is found first.
    /// </returns>
    public static int GetActiveElementStartIndex(string xml, int index)
    {
        var elementStartIndex = -1;

        var currentIndex = index - 1;
        for (var i = 0; i < index; ++i)
        {
            var currentChar = xml[currentIndex];
            if (currentChar == '<')
            {
                elementStartIndex = currentIndex;
                break;
            }

            if (currentChar == '>') break;

            --currentIndex;
        }

        return elementStartIndex;
    }

    /// <summary>
    /// Gets path of the xml element start tag that the specified 
    /// <paramref name="index"/> is currently inside.
    /// </summary>
    /// <remarks>If the index outside the start tag then an empty path
    /// is returned.</remarks>
    public static XmlElementPath GetActiveElementStartPath(string xml, int index)
    {
        var path = new XmlElementPath();

        var elementText = GetActiveElementStartText(xml, index);

        if (elementText != null)
        {
            var elementName = GetElementName(elementText);
            var elementNamespace = GetElementNamespace(elementText);

            path = GetParentElementPath(xml.Substring(0, index));
            if (elementNamespace.Namespace.Length == 0)
                if (path.Elements.Count > 0)
                {
                    var parentName = path.Elements[path.Elements.Count - 1];
                    elementNamespace.Namespace = parentName.Namespace;
                    elementNamespace.Prefix = parentName.Prefix;
                }

            path.Elements.Add(new QualifiedName(elementName.Name, elementNamespace.Namespace, elementNamespace.Prefix));
            path.Compact();
        }

        return path;
    }

    /// <summary>
    /// Gets the active element path given the element text.
    /// </summary>
    private static XmlElementPath GetActiveElementStartPath(string xml, int index, string elementText)
    {
        var elementName = GetElementName(elementText);
        var elementNamespace = GetElementNamespace(elementText);

        var path = GetParentElementPath(xml.Substring(0, index));
        if (elementNamespace.Namespace.Length == 0)
            if (path.Elements.Count > 0)
            {
                var parentName = path.Elements[path.Elements.Count - 1];
                elementNamespace.Namespace = parentName.Namespace;
                elementNamespace.Prefix = parentName.Prefix;
            }

        path.Elements.Add(new QualifiedName(elementName.Name, elementNamespace.Namespace, elementNamespace.Prefix));
        path.Compact();
        return path;
    }

    /// <summary>
    /// Gets path of the xml element start tag that the specified 
    /// <paramref name="index"/> is currently located. This is different to the
    /// GetActiveElementStartPath method since the index can be inside the element 
    /// name.
    /// </summary>
    /// <remarks>If the index outside the start tag then an empty path
    /// is returned.</remarks>
    public static XmlElementPath GetActiveElementStartPathAtIndex(string xml, int index)
    {
        // Find first non xml element name character to the right of the index.
        index = GetCorrectedIndex(xml.Length, index);
        var currentIndex = index;
        for (; currentIndex < xml.Length; ++currentIndex)
        {
            var ch = xml[currentIndex];
            if (!IsXmlNameChar(ch)) break;
        }

        var elementText = GetElementNameAtIndex(xml, currentIndex);
        if (elementText != null) return GetActiveElementStartPath(xml, currentIndex, elementText);
        return new XmlElementPath();
    }

    /// <summary>
    /// Gets the text of the xml element start tag that the index is 
    /// currently inside.
    /// </summary>
    /// <returns>
    /// Returns the text up to and including the start tag &lt; character.
    /// </returns>
    private static string? GetActiveElementStartText(string xml, int index)
    {
        var elementStartIndex = GetActiveElementStartIndex(xml, index);
        if (elementStartIndex >= 0)
            if (elementStartIndex < index)
            {
                var elementEndIndex = GetActiveElementEndIndex(xml, index);
                if (elementEndIndex >= index) return xml.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
            }

        return null;
    }

    /// <summary>
    /// Gets the name of the attribute inside but before the specified 
    /// index.
    /// </summary>
    public static string GetAttributeName(string xml, int index)
    {
        if (xml.Length == 0) return string.Empty;

        index = GetCorrectedIndex(xml.Length, index);

        return GetAttributeName(xml, index, true, true, true);
    }

    private static string GetAttributeName(string xml, int index, bool ignoreWhitespace, bool ignoreQuote, bool ignoreEqualsSign)
    {
        var name = string.Empty;

        // From the end of the string work backwards until we have
        // picked out the attribute name.
        var reversedAttributeName = new StringBuilder();

        var currentIndex = index;
        var invalidString = true;

        for (var i = 0; i <= index; ++i)
        {
            var currentChar = xml[currentIndex];

            if (char.IsLetterOrDigit(currentChar))
            {
                if (!ignoreEqualsSign)
                {
                    ignoreWhitespace = false;
                    reversedAttributeName.Append(currentChar);
                }
            }
            else if (char.IsWhiteSpace(currentChar))
            {
                if (!ignoreWhitespace)
                {
                    // Reached the start of the attribute name.
                    invalidString = false;
                    break;
                }
            }
            else if (currentChar is '\'' or '\"')
            {
                if (ignoreQuote)
                    ignoreQuote = false;
                else
                    break;
            }
            else if (currentChar == '=')
            {
                if (ignoreEqualsSign)
                    ignoreEqualsSign = false;
                else
                    break;
            }
            else if (IsAttributeValueChar(currentChar))
            {
                if (!ignoreQuote) break;
            }
            else
            {
                break;
            }

            --currentIndex;
        }

        if (!invalidString) name = ReverseString(reversedAttributeName.ToString());

        return name;
    }

    /// <summary>
    /// Gets the name of the attribute at the specified index. The index
    /// can be anywhere inside the attribute name or in the attribute value.
    /// </summary>
    public static string GetAttributeNameAtIndex(string xml, int index)
    {
        index = GetCorrectedIndex(xml.Length, index);

        var ignoreWhitespace = true;
        var ignoreEqualsSign = false;
        var ignoreQuote = false;

        if (IsInsideAttributeValue(xml, index))
        {
            // Find attribute name start.
            var elementStartIndex = GetActiveElementStartIndex(xml, index);
            if (elementStartIndex == -1) return string.Empty;

            // Find equals sign.
            for (var i = index; i > elementStartIndex; --i)
            {
                var ch = xml[i];
                if (ch == '=')
                {
                    index = i;
                    ignoreEqualsSign = true;
                    break;
                }
            }
        }
        else
        {
            // Find end of attribute name.
            for (; index < xml.Length; ++index)
            {
                var ch = xml[index];
                if (!char.IsLetterOrDigit(ch))
                {
                    if (ch is '\'' or '\"')
                    {
                        ignoreQuote = true;
                        ignoreEqualsSign = true;
                    }

                    break;
                }
            }

            --index;
        }

        return GetAttributeName(xml, index, ignoreWhitespace, ignoreQuote, ignoreEqualsSign);
    }

    /// <summary>
    /// Gets the attribute value at the specified index.
    /// </summary>
    /// <returns>An empty string if no attribute value can be found.</returns>
    public static string GetAttributeValueAtIndex(string xml, int index)
    {
        if (!IsInsideAttributeValue(xml, index)) return string.Empty;

        index = GetCorrectedIndex(xml.Length, index);

        var elementStartIndex = GetActiveElementStartIndex(xml, index);
        if (elementStartIndex == -1) return string.Empty;

        // Find equals sign.
        var equalsSignIndex = -1;
        for (var i = index; i > elementStartIndex; --i)
        {
            var ch = xml[i];
            if (ch == '=')
            {
                equalsSignIndex = i;
                break;
            }
        }

        if (equalsSignIndex == -1) return string.Empty;

        // Find attribute value.
        var quoteChar = ' ';
        var foundQuoteChar = false;
        var attributeValue = new StringBuilder();
        for (var i = equalsSignIndex; i < xml.Length; ++i)
        {
            var ch = xml[i];
            if (!foundQuoteChar)
            {
                if (ch is '\"' or '\'')
                {
                    quoteChar = ch;
                    foundQuoteChar = true;
                }
            }
            else
            {
                if (ch == quoteChar)
                    // End of attribute value.
                    return attributeValue.ToString();

                if (IsAttributeValueChar(ch) || ch == '\"' || ch == '\'')
                    attributeValue.Append(ch);
                else
                    // Invalid character found.
                    return string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Ensures that the index is on the last character if it is
    /// too large.
    /// </summary>
    /// <param name="length">The length of the string.</param>
    /// <param name="index">The current index.</param>
    /// <returns>The index unchanged if the index is smaller than the
    /// length of the string; otherwise it returns length - 1.</returns>
    private static int GetCorrectedIndex(int length, int index)
    {
        if (index >= length) index = length - 1;
        return index;
    }

    /// <summary>
    /// Gets the element name from the element start tag string.
    /// </summary>
    /// <param name="xml">This string must start at the 
    /// element we are interested in.</param>
    private static QualifiedName GetElementName(string xml)
    {
        // Find the end of the element name.
        xml = xml.Replace("\r\n", " ");
        var index = xml.IndexOf(' ');
        var name = index > 0
            ? xml.Substring(1, index - 1)
            : xml.Substring(1);

        var prefixIndex = name.IndexOf(':');
        var qualifiedName = prefixIndex > 0
            ? new QualifiedName(
                name.Substring(prefixIndex + 1),
                string.Empty,
                name.Substring(0, prefixIndex))
            : new QualifiedName(name, string.Empty);

        return qualifiedName;
    }

    /// <summary>
    /// Gets the element name at the specified index.
    /// </summary>
    private static string? GetElementNameAtIndex(string xml, int index)
    {
        var elementStartIndex = GetActiveElementStartIndex(xml, index);
        if (elementStartIndex >= 0 && elementStartIndex < index)
        {
            var elementEndIndex = GetActiveElementEndIndex(xml, index);
            if (elementEndIndex == -1) elementEndIndex = xml.IndexOf(' ', elementStartIndex);
            if (elementEndIndex >= elementStartIndex) return xml.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
        }

        return null;
    }

    /// <summary>
    /// Gets the element namespace from the element start tag
    /// string.
    /// </summary>
    /// <param name="xml">This string must start at the 
    /// element we are interested in.</param>
    private static NamespaceUri GetElementNamespace(string xml)
    {
        var namespaceUri = new NamespaceUri();

        var match = Regex.Match(xml, ".*?(xmlns\\s*?|xmlns:.*?)=\\s*?['\\\"](.*?)['\\\"]");
        if (match.Success)
        {
            namespaceUri.Namespace = match.Groups[2].Value;

            var xmlns = match.Groups[1].Value.Trim();
            var prefixIndex = xmlns.IndexOf(':');
            if (prefixIndex > 0) namespaceUri.Prefix = xmlns.Substring(prefixIndex + 1);
        }

        return namespaceUri;
    }

    /// <summary>
    /// Gets the parent element path based on the index position.
    /// </summary>
    public static XmlElementPath GetParentElementPath(string xml)
    {
        var path = new XmlElementPath();

        try
        {
            using (var reader = new StringReader(xml))
            {
                using (var xmlReader = new XmlTextReader(reader))
                {
                    xmlReader.XmlResolver = null;
                    while (xmlReader.Read())
                        switch (xmlReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (!xmlReader.IsEmptyElement)
                                {
                                    var elementName = new QualifiedName(xmlReader.LocalName, xmlReader.NamespaceURI, xmlReader.Prefix);
                                    path.Elements.Add(elementName);
                                }

                                break;
                            case XmlNodeType.EndElement:
                                path.Elements.RemoveLast();
                                break;
                        }
                }
            }
        }
        catch (XmlException)
        {
            //MessageBox.Show(e.Message);
        }
        catch (WebException)
        {
            // Do nothing.
        }

        path.Compact();

        return path;
    }

    /// <summary>
    /// Checks for valid xml attribute value character
    /// </summary>
    public static bool IsAttributeValueChar(char ch)
    {
        if (char.IsLetterOrDigit(ch) ||
            ch == ':' ||
            ch == '/' ||
            ch == '_' ||
            ch == '.' ||
            ch == '-' ||
            ch == '#')
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether the specified index is inside an attribute value.
    /// </summary>
    public static bool IsInsideAttributeValue(string xml, int index)
    {
        if (xml.Length == 0) return false;

        if (index > xml.Length) index = xml.Length;

        var elementStartIndex = GetActiveElementStartIndex(xml, index);
        if (elementStartIndex == -1) return false;

        // Count the number of double quotes and single quotes that exist
        // before the first equals sign encountered going backwards to
        // the start of the active element.
        var foundEqualsSign = false;
        var doubleQuotesCount = 0;
        var singleQuotesCount = 0;
        var lastQuoteChar = ' ';
        for (var i = index - 1; i > elementStartIndex; --i)
        {
            var ch = xml[i];
            if (ch == '=')
            {
                foundEqualsSign = true;
                break;
            }

            if (ch == '\"')
            {
                lastQuoteChar = ch;
                ++doubleQuotesCount;
            }
            else if (ch == '\'')
            {
                lastQuoteChar = ch;
                ++singleQuotesCount;
            }
        }

        var isInside = false;

        if (foundEqualsSign)
        {
            // Odd number of quotes?
            if (lastQuoteChar == '\"' && doubleQuotesCount % 2 > 0)
                isInside = true;
            else if (lastQuoteChar == '\'' && singleQuotesCount % 2 > 0) isInside = true;
        }

        return isInside;
    }

    public static bool IsInsideXmlTag(string xml, int index)
    {
        if (GetActiveElementStartIndex(xml, index) == -1) return false;

        return true;
    }

    /// <summary>
    /// Checks whether the attribute at the end of the string is a 
    /// namespace declaration.
    /// </summary>
    public static bool IsNamespaceDeclaration(string xml, int index)
    {
        if (xml.Length == 0) return false;

        index = GetCorrectedIndex(xml.Length, index);

        // Move back one character if the last character is an '='
        if (xml[index] == '=')
        {
            xml = xml.Substring(0, xml.Length - 1);
            --index;
        }

        // From the end of the string work backwards until we have
        // picked out the last attribute and reached some whitespace.
        var reversedAttributeName = new StringBuilder();

        var ignoreWhitespace = true;
        var currentIndex = index;
        for (var i = 0; i < index; ++i)
        {
            var currentChar = xml[currentIndex];

            if (char.IsWhiteSpace(currentChar))
            {
                if (!ignoreWhitespace)
                    // Reached the start of the attribute name.
                    break;
            }
            else if (char.IsLetterOrDigit(currentChar) || currentChar == ':')
            {
                ignoreWhitespace = false;
                reversedAttributeName.Append(currentChar);
            }
            else
            {
                // Invalid string.
                break;
            }

            --currentIndex;
        }

        // Did we get a namespace?
        var isNamespace =
            reversedAttributeName.ToString() == "snlmx"
            || reversedAttributeName.ToString().EndsWith(":snlmx");

        return isNamespace;
    }

    /// <summary>
    /// Checks for valid xml element or attribute name character.
    /// </summary>
    public static bool IsXmlNameChar(char ch)
    {
        if (char.IsLetterOrDigit(ch) ||
            ch == ':' ||
            ch == '/' ||
            ch == '_' ||
            ch == '.' ||
            ch == '-')
            return true;

        return false;
    }

    private static string ReverseString(string text)
    {
        var reversedString = new StringBuilder(text);

        var index = text.Length;
        foreach (var ch in text)
        {
            --index;
            reversedString[index] = ch;
        }

        return reversedString.ToString();
    }

    #endregion Static Methods
}