using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.TextEditor.Document;
using KaxamlPlugins.Utilities;

namespace Kaxaml.CodeCompletion;

/// <summary>
/// Determines folds for an xml string in the editor.
/// </summary>
public class XmlFoldingStrategy : IFoldingStrategy
{
    #region IFoldingStrategy

    public List<FoldMarker> GenerateFoldMarkers(IDocument document, string _, object parseInformation)
    {
        var xml = document.TextContent;
        if (string.IsNullOrWhiteSpace(xml)) return [];

        var showAttributes =
            parseInformation is not XmlFoldingOptions foldingOptions
            || foldingOptions.IsShowingAttributesWhenFolded;

        try
        {
            var foldStarts = XmlUtilities.CalculateXmlFolds(xml, showAttributes)
                .Where(fs => fs.EndLine >= fs.StartLine)
                .Select(fs => new FoldMarker(
                    document,
                    fs.StartLine,
                    fs.StartColumn,
                    fs.EndLine,
                    fs.EndColumn,
                    FoldType.TypeBody,
                    fs.FoldText))
                .ToList();

            return foldStarts;
        }
        catch (Exception ex)
        {
            if (ex.IsCriticalException()) throw;
            return document.FoldingManager.FoldMarker.ToList();
        }
    }

    #endregion
}