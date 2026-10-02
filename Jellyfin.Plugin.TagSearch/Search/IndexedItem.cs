using System;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// A library item as the tag index sees it.
/// </summary>
/// <param name="Id">The item id.</param>
/// <param name="IsMovie">Whether the item is a movie rather than a plain video.</param>
/// <param name="Day">The release day, or the day the item was added, counted from 1970-01-01; used to sort newest first.</param>
internal readonly record struct IndexedItem(Guid Id, bool IsMovie, int Day);
