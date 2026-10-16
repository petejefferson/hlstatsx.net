using HLStatsX.NET.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Web.TagHelpers;

/// <summary>
/// Removes the link from any <c>&lt;a asp-controller="Players" asp-action="Profile" asp-route-id="…"&gt;</c>
/// that points at a bot, leaving its text in place. Bot profiles are expensive to build, so they
/// are not linked when <c>HLStatsX:HideBotPlayers</c> is enabled.
/// </summary>
[HtmlTargetElement("a", Attributes = "asp-action")]
public class BotProfileLinkTagHelper : TagHelper
{
    private const string CacheKey = "BotPlayerIds";

    private readonly IPlayerService _players;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public BotProfileLinkTagHelper(IPlayerService players, IMemoryCache cache, IConfiguration config)
    {
        _players = players;
        _cache   = cache;
        _config  = config;
    }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    // Runs before the framework AnchorTagHelper so the link is dropped before an href is generated.
    public override int Order => -1000;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!_config.GetValue<bool>("HLStatsX:HideBotPlayers", true)) return;
        if (!IsPlayerProfileLink(context)) return;
        if (!int.TryParse(context.AllAttributes["asp-route-id"]?.Value?.ToString(), out var playerId)) return;

        var botIds = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _players.GetBotPlayerIdsAsync();
        });

        if (botIds!.Contains(playerId))
            output.TagName = null;
    }

    private bool IsPlayerProfileLink(TagHelperContext context)
    {
        if (!string.Equals(context.AllAttributes["asp-action"]?.Value?.ToString(), "Profile", StringComparison.OrdinalIgnoreCase))
            return false;

        var controller = context.AllAttributes["asp-controller"]?.Value?.ToString()
                         ?? ViewContext.RouteData.Values["controller"]?.ToString();
        return string.Equals(controller, "Players", StringComparison.OrdinalIgnoreCase);
    }
}
