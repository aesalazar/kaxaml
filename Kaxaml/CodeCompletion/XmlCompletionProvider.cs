using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;
using KaxamlPlugins.Utilities;

namespace Kaxaml.CodeCompletion
{
    public class XmlCompletionDataProvider : ICompletionDataProvider
    {

        #region Static Fields

        private static XmlSchemaCompletionData? _defaultSchemaCompletionData;

        #endregion Static Fields

        #region Fields


        protected string? preSelection;
        private readonly string _defaultNamespacePrefix = string.Empty;

        #endregion Fields

        #region Properties


        public static bool IsSchemaLoaded => _defaultSchemaCompletionData != null;

        #endregion Properties

        #region Static Methods

        public static void LoadSchema(string filename)
        {
            var ex = LoadSchemaFromFile(filename);
            if (ex is not null)
            {
                MessageBox.Show("Failed to load schema");
                Debug.WriteLine(ex);
            }
        }

        private static Exception? LoadSchemaFromFile(string filename)
        {
            try
            {
                _defaultSchemaCompletionData = new XmlSchemaCompletionData(filename);
                return null;
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }

                return ex;
            }
        }

        #endregion Static Methods


        #region ICompletionDataProvider Members

        public int DefaultIndex => 0;

        public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            var text = string.Concat(textArea.Document.GetText(0, textArea.Caret.Offset), charTyped);

            switch (charTyped)
            {
                case '<':
                    // Child element intellisense.
                    var parentPath = XmlParser.GetParentElementPath(text);
                    if (parentPath.Elements.Count > 0)
                    {
                        var data = GetChildElementCompletionData(parentPath);
                        //returnval = data;
                        return data;
                    }

                    if (_defaultSchemaCompletionData != null)
                    {
                        return _defaultSchemaCompletionData.GetElementCompletionData(_defaultNamespacePrefix);
                    }
                    break;

                case ' ':
                    // Attribute intellisense.
                    if (!XmlParser.IsInsideAttributeValue(text, text.Length))
                    {
                        var path = XmlParser.GetActiveElementStartPath(text, text.Length);
                        if (path.Elements.Count > 0)
                        {
                            return GetAttributeCompletionData(path);
                        }
                    }
                    break;

                case '\'':
                case '\"':

                    // Attribute value intellisense.
                    //if (XmlParser.IsAttributeValueChar(charTyped)) {
                    text = text.Substring(0, text.Length - 1);
                    var attributeName = XmlParser.GetAttributeName(text, text.Length);
                    if (attributeName.Length > 0)
                    {
                        var elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
                        if (elementPath.Elements.Count > 0)
                        {
                            preSelection = charTyped.ToString();
                            return GetAttributeValueCompletionData(elementPath, attributeName);
                            //		}
                        }
                    }
                    break;
            }

            return [];
        }


        private ICompletionData[] GetChildElementCompletionData(XmlElementPath path)
        {
            ICompletionData[]? completionData = null;

            var schema = _defaultSchemaCompletionData;
            if (schema != null)
            {
                completionData = schema.GetChildElementCompletionData(path);
            }

            return completionData ?? [];
        }

        private ICompletionData[] GetAttributeCompletionData(XmlElementPath path)
        {
            ICompletionData[]? completionData = null;

            var schema = _defaultSchemaCompletionData;
            if (schema != null)
            {
                completionData = schema.GetAttributeCompletionData(path);
            }

            return completionData ?? [];
        }

        private ICompletionData[] GetAttributeValueCompletionData(XmlElementPath path, string name)
        {
            ICompletionData[]? completionData = null;

            var schema = _defaultSchemaCompletionData;
            if (schema != null)
            {
                completionData = schema.GetAttributeValueCompletionData(path, name);
            }

            return completionData ?? [];
        }

        private ImageList? _imageList;
        public ImageList ImageList
        {
            get
            {
                if (_imageList == null)
                {
                    _imageList = new ImageList();
                    //_ImageList.Images.Add(new System.Drawing.Bitmap(@"C:\element2.png"));

                }

                return _imageList;
            }
        }

        public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
        {
            textArea.InsertString(data.Text);
            return false;
            //throw new Exception("The method or operation is not implemented.");
        }

        public string PreSelection => "";

        //get { throw new Exception("The method or operation is not implemented."); }
        public CompletionDataProviderKeyResult ProcessKey(char key)
        {
            return CompletionDataProviderKeyResult.NormalKey;
            //throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
