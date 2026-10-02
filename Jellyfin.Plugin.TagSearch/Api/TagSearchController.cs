using System;
using System.Security.Claims;
using Jellyfin.Plugin.TagSearch.Searches;
using Jellyfin.Plugin.TagSearch.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TagSearch.Api;

/// <summary>
/// The signed-in user's recent and saved searches, and the script that shows them on the search page.
/// </summary>
[ApiController]
[Route("TagSearch")]
public class TagSearchController : ControllerBase
{
    // The claim the server's authentication handler stores the user id in.
    private const string UserIdClaim = "Jellyfin-UserId";

    private readonly SearchStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagSearchController"/> class.
    /// </summary>
    /// <param name="store">The search store.</param>
    public TagSearchController(SearchStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets the current user's recent and saved searches.
    /// </summary>
    /// <returns>The searches.</returns>
    [HttpGet("Searches")]
    [Authorize]
    public ActionResult GetSearches()
        => CurrentUserId() is { } userId ? Ok(_store.Get(userId)) : Unauthorized();

    /// <summary>
    /// Records a search as the most recent one.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <returns>The updated searches.</returns>
    [HttpPost("Searches/Recent")]
    [Authorize]
    public ActionResult AddRecent([FromQuery] string query) => Change(query, _store.AddRecent);

    /// <summary>
    /// Removes a search from the recent ones.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <returns>The updated searches.</returns>
    [HttpDelete("Searches/Recent")]
    [Authorize]
    public ActionResult RemoveRecent([FromQuery] string query) => Change(query, _store.RemoveRecent);

    /// <summary>
    /// Saves a search.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <returns>The updated searches.</returns>
    [HttpPost("Searches/Saved")]
    [Authorize]
    public ActionResult AddSaved([FromQuery] string query) => Change(query, _store.AddSaved);

    /// <summary>
    /// Removes a saved search.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <returns>The updated searches.</returns>
    [HttpDelete("Searches/Saved")]
    [Authorize]
    public ActionResult RemoveSaved([FromQuery] string query) => Change(query, _store.RemoveSaved);

    /// <summary>
    /// Gets the script that shows these searches in place of the search page's suggestions.
    /// </summary>
    /// <returns>The script.</returns>
    [HttpGet("Client.js")]
    [AllowAnonymous]
    public ActionResult GetClientScript()
    {
        // The URL carries a content version, so the script never changes under a cached URL.
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(ClientScript.Bytes, "application/javascript; charset=utf-8");
    }

    private ActionResult Change(string query, Func<Guid, string, SearchLists> change)
    {
        if (CurrentUserId() is not { } userId)
        {
            return Unauthorized();
        }

        if (!SearchStore.IsValidQuery(query))
        {
            return BadRequest();
        }

        return Ok(change(userId, query.Trim()));
    }

    private Guid? CurrentUserId()
        => Guid.TryParse(User.FindFirstValue(UserIdClaim), out var userId) ? userId : null;
}
