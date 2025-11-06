using System;
using Kaxaml.Documents;
using KaxamlPlugins;

namespace Kaxaml.DocumentViews;

public interface IXamlDocumentView
{
    IKaxamlInfoTextEditor TextEditor { get; }

    XamlDocument? XamlDocument { get; }

    void Parse();

    void OnActivate();

    void ReportError(Exception e);
}