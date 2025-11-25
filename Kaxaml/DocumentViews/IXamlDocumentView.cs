using System;
using Kaxaml.Documents;
using KaxamlPlugins;

namespace Kaxaml.DocumentViews;

public interface IXamlDocumentView : IDisposable
{
    IKaxamlInfoTextEditor TextEditor { get; }

    XamlDocument? XamlDocument { get; }

    double Scale { get; }

    void Parse();

    void OnActivate();

    void ReportError(Exception e);
}