using FluentAssertions;
using KaxamlPlugins.Utilities.XmlComponents;
using static KaxamlPlugins.Utilities.XmlUtilities;

namespace Kaxaml.Tests.Utilities;

public sealed class XmlUtilitiesTests
{
    [Fact]
    public void AuditXmlTags_WhenXmlIsNull_ShouldReturnEmpty()
    {
        AuditXmlTags(null, null).Should().BeEmpty();
    }

    [Fact]
    public void AuditXmlTags_WhenXmlIsEmpty_ShouldReturnEmpty()
    {
        AuditXmlTags("", null).Should().BeEmpty();
    }

    [Theory]
    [InlineData("<Root></Root>")]
    [InlineData("<Root> </Root>")]
    [InlineData("<Root><Grid></Grid></Root>")]
    [InlineData("<Root> <Grid> </Grid> </Root>")]
    [InlineData("<Root><SelfClosing /><Another /></Root>")]
    public void AuditXmlTags_WhenXmlIsWellFormed_ShouldReturnEmpty(string xml)
    {
        var result = AuditXmlTags(xml, null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedOpen_ShouldIgnoreSelfClosingTags()
    {
        const string xml = "<Root><SelfClosing /><Bad> </Root>";
        var result = AuditXmlTags(xml, null);
        result.Should().NotBeEmpty();

        //Innermost are listed first
        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("Bad");
        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("Root");

        var second = result.Last();
        second.openTag.Should().NotBeNull();
        second.openTag.Name.Should().Be("Root");
        second.closeTag.Should().BeNull();
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedClosed_ShouldIgnoreSelfClosingTags()
    {
        const string xml = "<Root><SelfClosing /></Bad> </Root>";
        var result = AuditXmlTags(xml, null);
        result.Should().NotBeEmpty();

        //Innermost are listed first
        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("Root");
        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("Bad");

        var second = result.Last();
        second.openTag.Should().BeNull();
        second.closeTag.Should().NotBeNull();
        second.closeTag.Name.Should().Be("Root");
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedOpenTag_ShouldDetect()
    {
        const string xml = "<Root><Grid><Unclosed></Grid></Root>";
        var result = AuditXmlTags(xml, null);

        result.Count.Should().Be(3);

        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("Unclosed");
        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("Grid");

        var second = result.Skip(1).First();
        second.openTag.Should().NotBeNull();
        second.openTag.Name.Should().Be("Grid");
        second.closeTag.Should().NotBeNull();
        second.closeTag.Name.Should().Be("Root");

        var third = result.Last();
        third.openTag.Should().NotBeNull();
        third.openTag.Name.Should().Be("Root");
        third.closeTag.Should().BeNull();
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedClosedTag_ShouldDetect()
    {
        const string xml = "<Root></Orphan></Root>";
        var result = AuditXmlTags(xml, null);

        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("Root");

        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("Orphan");

        var second = result.Last();
        second.openTag.Should().BeNull();
        second.closeTag.Should().NotBeNull();
        second.closeTag.Name.Should().Be("Root");
    }

    [Fact]
    public void AuditXmlTags_WhenMismatchedPair_ShouldDetect()
    {
        const string xml = "<Root><Grid></Griddle></Root>";
        var result = AuditXmlTags(xml, null);
        result
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Match<(XmlTagInfo? openTag, XmlTagInfo? closeTag)>(pair =>
                pair.openTag != null &&
                pair.openTag.Name == "Grid" &&
                pair.closeTag != null &&
                pair.closeTag.Name == "Griddle");
    }

    [Fact]
    public void AuditXmlTags_WhenMaxTagCountOne_ShouldReturnOnlyFirst()
    {
        //Matches in order of innermost, then left to right
        const string xml = "<A><B></C></A><D></E>";
        var result = AuditXmlTags(xml, 1);
        result.Should().HaveCount(1);

        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("B");
        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("C");
    }

    [Fact]
    public void AuditXmlTags_WhenTagsHaveNamespaceAndDots_ShouldHandle()
    {
        const string xml = "<x:Page><GeometryModel3D.Material></GeometryModel3D.Material></x:Page>";
        var result = AuditXmlTags(xml, null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void AuditXmlTags_WhenMultipleUnmatched_ShouldDetect()
    {
        const string xml = "<Root><A><B></C></Root>";
        var result = AuditXmlTags(xml, null);
        result.Should().HaveCount(3);

        var first = result.First();
        first.openTag.Should().NotBeNull();
        first.openTag.Name.Should().Be("B");
        first.closeTag.Should().NotBeNull();
        first.closeTag.Name.Should().Be("C");

        var second = result.Skip(1).First();
        second.openTag.Should().NotBeNull();
        second.openTag.Name.Should().Be("A");
        second.closeTag.Should().NotBeNull();
        second.closeTag.Name.Should().Be("Root");

        var third = result.Last();
        third.openTag.Should().NotBeNull();
        third.openTag.Name.Should().Be("Root");
        third.closeTag.Should().BeNull();
    }

    [Fact]
    public void AuditXmlTags_WhenMultiLevel_ShouldCaptureCorrectStartIndex()
    {
        const string xml = "<Root><Grid></Grid1></Root1>";
        var result = AuditXmlTags(xml, null);

        var (gridOpen, gridClose) = result.First();
        gridOpen.Should().NotBeNull();
        gridOpen.Name.Should().Be("Grid");
        gridOpen.StartIndex.Should().Be(6);
        gridOpen.Should().NotBeNull();
        gridClose.Should().NotBeNull();
        gridClose.Name.Should().Be("Grid1");
        gridClose.StartIndex.Should().Be(12);

        var (rootOpen, rootCLose) = result.Last();
        rootOpen.Should().NotBeNull();
        rootOpen.Name.Should().Be("Root");
        rootOpen.StartIndex.Should().Be(0);
        rootCLose.Should().NotBeNull();
        rootCLose.Name.Should().Be("Root1");
        rootCLose.StartIndex.Should().Be(20);
    }

    [Fact]
    public void AuditXmlTagsWhenMultilevel_ShouldCaptureCorrectNameIndices()
    {
        const string xml = "<Root><Grid></Grid1></Root1>";
        var result = AuditXmlTags(xml, null);

        var (gridOpen, gridClose) = result.First();
        gridOpen.Should().NotBeNull();
        gridOpen.Name.Should().Be("Grid");
        gridOpen.NameStartIndex.Should().Be(7);
        gridOpen.Should().NotBeNull();
        gridClose.Should().NotBeNull();
        gridClose.Name.Should().Be("Grid1");
        gridClose.NameStartIndex.Should().Be(14);

        var (rootOpen, rootCLose) = result.Last();
        rootOpen.Should().NotBeNull();
        rootOpen.Name.Should().Be("Root");
        rootOpen.NameStartIndex.Should().Be(1);
        rootCLose.Should().NotBeNull();
        rootCLose.Name.Should().Be("Root1");
        rootCLose.NameStartIndex.Should().Be(22);
    }

    [Fact]
    public void AuditXmlTags_WhenMultiLevl_ShouldAssignCorrectDepth()
    {
        const string xml = "<Root><Grid><Inner></Inner1></Grid></Root>";
        var result = AuditXmlTags(xml, null);

        var (innerOpen, innerClose) = result.Single();

        innerOpen.Should().NotBeNull();
        innerOpen.Depth.Should().Be(2);

        innerClose.Should().NotBeNull();
        innerClose.Depth.Should().Be(2);
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedClose_ShouldHandleDepth()
    {
        //Both will be at the bottom
        const string xml = "<Root></Orphan></Root>";
        var result = AuditXmlTags(xml, null);

        var (firstOpen, firstClose) = result.First();
        firstOpen.Should().NotBeNull();
        firstOpen.Depth.Should().Be(0);
        firstClose.Should().NotBeNull();
        firstClose.Depth.Should().Be(0);

        var (secondOpen, secondClose) = result.Last();
        secondOpen.Should().BeNull();
        secondClose.Should().NotBeNull();
        secondClose.Depth.Should().Be(-1);
    }

    [Fact]
    public void AuditXmlTags_WhenUnmatchedOpen_ShouldHandleDepth()
    {
        const string xml = "<Root><Unclosed></Root>";
        var result = AuditXmlTags(xml, null);

        var (firstOpen, firstClose) = result.First();
        firstOpen.Should().NotBeNull();
        firstOpen.Depth.Should().Be(1);
        firstClose.Should().NotBeNull();
        firstClose.Depth.Should().Be(1);

        var (secondOpen, secondClose) = result.Last();
        secondOpen.Should().NotBeNull();
        secondOpen.Depth.Should().Be(0);
        secondClose.Should().BeNull();
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenXmlIsNull_ReturnsEmptyList()
    {
        var result = FindCommentAssemblyReferences(null);
        Assert.Empty(result);
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenXmlIsEmpty_ReturnsEmptyList()
    {
        var result = FindCommentAssemblyReferences(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenNoAssemblyReferenceCommentsExist_ReturnsEmptyList()
    {
        const string xml = "<Grid><!-- Just a regular comment --></Grid>";
        var result = FindCommentAssemblyReferences(xml);
        Assert.Empty(result);
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenSingleAssemblyReferenceComment_ReturnsDllPaths()
    {
        const string xml = """
                           <!--AssemblyReferences
                           C:\temp\Alpha.dll
                           C:\temp\Beta.dll
                           -->
                           """;

        var result = FindCommentAssemblyReferences(xml);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.FullName == @"C:\temp\Alpha.dll");
        Assert.Contains(result, f => f.FullName == @"C:\temp\Beta.dll");
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenMultipleAssemblyReferenceComments_ReturnsDllPaths()
    {
        const string xml = """
                           <!--AssemblyReferences
                           C:\temp\Alpha.dll
                           -->
                           <!--AssemblyReferences
                           C:\temp\Beta.dll
                           C:\temp\Gamma.dll
                           -->
                           """;

        var result = FindCommentAssemblyReferences(xml);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.FullName == @"C:\temp\Alpha.dll");
        Assert.Contains(result, f => f.FullName == @"C:\temp\Beta.dll");
        Assert.Contains(result, f => f.FullName == @"C:\temp\Gamma.dll");
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenNonDllLinesAssemblyReferenceComment_Ignores()
    {
        const string xml = """
                           <!--AssemblyReferences
                           C:\temp\Alpha.dll
                           NotAPath.txt
                           SomeOther.dll.config
                           -->
                           """;

        var result = FindCommentAssemblyReferences(xml);

        Assert.Single(result);
        Assert.Equal(@"C:\temp\Alpha.dll", result[0].FullName);
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenMixedLineEndingsAndWhitespace_Handles()
    {
        const string xml = "<!--AssemblyReferences\r\n  C:\\temp\\Alpha.dll  \n\tC:\\temp\\Beta.dll\r\n-->";
        var result = FindCommentAssemblyReferences(xml);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.FullName == @"C:\temp\Alpha.dll");
        Assert.Contains(result, f => f.FullName == @"C:\temp\Beta.dll");
    }

    [Fact]
    public void FindCommentAssemblyReferences_WhenDuplicateDllPaths_ReturnsDistinct()
    {
        const string xml = """
                           <!--AssemblyReferences
                           C:\temp\Alpha.dll
                           C:\temp\Alpha.dll
                           -->
                           """;

        var result = FindCommentAssemblyReferences(xml);

        Assert.Single(result);
        Assert.Equal(@"C:\temp\Alpha.dll", result[0].FullName);
    }

    [Fact]
    public void CalculateXmlFolds_WhenEmptyString_ReturnsEmptyList()
    {
        var result = CalculateXmlFolds(string.Empty, true);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateXmlFolds_WhenSingleLineSelfClosingTag_ReturnsEmptyList()
    {
        const string xml = @"<Page><TextBlock Text=""Hello""/></Page>";
        var result = CalculateXmlFolds(xml, true);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateXmlFolds_WhenWrappedMultiLineSelfClosingTag_ReturnsFold()
    {
        const string xml = @"<Page>
  <TextBlock 
    Text=""Hello"" 
    Foreground=""Red"" 
  />
</Page>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Name == "TextBlock");
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultiLineSelfClosingTag_ReturnsFold()
    {
        const string xml = @" 
<TextBlock 
    Text=""Hello"" 
    Foreground=""Red"" 
/>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Single(result);
        Assert.Contains(result, f => f.Name == "TextBlock");
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultiLineSelfClosingTagHasAttributes_IncludedInFoldText()
    {
        const string xml = @" 
<TextBlock 
    Text=""Hello"" 
    Foreground=""Red"" 
/>";

        var result = CalculateXmlFolds(xml, true);
        Assert.Single(result);
        Assert.Contains("Text=\"Hello\"", result.First().FoldText);
        Assert.DoesNotContain("Foreground=\"Red\"", result.First().FoldText);
        Assert.EndsWith(".../>", result.First().FoldText);
    }

    [Fact]
    public void CalculateXmlFolds_WhenShowAttributesTrue_FoldTextContainsAttributes()
    {
        const string xml = @"<Page Foreground=""Red"">
  <Grid Background=""Blue""></Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);
        var fold = Assert.Single(result, f => f.Name == "Page");
        Assert.Contains("Foreground=\"Red\"", fold.FoldText);
    }

    [Fact]
    public void CalculateXmlFolds_WhenShowAttributesFalse_FoldTextContainsOnlyTagName()
    {
        const string xml = @"<Page Foreground=""Red"">
  <Grid Background=""Blue""></Grid>
</Page>";

        var result = CalculateXmlFolds(xml, false);
        var fold = Assert.Single(result, f => f.Name == "Page");
        Assert.Equal("<Page>", fold.FoldText);
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultiLineComment_ReturnsCommentFold()
    {
        const string xml = @"<Page>
  <!--
    This is a
    multi-line comment
  -->
</Page>";

        var result = CalculateXmlFolds(xml, true);
        Assert.Contains(result, f => f.Name == "comment");

        var fold = result.First();
        Assert.Equal(1, fold.StartLine);
        Assert.Equal(2, fold.StartColumn);
        Assert.Equal(4, fold.EndLine);
        Assert.Equal(5, fold.EndColumn);
    }

    [Fact]
    public void CalculateXmlFolds_WhenNestedElements_AllNestedFoldsReturned()
    {
        const string xml = @"<Page>
  <Grid>
    <TextBlock Text=""Hello""/>
  </Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "Grid");
    }

    [Fact]
    public void CalculateXmlFolds_WhenNamespacePrefixedElement_PrefixIsPreservedInName()
    {
        const string xml = @"<Page>
  <x:Button 
    Content=""Click Me"" 
    Foreground=""Red"" 
  />
</Page>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "x:Button");
    }

    [Fact]
    public void CalculateXmlFolds_WhenDeeplyNestedStructure_AllLevelsReturned()
    {
        const string xml = @"<Page>
  <Grid>
    <StackPanel>
      <TextBlock 
        Text=""Hello"" 
        Foreground=""Red"" 
      />
    </StackPanel>
  </Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Equal(4, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "Grid");
        Assert.Contains(result, f => f.Name == "StackPanel");
        Assert.Contains(result, f => f.Name == "TextBlock");
    }

    [Fact]
    public void CalculateXmlFolds_WhenMalformedXml_ReturnsNoFolds()
    {
        const string xml = @"<Page>
  <Grid>
    <TextBlock Text=""Hello""
  </Grid>
</Page>"; // TextBlock never closed

        var result = CalculateXmlFolds(xml, true);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultipleSiblingElements_AllSiblingsReturned()
    {
        const string xml = @"<Page>
  <StackPanel>
    <TextBlock 
      Text=""First"" 
    />
    <TextBlock 
      Text=""Second"" 
    />
    <TextBlock 
      Text=""Third"" 
    />
  </StackPanel>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        Assert.Equal(5, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "StackPanel");
        Assert.Equal(3, result.Count(f => f.Name == "TextBlock"));
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultipleSameElements_ReturnsLinesAndColumns()
    {
        const string xml = @"<Page>
  <StackPanel>
    <TextBlock 
      Text=""First"" 
    />
    <TextBlock 
      Text=""Second"" 
    />
    <TextBlock 
      Text=""Third"" 
    />
  </StackPanel>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        var textBlocks = result.Where(f => f.Name == "TextBlock").ToList();
        var first = textBlocks.First();
        Assert.Equal(2, first.StartLine);
        Assert.Equal(4, first.StartColumn);
        Assert.Equal(4, first.EndLine);
        Assert.Equal(6, first.EndColumn);

        var second = textBlocks.Skip(1).First();
        Assert.Equal(5, second.StartLine);
        Assert.Equal(4, second.StartColumn);
        Assert.Equal(7, second.EndLine);
        Assert.Equal(6, second.EndColumn);

        var third = textBlocks.Skip(2).First();
        Assert.Equal(8, third.StartLine);
        Assert.Equal(4, third.StartColumn);
        Assert.Equal(10, third.EndLine);
        Assert.Equal(6, third.EndColumn);
    }

    [Fact]
    public void CalculateXmlFolds_WhenAttributesSpreadAcrossMultipleLines_FoldTextIsNormalizedToSingleLine()
    {
        const string xml = @"<Page>
  <Grid
    Background=""Blue""
    Margin=""10""
    Padding=""5""
  ></Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        var gridFold = Assert.Single(result, f => f.Name == "Grid");
        // FoldText should be normalized to a single line with attributes collapsed
        Assert.DoesNotContain("\n", gridFold.FoldText);
        Assert.Contains("Background=", gridFold.FoldText);
        Assert.DoesNotContain("Margin=", gridFold.FoldText);
        Assert.DoesNotContain("Padding=", gridFold.FoldText);
    }

    [Fact]
    public void CalculateXmlFolds_WhenCommentContainsAttributeLikeText_TreatedAsCommentNotElement()
    {
        const string xml = @"<Page>
  <!-- Background=""Blue"" 
       Margin=""10"" 
  -->
  <Grid>
    <TextBlock Text=""Hello""/>
  </Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        // Expect Page and Grid folds, plus a comment fold
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "Grid");
        Assert.Contains(result, f => f.Name == "comment");

        // Ensure no element fold was mistakenly created for "Background" or "Margin"
        Assert.DoesNotContain(result, f => f.Name.Contains("Background"));
        Assert.DoesNotContain(result, f => f.Name.Contains("Margin"));

        // FoldText for the comment should include the attribute-like text
        var commentFold = result.First(f => f.Name == "comment");
        Assert.Contains("Background=", commentFold.FoldText);
        Assert.DoesNotContain("Margin=", commentFold.FoldText);
    }

    [Fact]
    public void CalculateXmlFolds_WhenCDataSection_PreservedAsTextAndDoesNotProduceFolds()
    {
        const string xml = @"<Page>
  <Grid>
    <![CDATA[
      <TextBlock Text=""Hello"" Foreground=""Red""/>
    ]]>
  </Grid>
</Page>";

        var result = CalculateXmlFolds(xml, true);

        // Expect Page and Grid folds only
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Name == "Page");
        Assert.Contains(result, f => f.Name == "Grid");

        // Ensure no fold was created for the TextBlock inside CDATA
        Assert.DoesNotContain(result, f => f.Name == "TextBlock");

        // Ensure no fold was created for "CDATA"
        Assert.DoesNotContain(result, f => f.Name.Contains("CDATA"));
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultilevel_ReturnsLineAndColumnAccurately()
    {
        const string xml = @"<Page>
    <Grid>
        <!-- Test
            -->
            <TextBlock Text=""Hello""/>
     </Grid>
</Page>";

        var folds = CalculateXmlFolds(xml, false);
        var pageFold = folds.Single(f => f.Name == "Page");
        Assert.Equal(0, pageFold.StartLine);
        Assert.Equal(0, pageFold.StartColumn);
        Assert.Equal(6, pageFold.EndLine);
        Assert.Equal(7, pageFold.EndColumn);

        var commentFold = folds.Single(f => f.Name == "comment");
        Assert.Equal(2, commentFold.StartLine);
        Assert.Equal(8, commentFold.StartColumn);
        Assert.Equal(3, commentFold.EndLine);
        Assert.Equal(15, commentFold.EndColumn);

        var gridFold = folds.Single(f => f.Name == "Grid");
        Assert.Equal(1, gridFold.StartLine);
        Assert.Equal(4, gridFold.StartColumn);
        Assert.Equal(5, gridFold.EndLine);
        Assert.Equal(12, gridFold.EndColumn);

        Assert.DoesNotContain(folds, f => f.Name == "TextBlock");
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultilevelWithAttributes_ReturnsLineAndColumnAccurately()
    {
        const string xml = @"<Page>
    <Grid
        Foreground=""Red""
        Background=""Blue"">
        <!-- Test
            -->
            <TextBlock Text=""Hello""/>
     </Grid>
</Page>";

        var folds = CalculateXmlFolds(xml, false);
        var pageFold = folds.Single(f => f.Name == "Page");
        Assert.Equal(0, pageFold.StartLine);
        Assert.Equal(0, pageFold.StartColumn);
        Assert.Equal(8, pageFold.EndLine);
        Assert.Equal(7, pageFold.EndColumn);

        var gridFold = folds.Single(f => f.Name == "Grid");
        Assert.Equal(1, gridFold.StartLine);
        Assert.Equal(4, gridFold.StartColumn);
        Assert.Equal(7, gridFold.EndLine);
        Assert.Equal(12, gridFold.EndColumn);

        var commentFold = folds.Single(f => f.Name == "comment");
        Assert.Equal(4, commentFold.StartLine);
        Assert.Equal(8, commentFold.StartColumn);
        Assert.Equal(5, commentFold.EndLine);
        Assert.Equal(15, commentFold.EndColumn);

        Assert.DoesNotContain(folds, f => f.Name == "TextBlock");
    }

    [Fact]
    public void CalculateXmlFolds_WhenSelfContainedIsMultilevel_ReturnsLineAndColumnAccurately()
    {
        const string xml = @"<TextBlock 
    Text=""Hello"" />";

        var folds = CalculateXmlFolds(xml, true);
        var fold = folds.Single(f => f.Name == "TextBlock");
        Assert.Equal(0, fold.StartLine);
        Assert.Equal(0, fold.StartColumn);
        Assert.Equal(1, fold.EndLine);
        Assert.Equal(19, fold.EndColumn);
    }

    [Fact]
    public void CalculateXmlFolds_WhenSelfContainedIsMultilevelSeparateEnd_ReturnsLineAndColumnAccurately()
    {
        const string xml = @"<TextBlock 
    Text=""Hello""
    />";

        var folds = CalculateXmlFolds(xml, true);
        var fold = folds.Single(f => f.Name == "TextBlock");
        Assert.Equal(0, fold.StartLine);
        Assert.Equal(0, fold.StartColumn);
        Assert.Equal(2, fold.EndLine);
        //Assert.Equal(6, fold.EndColumn);
    }

    [Fact]
    public void CalculateXmlFolds_WhenMultilineComment_ReturnsLineAndColumnAccurately()
    {
        const string xml = @"<Page>
    <!--
        This is a multi-line
        comment block
    -->
    <Grid></Grid>
</Page>";

        var folds = CalculateXmlFolds(xml, false);
        var commentFold = folds.Single(f => f.Name == "comment");

        Assert.Equal(1, commentFold.StartLine);
        Assert.Equal(4, commentFold.StartColumn);
        Assert.Equal(4, commentFold.EndLine);
        Assert.Equal(7, commentFold.EndColumn);

        // FoldText should be truncated representation
        Assert.StartsWith("<!--", commentFold.FoldText);
        Assert.EndsWith("...", commentFold.FoldText);
    }
}