using Kaxaml.Properties;

namespace Kaxaml.Documents;

internal class WpfDocument : XamlDocument
{
    #region Constructors

    public WpfDocument(string folder, string sourceText)
        : base(folder)
    {
        XamlDocumentType = XamlDocumentType.WpfDocument;
        InitializeSourceText(sourceText);
    }

    public WpfDocument(string folder) : base(folder)
    {
        XamlDocumentType = XamlDocumentType.WpfDocument;
        InitializeSourceText(Settings.Default.WPFDefaultXaml);
    }

    #endregion Constructors
}