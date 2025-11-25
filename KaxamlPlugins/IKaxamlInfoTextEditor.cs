using System.Windows;

namespace KaxamlPlugins;

public interface IKaxamlInfoTextEditor : IDisposable
{
    // Properties
    string SelectedText { get; }
    int CaretIndex { get; set; }
    string Text { get; set; }
    int LineNumber { get; }
    int LinePosition { get; }

    // Methods
    void InsertCharacter(char ch);
    void InsertStringAtCaret(string s);
    void InsertString(string s, int offset);

    /// <summary>
    /// Replaces a section of text with another.
    /// </summary>
    /// <param name="beginIndex">The starting character index in the document where the replacement begins.</param>
    /// <param name="count">The number of characters to remove starting from offset.</param>
    /// <param name="s">The new string to insert in place of the removed text.</param>
    /// <returns>String that was replaced.</returns>
    string? ReplaceString(int beginIndex, int count, string s);

    void RemoveString(int beginIndex, int count);
    void Find(string s);
    void FindNext();
    void Replace(string s, string replacement, bool selectedonly);
    void ReplaceSelectedText(string s);
    void Undo();
    void Redo();

    // Events
    event RoutedEventHandler TextSelectionChanged;
}