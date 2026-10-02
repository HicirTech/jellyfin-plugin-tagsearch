using Jellyfin.Plugin.TagSearch.Search;
using Xunit;

namespace Jellyfin.Plugin.TagSearch.Tests;

public class TagQueryTests
{
    [Theory]
    [InlineData("辣妹, 巨乳")]
    [InlineData("辣妹，巨乳")]
    [InlineData("辣妹、巨乳")]
    [InlineData(" 辣妹 ,, 巨乳 ,")]
    public void Parse_SplitsOnEveryCommaKind(string text)
    {
        var query = TagQuery.Parse(text);

        Assert.NotNull(query);
        Assert.Equal(["辣妹", "巨乳"], query.Include);
        Assert.Empty(query.Exclude);
    }

    [Fact]
    public void Parse_TreatsLeadingMinusAsExclusion()
    {
        var query = TagQuery.Parse("巨乳, -中出, －單體作品");

        Assert.NotNull(query);
        Assert.Equal(["巨乳"], query.Include);
        Assert.Equal(["中出", "单体作品"], query.Exclude);
    }

    [Fact]
    public void Parse_KeepsSpacesInsideATerm()
    {
        var query = TagQuery.Parse("ラフティン マエシロ");

        Assert.NotNull(query);
        Assert.Equal(["ラフティン マエシロ"], query.Include);
    }

    [Fact]
    public void Parse_DropsRepeatedTerms()
    {
        var query = TagQuery.Parse("濫交, 滥交");

        Assert.NotNull(query);
        Assert.Equal(["滥交"], query.Include);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",，、")]
    [InlineData("-中出")]
    public void Parse_ReturnsNullWithoutATagToInclude(string? text)
    {
        Assert.Null(TagQuery.Parse(text));
    }
}
