using System.Collections.Generic;

namespace Jellyfin.Plugin.TagSearch.Searches;

/// <summary>
/// One user's recent and saved searches.
/// </summary>
/// <param name="Recent">Recent searches, most recent first.</param>
/// <param name="Saved">Saved searches, in the order they were saved.</param>
internal sealed record SearchLists(IReadOnlyList<string> Recent, IReadOnlyList<string> Saved)
{
    /// <summary>
    /// Gets the lists of a user who has none yet.
    /// </summary>
    public static SearchLists Empty { get; } = new([], []);
}
