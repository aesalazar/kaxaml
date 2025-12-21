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
        const string input = @"
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

        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        Assert.Equal(4, splits.Length);
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

        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        Assert.Equal(4, splits.Length);
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
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(4, splits.Length);
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
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);

        Assert.Equal(8, splits.Length);
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
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);

        Assert.Equal(7, splits.Length);
        Assert.Equal("<Page", splits[0]);
        Assert.Equal("  <Grid>", splits[3]);
    }

    [Fact]
    public void Index_WhenMappingDeclarationPresent_Remains()
    {
        const string input = @"<?Mapping version=""1.0"" encoding=""utf-8"" ?>
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  </Grid>
</Page>";
        var printer = CreatePrinter(attributeCountTolerance: 0);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);

        Assert.Equal(8, splits.Length);
        Assert.Equal("<?Mapping version=\"1.0\" encoding=\"utf-8\" ?>", splits[0]);
    }

    [Fact]
    public void Index_WhenMultiLineText_IndentsEqually()
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
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        Assert.Equal(8, splits.Length);
        Assert.Equal("    Line 1", splits[2]); //4 spaces
        Assert.Equal("    Line 2", splits[3]); //4 spaces
        Assert.Equal("    Line 3", splits[4]); //4 spaces
    }

    [Fact]
    public void Indent_WhenWrappingElementLongLines_BreaksOnWordsWithSecondaryTabs()
    {
        const string input = @"
<Page xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  
  </Grid>
</Page>
";
        var printer = CreatePrinter(isLongLineWrapping: true, longLineWrappingThreshold: 50, indentWidth: 3);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);

        Assert.Equal(7, splits.Length);
        Assert.Equal("<Page", splits[0]);
        Assert.Equal(@"   xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""", splits[1]); //3 spaces
        Assert.Equal(@"   xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">", splits[2]); //3 spaces
    }

    [Fact]
    public void Indent_WhenWrappingTextLongLines_BreaksOnWordsWithoutSecondaryTabs()
    {
        const string input = @"
<Page>
  <TextBlock>
123456789 987654321 ABCDEFGHI IHGFEDCBA
  </TextBlock>
</Page>
";
        var printer = CreatePrinter(isLongLineWrapping: true, longLineWrappingThreshold: 10, indentWidth: 3);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);

        Assert.Equal(9, splits.Length);
        Assert.Equal("<Page>", splits[0]);
        Assert.Equal("   <TextBlock>", splits[1]); //3 spaces
        Assert.Equal("      123456789", splits[2]); //6 spaces
        Assert.Equal("      987654321", splits[3]);
        Assert.Equal("      ABCDEFGHI", splits[4]);
        Assert.Equal("      IHGFEDCBA", splits[5]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_LeavesXmlDeclarationAlone()
    {
        const string input = @"<?Mapping version=""1.0"" encoding=""utf-8""?>
<Page
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>  
  </Grid>
</Page>";
        var printer = CreatePrinter(isLongLineWrapping: true, longLineWrappingThreshold: 10);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(@"<?Mapping version=""1.0"" encoding=""utf-8"" ?>", splits[0]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_SpaceAttributesOnSameLineCorrectly()
    {
        const string input = @"
<Page>
  <Grid   Height=""9""   Opacity=""0.1"">
  </Grid>
</Page>";
        var printer = CreatePrinter(isLongLineWrapping: true, longLineWrappingThreshold: 50);
        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(5, splits.Length);
        Assert.Equal("""  <Grid Height="9" Opacity="0.1">""", splits[1]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_BreaksOnUnclosedEmptyTags()
    {
        const string input = @"
<Page>
  <TextBlock></TextBlock>
  <Abcdefghijklmnopqustuvwxyz></Abcdefghijklmnopqustuvwxyz>
</Page>";
        var printer = CreatePrinter(
            isLongLineWrapping: true,
            longLineWrappingThreshold: 50,
            isEmptyNonSelfClosingSingleLine: true);

        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(6, splits.Length);
        Assert.Equal("  <TextBlock></TextBlock>", splits[1]);
        Assert.Equal("  <Abcdefghijklmnopqustuvwxyz>", splits[2]);
        Assert.Equal("  </Abcdefghijklmnopqustuvwxyz>", splits[3]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_OnlyBreaksOnSpacesOutsideOfQuotes()
    {
        const string input = @"
<Page>
    <Path Width=""21"" Height=""21"" Canvas.Left=""39"" Canvas.Top=""123"" Stretch=""Fill"" Fill=""#FFFFFFFF"" Data=""F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z ""/>
</Page>
";
        var printer = CreatePrinter(
            isLongLineWrapping: true,
            longLineWrappingThreshold: 110,
            reorderAttributes: false,
            indentWidth: 4,
            attributeCountTolerance: 100,
            isEmptyNonSelfClosingSingleLine: true);

        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(6, splits.Length);
        Assert.Equal("    <Path Width=\"21\" Height=\"21\" Canvas.Left=\"39\" Canvas.Top=\"123\" Stretch=\"Fill\" Fill=\"#FFFFFFFF\"", splits[1]);
        Assert.Equal("        Data=\"F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z \"", splits[2]);
        Assert.Equal("        />", splits[3]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_UsesAttributeCountToleranceFirst()
    {
        const string input = @"
<Page>
    <Path Width=""21"" Height=""21"" Canvas.Left=""39"" Canvas.Top=""123"" 
        Data=""F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z ""
        Stretch=""Fill"" Fill=""#FFFFFFFF""/>
</Page>
";
        var printer = CreatePrinter(
            isLongLineWrapping: true,
            longLineWrappingThreshold: 10000, //Fits all but tolerance takes precedent
            reorderAttributes: false,
            attributeCountTolerance: 6, //One too many attributes
            indentWidth: 4,
            isEmptyNonSelfClosingSingleLine: true);

        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(11, splits.Length);
        Assert.Equal("    <Path", splits[1]);
        Assert.Equal("        Width=\"21\"", splits[2]);
        Assert.Equal("        Height=\"21\"", splits[3]);
        Assert.Equal("        Data=\"F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z \"", splits[6]);
        Assert.Equal("        Fill=\"#FFFFFFFF\" />", splits[8]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_UsesAttributeCountToleranceFirst2()
    {
        const string input = @"
<Page>
    <Path Width=""21"" 
    Data=""F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z ""
    Height=""21"" Canvas.Left=""39"" Canvas.Top=""123"" Stretch=""Fill"" Fill=""#FFFFFFFF""/>
</Page>
";
        var printer = CreatePrinter(
            isLongLineWrapping: true,
            longLineWrappingThreshold: 10000, //Fits all but tolerance takes precedent
            reorderAttributes: false,
            attributeCountTolerance: 6, //One too many attributes
            indentWidth: 4,
            isEmptyNonSelfClosingSingleLine: true);

        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(11, splits.Length);
        Assert.Equal("    <Path", splits[1]);
        Assert.Equal("        Width=\"21\"", splits[2]);
        Assert.Equal("        Data=\"F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z \"", splits[3]);
        Assert.Equal("        Fill=\"#FFFFFFFF\" />", splits[8]);
    }

    [Fact]
    public void Index_WhenWrappingLongLines_UsesAttributeCountToleranceFirst3()
    {
        const string input = @"
<Page>
    <Path Width=""21"" Height=""21"" Canvas.Left=""39"" Canvas.Top=""123"" 
        Data=""F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z ""
        Stretch=""Fill"" Fill=""#FFFFFFFF""/>
</Page>
";
        var printer = CreatePrinter(
            isLongLineWrapping: true,
            longLineWrappingThreshold: 50, //Would cause line break but attribute tolerance should force to single for all
            reorderAttributes: false,
            attributeCountTolerance: 6, //One too many attributes
            indentWidth: 4,
            isEmptyNonSelfClosingSingleLine: true);

        var result = printer.Indent(input);
        var splits = result.Split([Environment.NewLine], StringSplitOptions.None);
        output.WriteLine(result);
        Assert.Equal(11, splits.Length);
        Assert.Equal("    <Path", splits[1]);
        Assert.Equal("        Width=\"21\"", splits[2]);
        Assert.Equal("        Height=\"21\"", splits[3]);
        Assert.Equal("        Data=\"F1 M 50,122.537C 55,123 60,127 60,132.935C 60,139 55,143 50,143.334C 44,143 39,139 39,132.935C 39,127 44,123 50,123 Z \"", splits[6]);
        Assert.Equal("        Fill=\"#FFFFFFFF\" />", splits[8]);
    }

    [Fact]
    public void Indent_WhenCommentsArePresent_AreIgnored()
    {
        const string input = @"
<!--Comment0 Line0 Line1-->
<Page>
    <!--
        Comment1
        Line2 
        Line3-->
</Page>
";
        var printer = CreatePrinter(
            indentWidth: 4,
            isLongLineWrapping:true,
            longLineWrappingThreshold: 5);
        var result = printer.Indent(input);
        var splits = result.Split(Environment.NewLine);
        output.WriteLine(result);
        
        Assert.Equal(8, splits.Length);
        Assert.Contains("<!--Comment0 Line0 Line1-->", splits[0]);
        Assert.Contains("<Page>", splits[1]);
        Assert.Contains("    <!--", splits[2]);
        Assert.Contains("        Comment1", splits[3]);
        Assert.Contains("        Line2", splits[4]);
        Assert.Contains("        Line3-->", splits[5]);
        Assert.Contains("</Page>", splits[6]);
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
        bool isEmptyNonSelfClosingSingleLine = false,
        bool isLongLineWrapping = false,
        int longLineWrappingThreshold = 100)
    {
        var config = new XamlPrettyPrintConfig(
            attributeCountTolerance,
            reorderAttributes,
            reducePrecision,
            precision,
            removeCommonDefaults,
            indentWidth,
            convertTabsToSpaces,
            isEmptyNonSelfClosingSingleLine,
            isLongLineWrapping,
            longLineWrappingThreshold);

        return new XamlPrettyPrinter(config);
    }

    #endregion
}