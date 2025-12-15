using System.Xml;

namespace Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;

/// <summary>
/// Core metadata about a XAML node.
/// </summary>
public record XamlNodeData(
    string Name,
    string Value,
    XmlNodeType NodeType,
    bool IsSelfClosingElement,
    int Depth,
    IList<XamlAttributeValuePair> AttributeValues)
{
    /// <summary>
    /// Traverses the XmlReader and reads all nodes into a list of XamlNodeData.
    /// </summary>
    /// <param name="xmlReader">XML data to traverse.</param>
    /// <returns>Collection of node metadata.</returns>
    /// <remarks>
    /// This allows for easier manipulation of the XAML structure before writing
    /// it back out.  For example, support for both forward and backward traversal.
    /// </remarks>
    public static IList<XamlNodeData> ReadAllNodes(XmlReader xmlReader)
    {
        var nodes = new List<XamlNodeData>();
        while (xmlReader.Read())
        {
            var isEmpty = xmlReader.IsEmptyElement;
            var attributes = new List<XamlAttributeValuePair>();
            if (xmlReader.HasAttributes)
            {
                for (var i = 0; i < xmlReader.AttributeCount; i++)
                {
                    xmlReader.MoveToAttribute(i);
                    attributes.Add(new XamlAttributeValuePair(
                        xmlReader.Name,
                        xmlReader.Value));
                }

                xmlReader.MoveToElement();
            }

            nodes.Add(new XamlNodeData(
                xmlReader.Name,
                xmlReader.Value,
                xmlReader.NodeType,
                isEmpty,
                xmlReader.Depth,
                attributes));
        }

        return nodes;
    }
}