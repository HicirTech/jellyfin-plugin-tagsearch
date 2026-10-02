# jellyfin-plugin-tagsearch

A Jellyfin plugin for searching the whole library by tag: genres, tags, studios, actors and directors,
where an item must carry every tag you list.

## What it does

Type tags into the search box of any Jellyfin client, separated by commas:

```
辣妹, 巨乳
```

returns the movies and videos that carry both tags, newest first. Jellyfin on its own matches titles only,
and its genre filter returns items carrying any of the selected genres rather than all of them.

- A full-width comma `，` and an ideographic comma `、` separate terms as well. A space does not, because
  names often contain one.
- A leading minus excludes a tag: `辣妹, 巨乳, -中出`.
- Terms match regardless of width, case and Traditional or Simplified Chinese, so `滥交` finds `濫交`.
- A single tag is a search too: `巨乳` returns every item carrying it, followed by the title matches
  Jellyfin would have found anyway.
- Anything that is not a list of known tags, such as a title or a half-typed term, is left to Jellyfin's
  own search.

In the web client, and in the official Android app, which loads the same web client, the search page shows
your saved and recent searches in place of its random suggestions:

- A search joins the recent list when you open one of its results.
- The star saves a search, so it stays when the recent list moves on.
- Both lists are kept on the server per user, so every device shows the same ones.

## What it does not do

- It does not add a page or a menu of its own; it uses the search box every client already has.
- Other clients, such as Findroid or Android TV, get the tag search in their search box but not the lists
  of searches, which are part of the web client.
- It does not read titles for tags. A tag has to be in the item's metadata, typically from its `.nfo`.

## Requirements

- Jellyfin 12.0 or later. The plugin targets .NET 10 and will not load on 10.x.
- The .NET 10 SDK to build it.

## Building

```sh
dotnet build -c Release
dotnet test
```

The plugin is `Jellyfin.Plugin.TagSearch/bin/Release/net10.0/Jellyfin.Plugin.TagSearch.dll`. It is the
only file the server needs; the Jellyfin assemblies it references are the server's own.

## Installing by hand

1. Stop the server.
2. Create `plugins/TagSearch_0.1.0.0/` in the server's data directory (`/config/plugins/` in the official
   container image) and copy the DLL into it. The server derives the version from the folder name when there
   is no `meta.json`.
3. Start the server. Dashboard > Plugins lists TagSearch as Active, and its page reports the loaded version.
4. Reload the web client once, so it fetches the page that carries the plugin's script.

To replace a build, stop the server, overwrite the DLL and start it again. Keep the folder name's version
equal to the version in `Directory.Build.props`.

## License

GPL-3.0, the license of the Jellyfin packages the plugin links against. See [LICENSE](LICENSE).
The bundled OpenCC character table is Apache-2.0; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
