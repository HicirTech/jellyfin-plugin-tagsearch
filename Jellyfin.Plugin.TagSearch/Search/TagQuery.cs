using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// A search for items carrying every listed tag, written as <c>辣妹, 巨乳, -中出</c>.
/// </summary>
/// <remarks>
/// Terms are separated by a comma, a full-width comma or an ideographic comma, never by a space,
/// because names such as directors' often contain one. A leading minus excludes a tag.
/// </remarks>
internal sealed class TagQuery
{
    private static readonly char[] Separators = [',', '，', '、'];

    private TagQuery(IReadOnlyList<string> include, IReadOnlyList<string> exclude)
    {
        Include = include;
        Exclude = exclude;
    }

    /// <summary>
    /// Gets the normalized tags an item must all carry.
    /// </summary>
    public IReadOnlyList<string> Include { get; }

    /// <summary>
    /// Gets the normalized tags an item must not carry.
    /// </summary>
    public IReadOnlyList<string> Exclude { get; }

    /// <summary>
    /// Parses search text, or returns null when it names no tag to include.
    /// </summary>
    /// <param name="text">The text typed into the search box.</param>
    /// <returns>The query, or null.</returns>
    public static TagQuery? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var include = new List<string>();
        var exclude = new List<string>();
        foreach (var part in text.Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var negated = part[0] is '-' or '－';
            var term = TagNormalizer.Normalize(negated ? part[1..] : part);
            if (term.Length == 0)
            {
                continue;
            }

            var target = negated ? exclude : include;
            if (!target.Contains(term))
            {
                target.Add(term);
            }
        }

        return include.Count == 0 ? null : new TagQuery(include, exclude);
    }
}
