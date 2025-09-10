using FluentAssertions;
using static KaxamlPlugins.Utilities.XmlUtilities;

namespace Kaxaml.Tests.Utilities;

public class XmlUtilitiesTests
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
}