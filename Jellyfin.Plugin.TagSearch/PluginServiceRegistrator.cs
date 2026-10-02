using Jellyfin.Plugin.TagSearch.Search;
using Jellyfin.Plugin.TagSearch.Searches;
using Jellyfin.Plugin.TagSearch.Web;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TagSearch;

/// <summary>
/// Registers the plugin's services with the server.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<TagIndexService>();
        serviceCollection.AddSingleton<SearchStore>();
        serviceCollection.AddTransient<IStartupFilter, ScriptInjectionStartupFilter>();
    }
}
