using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// Keeps the tag index in step with the library.
/// </summary>
/// <remarks>
/// A library change only marks the index stale; the next search rebuilds it. A scan fires an event
/// per item, and rebuilding once afterwards is cheaper than following every event.
/// </remarks>
public sealed class TagIndexService : IDisposable
{
    private const int PeopleBatchSize = 1000;

    private static readonly BaseItemKind[] IndexedKinds = [BaseItemKind.Movie, BaseItemKind.Video];

    private static readonly string[] PersonTypes = ["Actor", "Director"];

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TagIndexService> _logger;
    private readonly Lock _rebuildLock = new();
    private TagIndex _index = TagIndex.Empty;
    private volatile bool _stale = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagIndexService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public TagIndexService(ILibraryManager libraryManager, ILogger<TagIndexService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _libraryManager.ItemAdded += OnLibraryChanged;
        _libraryManager.ItemUpdated += OnLibraryChanged;
        _libraryManager.ItemRemoved += OnLibraryChanged;
    }

    /// <summary>
    /// Gets the current index, rebuilding it first if the library changed.
    /// </summary>
    internal TagIndex Current
    {
        get
        {
            if (!_stale)
            {
                return _index;
            }

            lock (_rebuildLock)
            {
                if (_stale)
                {
                    // Cleared before the build, so a change during the build marks the new index stale.
                    _stale = false;
                    try
                    {
                        _index = Build();
                    }
                    catch
                    {
                        _stale = true;
                        throw;
                    }
                }

                return _index;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnLibraryChanged;
        _libraryManager.ItemUpdated -= OnLibraryChanged;
        _libraryManager.ItemRemoved -= OnLibraryChanged;
    }

    private static IEnumerable<string> TagsOf(BaseItem item, Dictionary<Guid, IReadOnlyList<string>> people)
    {
        IEnumerable<string> tags = item.Genres.Concat(item.Studios).Concat(item.Tags);
        return people.TryGetValue(item.Id, out var names) ? tags.Concat(names) : tags;
    }

    private static int DayOf(BaseItem item)
        => (int)((item.PremiereDate ?? item.DateCreated) - DateTime.UnixEpoch).TotalDays;

    private void OnLibraryChanged(object? sender, ItemChangeEventArgs e)
    {
        _stale = true;
    }

    private TagIndex Build()
    {
        var started = Stopwatch.GetTimestamp();
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = IndexedKinds,
            Recursive = true,
            IsVirtualItem = false
        });

        var people = new Dictionary<Guid, IReadOnlyList<string>>();
        foreach (var batch in items.Select(item => item.Id).Chunk(PeopleBatchSize))
        {
            foreach (var (id, names) in _libraryManager.GetPeopleNamesByItems(batch, PersonTypes))
            {
                people[id] = names;
            }
        }

        var index = TagIndex.Build(items.Select(item => (new IndexedItem(item.Id, item is Movie, DayOf(item)), TagsOf(item, people))));
        _logger.LogInformation(
            "Indexed {TagCount} tags over {ItemCount} items in {ElapsedMs} ms",
            index.TagCount,
            items.Count,
            (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return index;
    }
}
