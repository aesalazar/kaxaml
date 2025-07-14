using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Kaxaml.Plugins.XamlScrubber.Properties;
using KaxamlPlugins;

namespace Kaxaml.Plugins.XamlScrubber;

[Plugin(
    Name = "Xaml Scrubber",
    Icon = "Images\\page_lightning.png",
    Description = "Reformat and cleanup up your XAML (Ctrl+K)",
    ModifierKeys = ModifierKeys.Control,
    Key = Key.K
)]
public partial class XamlScrubberPlugin
{
    public XamlScrubberPlugin()
    {
        InitializeComponent();

        var binding = new CommandBinding(GoCommand);
        binding.Executed += Go_Executed;
        binding.CanExecute += Go_CanExecute;
        InputBindings.Add(new InputBinding(binding.Command, new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl+D")));
        CommandBindings.Add(binding);
    }

    private void Go_Click(object sender, RoutedEventArgs e)
    {
        Go();
    }

    private void Go()
    {
        InitializeValues();

        if (KaxamlInfo.Editor is not null)
        {
            var s = KaxamlInfo.Editor.Text;

            s = Indent(s);
            s = ReducePrecision(s);

            KaxamlInfo.Editor.Text = s;
        }
    }


    public string ReducePrecision(string s)
    {
        var old = s;

        if (_reducePrecision)
        {
            var begin = 0;
            var end = 0;

            while (true)
            {
                begin = old.IndexOf('.', begin);
                if (begin == -1) break;

                // get past the period
                begin++;


                for (var i = 0; i < _precision; i++)
                    if (old[begin] >= '0' && old[begin] <= '9')
                        begin++;

                end = begin;

                while (end < old.Length && old[end] >= '0' && old[end] <= '9') end++;

                old = old.Substring(0, begin) + old.Substring(end, old.Length - end);

                begin++;
            }
        }

        return old;
    }

    public string Indent(string s)
    {
        string result;

        var ms = new MemoryStream(s.Length);
        var sw = new StreamWriter(ms);
        sw.Write(s);
        sw.Flush();

        ms.Seek(0, SeekOrigin.Begin);

        var reader = new StreamReader(ms);
        var xmlReader = XmlReader.Create(reader.BaseStream);
        xmlReader.Read();
        var str = "";

        while (!xmlReader.EOF)
        {
            string xml;
            int num;
            int num6;
            int num7;
            int num8;

            switch (xmlReader.NodeType)
            {
                case XmlNodeType.Element:
                    xml = "";
                    num = 0;
                    goto Element;

                case XmlNodeType.Text:
                {
                    var str4 = xmlReader.Value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
                    str = str + str4;
                    xmlReader.Read();
                    continue;
                }
                case XmlNodeType.ProcessingInstruction:
                    xml = "";
                    num7 = 0;
                    goto ProcessingInstruction;

                case XmlNodeType.Comment:
                    xml = "";
                    num8 = 0;
                    goto Comment;

                case XmlNodeType.Whitespace:
                {
                    xmlReader.Read();
                    continue;
                }
                case XmlNodeType.EndElement:
                    xml = "";
                    num6 = 0;
                    goto EndElement;

                default:
                    goto Other;
            }

            Label_00C0:
            xml = xml + IndentString;
            num++;

            Element:
            if (num < xmlReader.Depth) goto Label_00C0;

            var elementName = xmlReader.Name;

            var str5 = str;
            str = str5 + "\r\n" + xml + "<" + xmlReader.Name;
            var isEmptyElement = xmlReader.IsEmptyElement;


            if (xmlReader.HasAttributes)
            {
                // construct an array of the attributes that we reorder later on
                var attributes = new List<AttributeValuePair>(xmlReader.AttributeCount);

                for (var k = 0; k < xmlReader.AttributeCount; k++)
                {
                    xmlReader.MoveToAttribute(k);

                    if (_removeCommonDefaultValues)
                    {
                        if (!AttributeValuePair.IsCommonDefault(elementName, xmlReader.Name, xmlReader.Value)) attributes.Add(new AttributeValuePair(elementName, xmlReader.Name, xmlReader.Value));
                    }
                    else
                    {
                        attributes.Add(new AttributeValuePair(elementName, xmlReader.Name, xmlReader.Value));
                    }
                }

                if (_reorderAttributes) attributes.Sort();

                xml = "";
                var str3 = "";
                var depth = xmlReader.Depth;

                //str3 = str3 + IndentString;

                for (var j = 0; j < depth; j++) xml = xml + IndentString;

                foreach (var a in attributes)
                {
                    var str7 = str;

                    if (attributes.Count > _attributeCounteTolerance && !AttributeValuePair.ForceNoLineBreaks(elementName))
                        // break up attributes into different lines
                        str = str7 + "\r\n" + xml + str3 + a.Name + "=\"" + a.Value + "\"";
                    else
                        // attributes on one line
                        str = str7 + " " + a.Name + "=\"" + a.Value + "\"";
                }
            }

            if (isEmptyElement) str = str + "/";
            str = str + ">";
            xmlReader.Read();
            continue;
            Label_02F4:
            xml = xml + IndentString;
            num6++;
            EndElement:
            if (num6 < xmlReader.Depth) goto Label_02F4;
            var str8 = str;
            str = str8 + "\r\n" + xml + "</" + xmlReader.Name + ">";
            xmlReader.Read();
            continue;
            Label_037A:
            xml = xml + "    ";
            num7++;
            ProcessingInstruction:
            if (num7 < xmlReader.Depth) goto Label_037A;
            var str9 = str;
            str = str9 + "\r\n" + xml + "<?Mapping " + xmlReader.Value + " ?>";
            xmlReader.Read();
            continue;

            Comment:

            if (num8 < xmlReader.Depth)
            {
                xml = xml + IndentString;
                num8++;
            }

            str = str + "\r\n" + xml + "<!--" + xmlReader.Value + "-->";

            xmlReader.Read();
            continue;

            Other:
            xmlReader.Read();
        }

        xmlReader.Close();

        result = str;
        return result;
    }

    private class AttributeValuePair : IComparable
    {
        public readonly AttributeType AttributeType = AttributeType.Other;
        public readonly string Name = "";
        public readonly string Value = "";

        public AttributeValuePair(string elementname, string name, string value)
        {
            Name = name;
            Value = value;

            // compute the AttributeType
            if (name.StartsWith("xmlns"))
                AttributeType = AttributeType.Namespace;
            else
                switch (name)
                {
                    case "Key":
                    case "x:Key":
                        AttributeType = AttributeType.Key;
                        break;

                    case "Name":
                    case "x:Name":
                        AttributeType = AttributeType.Name;
                        break;

                    case "x:Class":
                        AttributeType = AttributeType.Class;
                        break;

                    case "Canvas.Top":
                    case "Canvas.Left":
                    case "Canvas.Bottom":
                    case "Canvas.Right":
                    case "Grid.Row":
                    case "Grid.RowSpan":
                    case "Grid.Column":
                    case "Grid.ColumnSpan":
                        AttributeType = AttributeType.AttachedLayout;
                        break;

                    case "Width":
                    case "Height":
                    case "MaxWidth":
                    case "MinWidth":
                    case "MinHeight":
                    case "MaxHeight":
                        AttributeType = AttributeType.CoreLayout;
                        break;

                    case "Margin":
                    case "VerticalAlignment":
                    case "HorizontalAlignment":
                    case "Panel.ZIndex":
                        AttributeType = AttributeType.StandardLayout;
                        break;

                    case "mc:Ignorable":
                    case "d:IsDataSource":
                    case "d:LayoutOverrides":
                    case "d:IsStaticText":

                        AttributeType = AttributeType.BlendGoo;
                        break;

                    default:
                        AttributeType = AttributeType.Other;
                        break;
                }
        }

        #region IComparable Members

        public int CompareTo(object? obj)
        {
            if (obj is AttributeValuePair other)
            {
                if (AttributeType == other.AttributeType)
                {
                    // some common special cases where we want things to be out of the normal order

                    if (Name.Equals("StartPoint") && other.Name.Equals("EndPoint")) return -1;
                    if (Name.Equals("EndPoint") && other.Name.Equals("StartPoint")) return 1;

                    if (Name.Equals("Width") && other.Name.Equals("Height")) return -1;
                    if (Name.Equals("Height") && other.Name.Equals("Width")) return 1;

                    if (Name.Equals("Offset") && other.Name.Equals("Color")) return -1;
                    if (Name.Equals("Color") && other.Name.Equals("Offset")) return 1;

                    if (Name.Equals("TargetName") && other.Name.Equals("Property")) return -1;
                    if (Name.Equals("Property") && other.Name.Equals("TargetName")) return 1;

                    return Name.CompareTo(other.Name);
                }

                return AttributeType.CompareTo(other.AttributeType);
            }

            return 0;
        }

        public static bool IsCommonDefault(string elementname, string name, string value)
        {
            if (
                (name == "HorizontalAlignment" && value == "Stretch") ||
                (name == "VerticalAlignment" && value == "Stretch") ||
                (name == "Margin" && value == "0") ||
                (name == "Margin" && value == "0,0,0,0") ||
                (name == "Opacity" && value == "1") ||
                (name == "FontWeight" && value == "{x:Null}") ||
                (name == "Background" && value == "{x:Null}") ||
                (name == "Stroke" && value == "{x:Null}") ||
                (name == "Fill" && value == "{x:Null}") ||
                (name == "Visibility" && value == "Visible") ||
                (name == "Grid.RowSpan" && value == "1") ||
                (name == "Grid.ColumnSpan" && value == "1") ||
                (name == "BasedOn" && value == "{x:Null}") ||

                //(elementname == "ScaleTransform" && name == "ScaleX" && value == "1") ||
                //(elementname == "ScaleTransform" && name == "ScaleY" && value == "1") ||
                //(elementname == "SkewTransform" && name == "AngleX" && value == "0") ||
                //(elementname == "SkewTransform" && name == "AngleY" && value == "0") ||
                //(elementname == "RotateTransform" && name == "Angle" && value == "0") ||
                //(elementname == "TranslateTransform" && name == "X" && value == "0") ||
                //(elementname == "TranslateTransform" && name == "Y" && value == "0") ||
                (elementname != "ColumnDefinition" && elementname != "RowDefinition" && name == "Width" && value == "Auto") ||
                (elementname != "ColumnDefinition" && elementname != "RowDefinition" && name == "Height" && value == "Auto")
            )
                return true;

            return false;
        }

        public static bool ForceNoLineBreaks(string elementname)
        {
            if (
                elementname is "RadialGradientBrush" or "GradientStop" or "LinearGradientBrush" or "ScaleTransfom" or "SkewTransform" or "RotateTransform" or "TranslateTransform" or "Trigger" or "Setter"
            )
                return true;

            return false;
        }

        #endregion
    }

    // note that these are declared in priority order for easy sorting
    private enum AttributeType
    {
        Key = 10,
        Name = 20,
        Class = 30,
        Namespace = 40,
        CoreLayout = 50,
        AttachedLayout = 60,
        StandardLayout = 70,
        Other = 1000,
        BlendGoo = 2000
    }

    #region GoCommand

    public static readonly RoutedUICommand GoCommand = new("_Go", "GoCommand", typeof(XamlScrubberPlugin));

    private void Go_Executed(object sender, ExecutedRoutedEventArgs args)
    {
        if (Equals(sender, this)) Go();
    }

    private void Go_CanExecute(object sender, CanExecuteRoutedEventArgs args)
    {
        if (Equals(sender, this)) args.CanExecute = true;
    }

    #endregion


    #region Config Stuff

    private int _attributeCounteTolerance = 3;
    private bool _reorderAttributes = true;
    private bool _reducePrecision = true;
    private int _precision = 3;
    private bool _removeCommonDefaultValues = true;
    private bool _forceLineMin = true;
    private int _spaceCount = 2;
    private bool _convertTabsToSpaces = true;

    private void InitializeValues()
    {
        _attributeCounteTolerance = Settings.Default.AttributeCounteTolerance;
        _reorderAttributes = Settings.Default.ReorderAttributes;
        _reducePrecision = Settings.Default.ReducePrecision;
        _precision = Settings.Default.Precision;
        _removeCommonDefaultValues = Settings.Default.RemoveCommonDefaultValues;
        _forceLineMin = Settings.Default.ForceLineMin;
        _spaceCount = Settings.Default.SpaceCount;
        _convertTabsToSpaces = Settings.Default.ConvertTabsToSpaces;
    }

    private string IndentString
    {
        get
        {
            if (_convertTabsToSpaces)
            {
                var spaces = "";
                spaces = spaces.PadRight(_spaceCount, ' ');

                return spaces;
            }

            return "\t";
        }
    }

    #endregion
}