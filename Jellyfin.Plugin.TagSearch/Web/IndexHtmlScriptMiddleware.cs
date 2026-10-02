using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Plugin.TagSearch.Web;

/// <summary>
/// Adds the plugin's script tag to the web client's index.html as it is served.
/// </summary>
/// <remarks>
/// Only the one HTML file is touched; the web client's own bundles are served unchanged.
/// The official Android app loads this same file from the server, so the script reaches it too.
/// </remarks>
internal sealed class IndexHtmlScriptMiddleware
{
    private const string HeadEnd = "</head>";
    private const string ScriptPath = "TagSearch/Client.js";

    private readonly RequestDelegate _next;

    public IndexHtmlScriptMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsIndexRequest(context.Request))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // The body is rewritten below, so ask for it uncompressed and in full: neither a compressed
        // body nor a 304 against the unmodified file would carry the script tag.
        context.Request.Headers.Remove(HeaderNames.AcceptEncoding);
        context.Request.Headers.Remove(HeaderNames.IfNoneMatch);
        context.Request.Headers.Remove(HeaderNames.IfModifiedSince);

        var original = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = original;
        }

        var body = buffer.ToArray();
        if (context.Response.StatusCode == StatusCodes.Status200OK
            && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            var html = Encoding.UTF8.GetString(body);
            var headEnd = html.IndexOf(HeadEnd, StringComparison.OrdinalIgnoreCase);
            if (headEnd >= 0 && !html.Contains(ScriptPath, StringComparison.Ordinal))
            {
                // Relative to /web/, so it resolves under any configured base URL.
                var tag = "<script defer src=\"../" + ScriptPath + "?v=" + ClientScript.Version + "\"></script>";
                body = Encoding.UTF8.GetBytes(html.Insert(headEnd, tag));
                context.Response.Headers.Remove(HeaderNames.ETag);
                context.Response.Headers.Remove(HeaderNames.LastModified);
                context.Response.Headers.CacheControl = "no-cache";
            }
        }

        context.Response.ContentLength = body.Length;
        await original.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }

    private static bool IsIndexRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? string.Empty;
        return path.EndsWith("/web/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/web/index.html", StringComparison.OrdinalIgnoreCase);
    }
}
