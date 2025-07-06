using System;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Kaxaml.CodeCompletion
{
    /// <summary>
    /// Holds the text for  namespace, child element or attribute 
    /// autocomplete (intellisense).
    /// </summary>
    public class XmlCompletionData : ICompletionData, IComparable
    {

        /// <summary>
        /// The type of text held in this object.
        /// </summary>
        public enum DataType
        {
            XmlElement = 1,
            XmlAttribute = 2,
            NamespaceUri = 3,
            XmlAttributeValue = 4,
            Snippet = 5,
            Comment = 6,
            Other = 7,
        }

        public XmlCompletionData(string text)
            : this(text, string.Empty, DataType.XmlElement)
        {
        }

        public XmlCompletionData(string text, string description)
            : this(text, description, DataType.XmlElement)
        {
        }

        public XmlCompletionData(string text, DataType dataType)
            : this(text, string.Empty, dataType)
        {
        }

        public XmlCompletionData(string text, string description, DataType dataType)
        {
            Text = text;
            Description = description;
            CompletionDataType = dataType;
        }

        public int ImageIndex => 0;

        public string Text { get; set; }

        /// <summary>
        /// Returns the xml item's documentation as retrieved from
        /// the xs:annotation/xs:documentation element.
        /// </summary>
        public string Description { get; }

        public double Priority => 0;

        public DataType CompletionDataType { get; }

        public bool InsertAction(TextArea textArea, char ch)
        {
            if (CompletionDataType is DataType.XmlElement or DataType.XmlAttributeValue)
            {
                textArea.InsertString(Text);
            }
            else if (CompletionDataType == DataType.NamespaceUri)
            {
                textArea.InsertString(string.Concat("\"", Text, "\""));
            }
            else
            {
                // Insert an attribute.
                var caret = textArea.Caret;
                textArea.InsertString(string.Concat(Text, "=\"\""));

                // Move caret into the middle of the attribute quotes.
                caret.Position = textArea.Document.OffsetToPosition(caret.Offset - 1);
            }
            return false;
        }

        public int CompareTo(object? obj)
        {
            if (obj is not XmlCompletionData data)
            {
                return -1;
            }
            return string.Compare(Text, data.Text, StringComparison.Ordinal);
        }
    }
}
