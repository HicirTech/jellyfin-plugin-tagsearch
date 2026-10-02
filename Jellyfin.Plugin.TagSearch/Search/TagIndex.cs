using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// An immutable map from each normalized tag to the items that carry it.
/// </summary>
internal sealed class TagIndex
{
    private readonly Dictionary<string, List<IndexedItem>> _byTag;

    private TagIndex(Dictionary<string, List<IndexedItem>> byTag)
    {
        _byTag = byTag;
    }

    /// <summary>
    /// Gets an index with no tags.
    /// </summary>
    public static TagIndex Empty { get; } = new(new Dictionary<string, List<IndexedItem>>(StringComparer.Ordinal));

    /// <summary>
    /// Gets the number of distinct tags.
    /// </summary>
    public int TagCount => _byTag.Count;

    /// <summary>
    /// Builds an index from items and the tags each carries.
    /// </summary>
    /// <param name="entries">Each item with its tags, in any script and spelling.</param>
    /// <returns>The index.</returns>
    public static TagIndex Build(IEnumerable<(IndexedItem Item, IEnumerable<string> Tags)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var byTag = new Dictionary<string, List<IndexedItem>>(StringComparer.Ordinal);
        foreach (var (item, tags) in entries)
        {
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                var key = TagNormalizer.Normalize(tag);
                if (!byTag.TryGetValue(key, out var items))
                {
                    items = [];
                    byTag[key] = items;
                }

                // An item's tags are added together, so a repeat (a genre that is also a studio) is always the last entry.
                if (items.Count == 0 || items[^1] != item)
                {
                    items.Add(item);
                }
            }
        }

        return new TagIndex(byTag);
    }

    /// <summary>
    /// Finds the items that carry every included tag and none of the excluded ones.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The matching items, or null when an included tag is unknown, so the query is not a tag search.</returns>
    public IReadOnlyCollection<IndexedItem>? Match(TagQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sets = new List<List<IndexedItem>>(query.Include.Count);
        foreach (var tag in query.Include)
        {
            if (!_byTag.TryGetValue(tag, out var items))
            {
                return null;
            }

            sets.Add(items);
        }

        // Start from the rarest tag so the intersection stays small.
        sets.Sort((left, right) => left.Count.CompareTo(right.Count));
        var result = new HashSet<IndexedItem>(sets[0]);
        for (var i = 1; i < sets.Count; i++)
        {
            result.IntersectWith(sets[i]);
        }

        foreach (var tag in query.Exclude)
        {
            if (_byTag.TryGetValue(tag, out var excluded))
            {
                result.ExceptWith(excluded);
            }
        }

        return result;
    }
}
