using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.TagSearch.Searches;

/// <summary>
/// Keeps each user's recent and saved searches in a JSON file under the server's data folder,
/// so the web client and the apps see the same lists.
/// </summary>
public sealed class SearchStore
{
    /// <summary>
    /// The number of recent searches kept per user.
    /// </summary>
    internal const int MaxRecent = 20;

    /// <summary>
    /// The number of saved searches kept per user.
    /// </summary>
    internal const int MaxSaved = 50;

    /// <summary>
    /// The longest search text accepted.
    /// </summary>
    internal const int MaxQueryLength = 200;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _folder;
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">The server's application paths.</param>
    public SearchStore(IApplicationPaths applicationPaths)
        : this(Path.Combine(applicationPaths?.DataPath ?? throw new ArgumentNullException(nameof(applicationPaths)), "tagsearch"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchStore"/> class.
    /// </summary>
    /// <param name="folder">The folder holding one file per user.</param>
    internal SearchStore(string folder)
    {
        _folder = folder;
    }

    /// <summary>
    /// Checks that search text is worth keeping.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <returns>Whether the text is not blank and not too long.</returns>
    internal static bool IsValidQuery(string? query)
        => !string.IsNullOrWhiteSpace(query) && query.Trim().Length <= MaxQueryLength;

    internal SearchLists Get(Guid userId)
    {
        lock (_lock)
        {
            return Read(userId);
        }
    }

    internal SearchLists AddRecent(Guid userId, string query)
        => Update(userId, lists => lists with { Recent = [query, .. Without(lists.Recent, query).Take(MaxRecent - 1)] });

    internal SearchLists RemoveRecent(Guid userId, string query)
        => Update(userId, lists => lists with { Recent = Without(lists.Recent, query) });

    internal SearchLists AddSaved(Guid userId, string query)
        => Update(userId, lists => lists.Saved.Contains(query) || lists.Saved.Count >= MaxSaved
            ? lists
            : lists with { Saved = [.. lists.Saved, query] });

    internal SearchLists RemoveSaved(Guid userId, string query)
        => Update(userId, lists => lists with { Saved = Without(lists.Saved, query) });

    private static List<string> Without(IEnumerable<string> queries, string query)
        => queries.Where(existing => !string.Equals(existing, query, StringComparison.Ordinal)).ToList();

    private SearchLists Update(Guid userId, Func<SearchLists, SearchLists> change)
    {
        lock (_lock)
        {
            var updated = change(Read(userId));
            Write(userId, updated);
            return updated;
        }
    }

    private SearchLists Read(Guid userId)
    {
        var path = PathFor(userId);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<SearchLists>(File.ReadAllText(path), JsonOptions) ?? SearchLists.Empty
            : SearchLists.Empty;
    }

    private void Write(Guid userId, SearchLists lists)
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor(userId);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(lists, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private string PathFor(Guid userId)
        => Path.Combine(_folder, userId.ToString("N", CultureInfo.InvariantCulture) + ".json");
}
