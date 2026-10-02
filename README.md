# jellyfin-plugin-tagsearch

A Jellyfin plugin for searching the whole library by tag: genres, tags, studios, actors and directors.

## Status

Scaffold only. The plugin loads on Jellyfin 12.x and shows the build the server loaded on its dashboard
page. It does not search anything yet.

## Requirements

- Jellyfin 12.0 or later. The plugin targets .NET 10 and will not load on 10.x.
- The .NET 10 SDK to build it.

## Building

```sh
dotnet build -c Release
```

The plugin is `Jellyfin.Plugin.TagSearch/bin/Release/net10.0/Jellyfin.Plugin.TagSearch.dll`. It is the
only file the server needs; the Jellyfin assemblies it references are the server's own.

## Installing by hand

1. Stop the server.
2. Create `plugins/TagSearch_0.1.0.0/` in the server's data directory (`/config/plugins/` in the official
   container image) and copy the DLL into it. The server derives the version from the folder name when there
   is no `meta.json`.
3. Start the server. Dashboard > Plugins lists TagSearch as Active, and its page reports the loaded version.

To replace a build, stop the server, overwrite the DLL and start it again. Keep the folder name's version
equal to the version in `Directory.Build.props`.

## License

GPL-3.0, the license of the Jellyfin packages the plugin links against. See [LICENSE](LICENSE).
