using Jellyfin.Plugin.TagSearch.Search;
using Xunit;

namespace Jellyfin.Plugin.TagSearch.Tests;

public class TagNormalizerTests
{
    [Theory]
    [InlineData("濫交", "滥交")]
    [InlineData("單體作品", "单体作品")]
    [InlineData("女同性戀", "女同性恋")]
    public void Normalize_FoldsTraditionalToSimplified(string traditional, string simplified)
    {
        Assert.Equal(simplified, TagNormalizer.Normalize(traditional));
        Assert.Equal(simplified, TagNormalizer.Normalize(simplified));
    }

    [Theory]
    [InlineData("ＳＭ", "sm")]
    [InlineData(" OL ", "ol")]
    [InlineData("ラフティン　 マエシロ", "ラフティン マエシロ")]
    public void Normalize_FoldsWidthCaseAndWhitespace(string value, string expected)
    {
        Assert.Equal(expected, TagNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_LeavesKanaAlone()
    {
        Assert.Equal("皆月ひかる", TagNormalizer.Normalize("皆月ひかる"));
    }
}
