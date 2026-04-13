using System.Xml.Linq;

namespace HLStatsX.NET.Web.Services;

public class SteamService : ISteamService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SteamService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetAvatarUrlAsync(string? uniqueId, CancellationToken ct = default)
    {
        var steam64 = ISteamService.ToSteam64(uniqueId);
        if (steam64 is null) return null;

        try
        {
            var http = _httpClientFactory.CreateClient("Steam");
            var xml = await http.GetStringAsync(
                $"https://steamcommunity.com/profiles/{steam64}?xml=1", ct);
            var doc = XDocument.Parse(xml);
            return doc.Root?.Element("avatarFull")?.Value;
        }
        catch
        {
            return null;
        }
    }
}
