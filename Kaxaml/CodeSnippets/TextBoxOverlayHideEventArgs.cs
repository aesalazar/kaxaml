using System;

namespace Kaxaml.CodeSnippets;

public sealed class TextBoxOverlayHideEventArgs : EventArgs
{
    #region Constructors

    public TextBoxOverlayHideEventArgs(TextBoxOverlayResult result, string resultText)
    {
        Result = result;
        ResultText = resultText;
    }

    #endregion Constructors

    #region Fields

    public readonly string ResultText;

    public readonly TextBoxOverlayResult Result;

    #endregion Fields
}