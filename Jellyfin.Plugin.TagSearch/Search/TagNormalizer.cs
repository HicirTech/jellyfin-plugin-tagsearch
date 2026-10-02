using System;
using System.Text;

namespace Jellyfin.Plugin.TagSearch.Search;

/// <summary>
/// Folds a tag or a search term to the form in which the two are compared.
/// </summary>
/// <remarks>
/// Width, case, runs of whitespace and Traditional against Simplified Chinese all compare equal,
/// so a term typed with a Simplified input method finds a tag stored in Traditional.
/// </remarks>
internal static class TagNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var folded = ChineseVariants.ToSimplified(value.Normalize(NormalizationForm.FormKC).ToLowerInvariant());
        var builder = new StringBuilder(folded.Length);
        var pendingSpace = false;
        foreach (var c in folded)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }
}
