using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace HLStatsX.NET.Web.Services;

public class SteamService : ISteamService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public SteamService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<string?> GetAvatarUrlAsync(long steam64, CancellationToken ct = default)
    {
        var cacheKey = $"steam_avatar:{steam64}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        try
        {
            var http = _httpClientFactory.CreateClient("Steam");
            var xml = await http.GetStringAsync(
                $"https://steamcommunity.com/profiles/{steam64}?xml=1", ct);
            var doc = XDocument.Parse(xml);
            var avatarUrl = doc.Root?.Element("avatarFull")?.Value;
            if (!string.IsNullOrEmpty(avatarUrl))
                _cache.Set(cacheKey, avatarUrl, TimeSpan.FromHours(24));
            return avatarUrl;
        }
        catch
        {
            return null;
        }
    }
}
