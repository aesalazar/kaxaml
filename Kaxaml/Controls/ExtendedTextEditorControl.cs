using System;
using System.Windows.Forms;
using ICSharpCode.TextEditor;

namespace Kaxaml.Controls;

/// <summary>
/// Local extension of the <see cref="TextEditorControl"/>.
/// </summary>
public sealed class ExtendedTextEditorControl : TextEditorControl
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            // Notify for Ctrl+Z but only if there is something to do
            case Keys.Control | Keys.Z when Document.UndoStack.UndoItemCount is not 0:
                UndoTriggered?.Invoke(this, EventArgs.Empty);
                break;

            // Notify for Ctrl+Y but only if there is something to do
            case Keys.Control | Keys.Y when Document.UndoStack.RedoItemCount is not 0:
                RedoTriggered?.Invoke(this, EventArgs.Empty);
                break;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>
    /// Fires when the user invokes an undo (e.g. Ctrl+Z).
    /// </summary>
    public event EventHandler? UndoTriggered;

    /// <summary>
    /// Fires when the user invokes an undo (e.g. Ctrl+Y).
    /// </summary>
    public event EventHandler? RedoTriggered;
}