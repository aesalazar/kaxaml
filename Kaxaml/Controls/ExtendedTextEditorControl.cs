using System;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using KaxamlPlugins.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaxaml.Controls;

/// <summary>
/// Local extension of the <see cref="TextEditorControl"/>.
/// </summary>
public sealed class ExtendedTextEditorControl : TextEditorControl
{
    private readonly ILogger _logger;

    public ExtendedTextEditorControl()
    {
        _logger = ApplicationDiServiceProvider.Services.GetRequiredService<ILogger<ExtendedTextEditorControl>>();
        _logger.LogInformation("Created.");

        document.UndoStack.ActionUndone += UndoStack_OnActionUndone;
        document.UndoStack.ActionRedone += UndoStack_OnActionRedone;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            // Notify for Ctrl+Z but only if there is something to do
            case Keys.Control | Keys.Z when document.UndoStack.CanUndo:
                _logger.LogDebug("Invoking UNDO Start Event");
                UndoStarted?.Invoke(this, EventArgs.Empty);
                break;

            // Notify for Ctrl+Y but only if there is something to do
            case Keys.Control | Keys.Y when document.UndoStack.CanRedo:
                _logger.LogDebug("Invoking REDO Start Event");
                RedoStarted?.Invoke(this, EventArgs.Empty);
                break;
        }

        _logger.LogDebug("Passing keys to base: {Key}", keyData);
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        document.UndoStack.ActionUndone -= UndoStack_OnActionUndone;
        document.UndoStack.ActionRedone -= UndoStack_OnActionRedone;
    }

    /// <summary>
    /// Fires when the user invokes an undo (e.g. Ctrl+Z).
    /// </summary>
    public event EventHandler? UndoStarted;

    /// <summary>
    /// Fires when the user invoked undo is complete.
    /// </summary>
    public event EventHandler? UndoCompleted;

    /// <summary>
    /// Fires when the user invokes an undo (e.g. Ctrl+Y).
    /// </summary>
    public event EventHandler? RedoStarted;

    /// <summary>
    /// Fires when the user invoked redo is complete.
    /// </summary>
    public event EventHandler? RedoCompleted;

    private void UndoStack_OnActionUndone(object? _, EventArgs __)
    {
        _logger.LogInformation("Invoking UNDO Completed Event");
        UndoCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void UndoStack_OnActionRedone(object? sender, EventArgs e)
    {
        _logger.LogInformation("Invoking REDO Completed Event.,");
        RedoCompleted?.Invoke(this, EventArgs.Empty);
    }
}