using System;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.TagSearch.Searches;
using Xunit;

namespace Jellyfin.Plugin.TagSearch.Tests;

public sealed class SearchStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "tagsearch-tests-" + Guid.NewGuid().ToString("N"));
    private readonly Guid _user = Guid.NewGuid();

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void AddRecent_MovesARepeatToTheFrontAndKeepsTheLimit()
    {
        var store = new SearchStore(_folder);
        for (var i = 0; i < SearchStore.MaxRecent + 5; i++)
        {
            store.AddRecent(_user, "q" + i.ToString(CultureInfo.InvariantCulture));
        }

        var lists = store.AddRecent(_user, "q10");

        Assert.Equal(SearchStore.MaxRecent, lists.Recent.Count);
        Assert.Equal("q10", lists.Recent[0]);
        Assert.Single(lists.Recent, q => q == "q10");
    }

    [Fact]
    public void AddSaved_IgnoresARepeatAndRemoveSavedDropsIt()
    {
        var store = new SearchStore(_folder);
        store.AddSaved(_user, "辣妹, 巨乳");
        store.AddSaved(_user, "中出");

        Assert.Equal(["辣妹, 巨乳", "中出"], store.AddSaved(_user, "辣妹, 巨乳").Saved);
        Assert.Equal(["中出"], store.RemoveSaved(_user, "辣妹, 巨乳").Saved);
    }

    [Fact]
    public void Lists_SurviveANewStoreAndStaySeparatePerUser()
    {
        new SearchStore(_folder).AddSaved(_user, "辣妹, 巨乳");
        var other = Guid.NewGuid();

        var reopened = new SearchStore(_folder);

        Assert.Equal(["辣妹, 巨乳"], reopened.Get(_user).Saved);
        Assert.Empty(reopened.Get(other).Saved);
        Assert.Empty(reopened.Get(other).Recent);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("辣妹, 巨乳", true)]
    public void IsValidQuery_RejectsBlankText(string? query, bool expected)
    {
        Assert.Equal(expected, SearchStore.IsValidQuery(query));
    }

    [Fact]
    public void IsValidQuery_RejectsTextOverTheLimit()
    {
        Assert.True(SearchStore.IsValidQuery(new string('a', SearchStore.MaxQueryLength)));
        Assert.False(SearchStore.IsValidQuery(new string('a', SearchStore.MaxQueryLength + 1)));
    }
}
