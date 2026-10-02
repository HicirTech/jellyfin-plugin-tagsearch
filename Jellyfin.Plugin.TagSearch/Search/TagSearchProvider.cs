using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// Answers tag searches typed into any client's search box, such as <c>辣妹, 巨乳</c>:
/// an item must carry every listed tag.
/// </summary>
/// <remarks>
/// The server drops its own title matches whenever an external provider returns results, so this
/// provider claims a query only when every included term is a known tag, and for a single term it
/// also returns the title matches the built-in search would have found.
/// </remarks>
public sealed class TagSearchProvider : IExternalSearchProvider
{
    // Tag matches rank above title matches; within each group newer releases come first.
    private const float TagMatchScore = 1_000_000f;
    private const float TitleMatchScore = 500_000f;

    private static readonly BaseItemKind[] IndexedKinds = [BaseItemKind.Movie, BaseItemKind.Video];

    private readonly TagIndexService _index;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagSearchProvider"/> class.
    /// </summary>
    /// <param name="index">The tag index.</param>
    /// <param name="libraryManager">The library manager.</param>
    public TagSearchProvider(TagIndexService index, ILibraryManager libraryManager)
    {
        _index = index;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public string Name => "TagSearch";

    /// <inheritdoc />
    public MetadataPluginType Type => MetadataPluginType.SearchProvider;

    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public bool CanSearch(SearchProviderQuery query) => Plan(query) is not null;

    /// <inheritdoc />
    public IAsyncEnumerable<SearchResult> SearchAsync(SearchProviderQuery query, CancellationToken cancellationToken)
        => Search(query).ToAsyncEnumerable();

    /// <inheritdoc />
    Task<IReadOnlyList<SearchResult>> ISearchProvider.SearchAsync(SearchProviderQuery query, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SearchResult>>(Search(query));

    private static bool Allows(SearchProviderQuery query, BaseItemKind kind)
        => query.IncludeItemTypes.Length > 0 ? query.IncludeItemTypes.Contains(kind) : !query.ExcludeItemTypes.Contains(kind);

    private (TagQuery Query, bool Movies, bool Videos, IReadOnlyCollection<IndexedItem> Matches)? Plan(SearchProviderQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var movies = Allows(query, BaseItemKind.Movie);
        var videos = Allows(query, BaseItemKind.Video);
        if (!(movies || videos) || (query.MediaTypes.Length > 0 && !query.MediaTypes.Contains(MediaType.Video)))
        {
            return null;
        }

        var tagQuery = TagQuery.Parse(query.SearchTerm);
        if (tagQuery is null || _index.Current.Match(tagQuery) is not { } matches)
        {
            return null;
        }

        return (tagQuery, movies, videos, matches);
    }

    private List<SearchResult> Search(SearchProviderQuery query)
    {
        if (Plan(query) is not { } plan)
        {
            return [];
        }

        var limit = query.Limit ?? int.MaxValue;
        var scope = query.ParentId is { } parentId
            ? _libraryManager.GetItemIds(new InternalItemsQuery
            {
                AncestorIds = [parentId],
                IncludeItemTypes = IndexedKinds,
                Recursive = true
            }).ToHashSet()
            : null;

        var results = plan.Matches
            .Where(item => (item.IsMovie ? plan.Movies : plan.Videos) && (scope is null || scope.Contains(item.Id)))
            .OrderByDescending(item => item.Day)
            .Take(limit)
            .Select(item => new SearchResult(item.Id, TagMatchScore + item.Day))
            .ToList();

        // A single tag is also a valid title search; keep what the built-in search finds for it.
        if (plan.Query.Include.Count == 1 && plan.Query.Exclude.Count == 0 && results.Count < limit)
        {
            var seen = results.Select(result => result.ItemId).ToHashSet();
            var titleMatches = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                SearchTerm = query.SearchTerm.Trim(),
                IncludeItemTypes = query.IncludeItemTypes,
                ExcludeItemTypes = query.ExcludeItemTypes,
                MediaTypes = query.MediaTypes,
                AncestorIds = query.ParentId is { } ancestorId ? [ancestorId] : [],
                Recursive = true,
                IsVirtualItem = false,
                Limit = limit - results.Count
            });

            var rank = 0;
            foreach (var id in titleMatches)
            {
                if (seen.Add(id))
                {
                    results.Add(new SearchResult(id, TitleMatchScore - rank++));
                }
            }
        }

        return results;
    }
}
