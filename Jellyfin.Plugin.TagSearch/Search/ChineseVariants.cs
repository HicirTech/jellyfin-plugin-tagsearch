using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// Folds Traditional Chinese characters to Simplified, one character at a time.
/// </summary>
/// <remarks>
/// The table is OpenCC's TSCharacters.txt, embedded unmodified. Character-level folding is enough
/// for comparing tags; it is not a text converter and ignores OpenCC's phrase tables.
/// </remarks>
internal static class ChineseVariants
{
    private const string ResourceName = "Jellyfin.Plugin.TagSearch.Search.TSCharacters.txt";

    private static readonly Lazy<FrozenDictionary<int, string>> Map = new(Load);

    public static string ToSimplified(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var map = Map.Value;
        StringBuilder? builder = null;
        var index = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (map.TryGetValue(rune.Value, out var simplified))
            {
                builder ??= new StringBuilder(value, 0, index, value.Length);
                builder.Append(simplified);
            }
            else
            {
                builder?.Append(rune.ToString());
            }

            index += rune.Utf16SequenceLength;
        }

        return builder?.ToString() ?? value;
    }

    private static FrozenDictionary<int, string> Load()
    {
        using var stream = typeof(ChineseVariants).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Missing embedded resource " + ResourceName);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var map = new Dictionary<int, string>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var tab = line.IndexOf('\t', StringComparison.Ordinal);
            if (tab <= 0)
            {
                continue;
            }

            var key = line[..tab];
            var candidates = line[(tab + 1)..];
            var space = candidates.IndexOf(' ', StringComparison.Ordinal);
            var first = space < 0 ? candidates : candidates[..space];

            // The first candidate is OpenCC's default; a key that is its own default needs no entry.
            if (Rune.TryGetRuneAt(key, 0, out var rune)
                && rune.Utf16SequenceLength == key.Length
                && first.Length > 0
                && !string.Equals(first, key, StringComparison.Ordinal))
            {
                map[rune.Value] = first;
            }
        }

        return map.ToFrozenDictionary();
    }
}
