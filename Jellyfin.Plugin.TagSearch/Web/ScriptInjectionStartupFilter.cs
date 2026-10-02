using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.TagSearch.Web;

/// <summary>
/// Puts <see cref="IndexHtmlScriptMiddleware"/> in front of the server's own pipeline.
/// </summary>
internal sealed class ScriptInjectionStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<IndexHtmlScriptMiddleware>();
            next(app);
        };
    }
}
