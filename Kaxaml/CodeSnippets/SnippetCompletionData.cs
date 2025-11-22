using System;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace Kaxaml.CodeSnippets;

public sealed class SnippetCompletionData : ICompletionData
{
    #region Constructors

    public SnippetCompletionData(string description, string text, Snippet snippet)
    {
        Description = description;
        Text = text;
        Snippet = snippet;
    }

    #endregion Constructors

    #region IComparable Members

    public int CompareTo(object obj)
    {
        var s = (SnippetCompletionData)obj;
        return string.Compare(s.Text, Text, StringComparison.Ordinal);
    }

    #endregion

    #region ICompletionData Members

    public string Description { get; }

    public int ImageIndex => 0;

    public bool InsertAction(TextArea textArea, char ch) => true;

    public double Priority => 0;

    public string Text { get; set; }

    public Snippet Snippet { get; set; }

    #endregion
}