using Kaxaml.Plugins.XamlScrubber.XamlPrettyPrint;
using Xunit.Abstractions;

namespace Kaxaml.Tests.XamlScrubberPlugin;

public sealed class XamlPrettyPrinterTests(ITestOutputHelper output)
{
    [Fact]
    public void ReducePrecision_WhenDisabled_ReturnsOriginal()
    {
        const string input = "Width=\"12.34567\"";
        var printer = CreatePrinter(false);
        var result = printer.ReducePrecision(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void ReducePrecision_WhenPrecisionIs3_TrimsExtraDigits()
    {
        const string input = "Width=\"12.34567\"";
        var printer = CreatePrinter(precision: 3);
        var result = printer.ReducePrecision(input);

        Assert.Equal("Width=\"12.345\"", result);
    }

    [Fact]
    public void ReducePrecision_WhenNoDecimal_ReturnsOriginal()
    {
        const string input = "Width=\"123\"";
        var printer = CreatePrinter();
        var result = printer.ReducePrecision(input);

        Assert.Equal("Width=\"123\"", result);
    }

    // --- Indent tests ---

    [Fact]
    public void Indent_WhenSingleElement_ProducesIndentedOutput()
    {
        const string input = "<Grid></Grid>";
        var printer = CreatePrinter(indentWidth: 2, convertTabsToSpaces: true);
        var result = printer.Indent(input);

        Assert.Contains("<Grid>", result);
        Assert.Contains("</Grid>", result);
    }

    [Fact]
    public void Indent_WhenElementHasAttributes_ReordersAttributes()
    {
        const string input = "<Button Width=\"100\" Name=\"MyButton\"></Button>";
        var printer = CreatePrinter(reorderAttributes: true);
        var result = printer.Indent(input);

        // Expect Name before Width due to AttributeType ordering
        Assert.Contains("Name=\"MyButton\"", result);
        Assert.Contains("Width=\"100\"", result);
        Assert.True(
            result.IndexOf("Name=\"MyButton\"", StringComparison.Ordinal)
            < result.IndexOf("Width=\"100\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Indent_WhenRemoveCommonDefaults_RemovesDefaultValues()
    {
        const string input = "<Button HorizontalAlignment=\"Stretch\"></Button>";
        var printer = CreatePrinter(removeCommonDefaults: true);
        var result = printer.Indent(input);

        // Default HorizontalAlignment=Stretch should be removed
        Assert.DoesNotContain("HorizontalAlignment", result);
    }

    [Fact]
    public void Indent_WhenConvertTabsToSpaces_UsesSpaces()
    {
        const string input = "<StackPanel><Button /></StackPanel>";
        var printer = CreatePrinter(indentWidth: 4, convertTabsToSpaces: true);
        var result = printer.Indent(input);

        Assert.Contains("    <Button", result); // 4 spaces
    }

    [Fact]
    public void Indent_WhenConvertTabsToSpacesFalse_UsesTabs()
    {
        const string input = "<StackPanel><Button /></StackPanel>";
        var printer = CreatePrinter(convertTabsToSpaces: false);
        var result = printer.Indent(input);

        Assert.Contains("\t<Button", result);
    }

    [Fact]
    public void Indent_WhenNestedElements_ProducesCorrectHierarchy()
    {
        const string input = "<Grid><StackPanel><Button /></StackPanel></Grid>";
        var printer = CreatePrinter();
        var result = printer.Indent(input);

        // Expect nested indentation
        Assert.Contains("<Grid>", result);
        Assert.Contains("  <StackPanel>", result); // 2 spaces
        Assert.Contains("    <Button", result); // 4 spaces
    }

    [Fact]
    public void Indent_WhenGradientStopAttributes_ForceNoLineBreaks()
    {
        const string input = @"
<GradientStop 
    Color=""Red""
    Offset=""0.5"" />";
        var printer = CreatePrinter(attributeCountTolerance: 1, reorderAttributes: false);
        var result = printer.Indent(input);

        // GradientStop should force attributes on one line
        Assert.Contains("Color=\"Red\" Offset=\"0.5\"", result);

        // Ensure attributes are on the same line (no line break between them)
        var firstLine = result.Split(["\r\n"], StringSplitOptions.None)[0];
        Assert.Contains("Color=\"Red\" Offset=\"0.5\"", firstLine);
    }

    [Fact]
    public void Indent_WhenLinearGradientBrushAttributes_ForceNoLineBreaks()
    {
        const string input = "<LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"1,1\" />";
        var printer = CreatePrinter(attributeCountTolerance: 1);
        var result = printer.Indent(input);

        // LinearGradientBrush should keep attributes inline
        Assert.Contains("StartPoint=\"0,0\" EndPoint=\"1,1\"", result);
    }

    [Fact]
    public void Indent_WhenWidthAndHeightAttributes_OrdersWidthBeforeHeight()
    {
        const string input = "<Button Height=\"100\" Width=\"200\"></Button>";
        var printer = CreatePrinter();
        var result = printer.Indent(input);

        // Width should appear before Height
        Assert.True(
            result.IndexOf("Width=\"200\"", StringComparison.Ordinal)
            < result.IndexOf("Height=\"100\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Indent_WhenSetterAttributes_OrdersTargetNameBeforeProperty()
    {
        const string input = "<Setter Property=\"Background\" TargetName=\"MyElement\" />";
        var printer = CreatePrinter();
        var result = printer.Indent(input);

        // TargetName should appear before Property
        Assert.True(
            result.IndexOf("TargetName=\"MyElement\"", StringComparison.Ordinal)
            < result.IndexOf("Property=\"Background\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Indent_WhenRemoveCommonDefaults_RemovesMarginZero()
    {
        const string input = "<Border Margin=\"0\"></Border>";
        var printer = CreatePrinter(removeCommonDefaults: true);
        var result = printer.Indent(input);

        // Margin=0 should be removed
        Assert.DoesNotContain("Margin", result);
    }

    [Fact]
    public void Indent_WhenSelfClosingEmptyElement_RespectsSelfCloseTag()
    {
        const string input = "<TextBlock />";
        var printer = CreatePrinter();
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock />", result);
    }

    [Fact]
    public void Indent_WhenSelfClosingEmptyElementWithAttributes_RespectsSelfCloseTag()
    {
        const string input = "<TextBlock Text=\"Test Text\" />";
        var printer = CreatePrinter();
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock Text=\"Test Text\" />", result);
    }

    [Fact]
    public void Indent_WhenCommentsAboveElements_AlignsWithElementOpen()
    {
        const string input
            = @"
<!--Test0-->
<Page
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
<!--Test1-->
    <Grid>
<!--Test2-->
        <TextBlock Text=""Test Text"" />
    </Grid>
</Page>
";
        var printer = CreatePrinter(
            indentWidth: 4,
            attributeCountTolerance: 0);
        var result = printer.Indent(input);

        var splits = result.Split(Environment.NewLine);

        Assert.Contains("<!--Test0-->", splits[0]);
        Assert.Contains("    <!--Test1-->", splits[4]); // 4 spaces
        Assert.Contains("        <!--Test2-->", splits[6]); // 8 spaces
    }

    [Fact]
    public void Index_WhenEmptyNonSelfClosing_RemainsNonSelfClosing()
    {
        const string input = "<TextBlock> </TextBlock>";
        var printer = CreatePrinter(attributeCountTolerance: 0);
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock>\r\n</TextBlock>", result);
    }

    [Fact]
    public void Index_WhenContainsInlineContent_BreaksIntoSeparateLines()
    {
        const string input = """<TextBlock ToolTip="Test Text">Test Test Inline</TextBlock>""";
        var printer = CreatePrinter(indentWidth: 4);
        var result = printer.Indent(input);

        output.WriteLine("RESULT:");
        output.WriteLine(result);

        var splits = result.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, splits.Length);
        Assert.Equal("    Test Test Inline", splits[1]);
    }

    [Fact]
    public void Index_WhenRepeated_KeepsSameFormat()
    {
        const string input = @"
<Grid>
        <TextBlock>
Line 1
    Line 2
Line 3
        </TextBlock>
</Grid>";

        var printer = CreatePrinter();
        var result1 = printer.Indent(input);
        var result2 = printer.Indent(result1);
        var result3 = printer.Indent(result2);

        output.WriteLine("RESULT1:");
        output.WriteLine(result1);
        Assert.Equal(result1, result3);
    }

    [Fact]
    public void Index_WhenIsEmptyNonSelfClosingSingleLine_KeepsEmptyTagOnSingleLine()
    {
        const string input = "<TextBlock></TextBlock>";
        var printer = CreatePrinter(isEmptyNonSelfClosingSingleLine: true);
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock></TextBlock>", result);
    }

    [Fact]
    public void Index_WhenIsEmptyNonSelfClosingSingleLine_CrushesWhitespaceOnlyContent()
    {
        const string input = "<TextBlock> </TextBlock>";
        var printer = CreatePrinter(isEmptyNonSelfClosingSingleLine: true);
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock></TextBlock>", result);
    }

    [Fact]
    public void Index_WhenIsEmptyNonSelfClosingSingleLineWithAttributes_KeepsOnSingleLine()
    {
        const string input = @"
<TextBlock Text=""Test Text"">
</TextBlock>";
        var printer = CreatePrinter(isEmptyNonSelfClosingSingleLine: true);
        var result = printer.Indent(input);
        Assert.Contains("<TextBlock Text=\"Test Text\"></TextBlock>", result);
    }

    [Fact]
    public void Index_WhenIsEmptyNonSelfClosingSingleLineButContainsSelfClosing_ReturnsExpected()
    {
        const string input = @"
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
<Ellipse
/>
</Page>
";
        var printer = CreatePrinter(isEmptyNonSelfClosingSingleLine: true);
        var result = printer.Indent(input);
        output.WriteLine(result);

        var splits = result.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, splits.Length);
        Assert.Equal("  <Ellipse />", splits[1]);
    }

    [Fact]
    public void Index_WhenIsEmptyNonSelfClosingSingleLine_KeepsOnSingleLine()
    {
        const string input = @"
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  
  </Grid>
</Page>";
        var printer = CreatePrinter(isEmptyNonSelfClosingSingleLine: true);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        output.WriteLine(result);
        Assert.Equal(3, splits.Length);
        Assert.Equal("  <Grid></Grid>", splits[1]);
    }

    [Fact]
    public void Index_WhenProcessingInstructionOnFirstLine_IsChangedToMapping()
    {
        const string input = @"<?xaml-comp compile=""true""?>
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  </Grid>
</Page>";
        var printer = CreatePrinter(attributeCountTolerance: 0);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        output.WriteLine(result);

        Assert.Equal(7, splits.Length);
        Assert.Equal("<?Mapping compile=\"true\" ?>", splits[0]);
        Assert.Equal("<Page", splits[1]);
        Assert.Equal("  <Grid>", splits[4]);
    }

    [Fact]
    public void Index_WhenXmlDeclarationPresent_IsRemoved()
    {
        const string input = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  </Grid>
</Page>";
        var printer = CreatePrinter(attributeCountTolerance: 0);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
        output.WriteLine(result);

        Assert.Equal(6, splits.Length);
        Assert.Equal("<Page", splits[0]);
        Assert.Equal("  <Grid>", splits[3]);
    }

    #region Helpers

    private static XamlPrettyPrinter CreatePrinter(
        bool reducePrecision = true,
        int precision = 3,
        bool reorderAttributes = true,
        bool removeCommonDefaults = true,
        int attributeCountTolerance = 3,
        int indentWidth = 2,
        bool convertTabsToSpaces = true,
        bool isEmptyNonSelfClosingSingleLine = false)
    {
        var config = new XamlPrettyPrintConfig(
            attributeCountTolerance,
            reorderAttributes,
            reducePrecision,
            precision,
            removeCommonDefaults,
            indentWidth,
            convertTabsToSpaces,
            isEmptyNonSelfClosingSingleLine);

        return new XamlPrettyPrinter(config);
    }

    #endregion
}