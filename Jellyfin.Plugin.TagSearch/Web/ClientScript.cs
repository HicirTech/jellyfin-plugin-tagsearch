using System;
using System.IO;
using System.Security.Cryptography;

namespace Jellyfin.Plugin.TagSearch.Web;

/// <summary>
/// The script added to the web client, and a version derived from its content.
/// </summary>
/// <remarks>
/// The version goes into the script's URL, so a changed script is fetched again while an unchanged
/// one can be cached for as long as the browser likes.
/// </remarks>
internal static class ClientScript
{
    private const string ResourceName = "Jellyfin.Plugin.TagSearch.Web.client.js";

    private static readonly Lazy<byte[]> Content = new(Load);

    private static readonly Lazy<string> ContentVersion = new(() => Convert.ToHexStringLower(SHA256.HashData(Content.Value))[..12]);

    public static byte[] Bytes => Content.Value;

    public static string Version => ContentVersion.Value;

    private static byte[] Load()
    {
        using var stream = typeof(ClientScript).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Missing embedded resource " + ResourceName);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
