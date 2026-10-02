using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TagSearch.Search;
using Xunit;

namespace Jellyfin.Plugin.TagSearch.Tests;

public class TagIndexTests
{
    private static readonly IndexedItem Both = new(Guid.NewGuid(), true, 20000);
    private static readonly IndexedItem GalOnly = new(Guid.NewGuid(), true, 20001);
    private static readonly IndexedItem BustyOnly = new(Guid.NewGuid(), false, 20002);

    private static readonly TagIndex Index = TagIndex.Build(
    [
        (Both, ["辣妹", "巨乳", "中出"]),
        (GalOnly, ["辣妹", "單體作品"]),
        (BustyOnly, ["巨乳", "ビビアン"])
    ]);

    [Fact]
    public void Match_RequiresEveryIncludedTag()
    {
        Assert.Equal([Both], Match("辣妹, 巨乳"));
    }

    [Fact]
    public void Match_ReturnsEveryCarrierOfASingleTag()
    {
        Assert.Equal(new[] { Both, BustyOnly }.OrderBy(i => i.Day), Match("巨乳").OrderBy(i => i.Day));
    }

    [Fact]
    public void Match_RemovesExcludedTags()
    {
        Assert.Equal([BustyOnly], Match("巨乳, -中出"));
    }

    [Fact]
    public void Match_FindsTraditionalTagsFromSimplifiedInput()
    {
        Assert.Equal([GalOnly], Match("单体作品"));
    }

    [Fact]
    public void Match_ReturnsNullWhenATagIsUnknown()
    {
        Assert.Null(Index.Match(TagQuery.Parse("辣妹, 不存在")!));
    }

    [Fact]
    public void Build_CountsATagCarriedTwiceByOneItemOnce()
    {
        var item = new IndexedItem(Guid.NewGuid(), true, 1);
        var index = TagIndex.Build([(item, ["DOC", "doc"])]);

        Assert.Equal(1, index.TagCount);
        Assert.Equal([item], index.Match(TagQuery.Parse("Doc")!));
    }

    private static IReadOnlyCollection<IndexedItem> Match(string text)
    {
        var result = Index.Match(TagQuery.Parse(text)!);
        Assert.NotNull(result);
        return result;
    }
}
