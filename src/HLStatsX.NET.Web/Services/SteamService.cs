using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Web.Services;

public class SteamService : ISteamService
{
    private static readonly TimeSpan HitTtl   = TimeSpan.FromHours(24);
    private static readonly TimeSpan MissTtl  = TimeSpan.FromHours(24);
    private static readonly TimeSpan ErrorTtl = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SteamService> _logger;
    private readonly string? _apiKey;

    public SteamService(IHttpClientFactory httpClientFactory, IMemoryCache cache,
        ILogger<SteamService> logger, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
        _apiKey = config["HLStatsX:Steam:ApiKey"] is { Length: > 0 } key ? key : null;
    }

    public async Task<string?> GetAvatarUrlAsync(long steam64, CancellationToken ct = default)
    {
        var cacheKey = $"steam_avatar:{steam64}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return string.IsNullOrEmpty(cached) ? null : cached;

        try
        {
            var http = _httpClientFactory.CreateClient("Steam");
            var avatarUrl = _apiKey is null
                ? await FetchFromCommunityAsync(http, steam64, ct)
                : await FetchFromWebApiAsync(http, steam64, _apiKey, ct);

            _cache.Set(cacheKey, avatarUrl ?? "", avatarUrl is null ? MissTtl : HitTtl);
            return avatarUrl;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Steam avatar request for {Steam64} failed with HTTP status {StatusCode}; retrying in {Minutes} min",
                steam64, ex.StatusCode, ErrorTtl.TotalMinutes);
            _cache.Set(cacheKey, "", ErrorTtl);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Steam avatar lookup for {Steam64} failed; retrying in {Minutes} min",
                steam64, ErrorTtl.TotalMinutes);
            _cache.Set(cacheKey, "", ErrorTtl);
            return null;
        }
    }

    private async Task<string?> FetchFromWebApiAsync(HttpClient http, long steam64, string apiKey, CancellationToken ct)
    {
        var json = await http.GetStringAsync(
            $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={Uri.EscapeDataString(apiKey)}&steamids={steam64}", ct);
        using var doc = JsonDocument.Parse(json);
        var players = doc.RootElement.GetProperty("response").GetProperty("players");
        var avatarUrl = players.GetArrayLength() > 0 && players[0].TryGetProperty("avatarfull", out var avatar)
            ? avatar.GetString()
            : null;
        if (string.IsNullOrEmpty(avatarUrl))
        {
            _logger.LogWarning("Steam Web API returned no profile or avatar for {Steam64}", steam64);
            return null;
        }
        return avatarUrl;
    }

    private async Task<string?> FetchFromCommunityAsync(HttpClient http, long steam64, CancellationToken ct)
    {
        var xml = await http.GetStringAsync($"https://steamcommunity.com/profiles/{steam64}?xml=1", ct);
        var doc = XDocument.Parse(xml);
        var avatarUrl = doc.Root?.Element("avatarFull")?.Value;
        if (string.IsNullOrEmpty(avatarUrl))
        {
            _logger.LogWarning("Steam returned no avatar for {Steam64}: {Reason}",
                steam64, doc.Root?.Element("error")?.Value ?? "no avatarFull element");
            return null;
        }
        return avatarUrl;
    }
}
