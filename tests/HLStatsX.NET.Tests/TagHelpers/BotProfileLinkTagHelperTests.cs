using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Web.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace HLStatsX.NET.Tests.TagHelpers;

public class BotProfileLinkTagHelperTests
{
    private readonly Mock<IPlayerService> _players = new();

    public BotProfileLinkTagHelperTests() =>
        _players.Setup(p => p.GetBotPlayerIdsAsync(default))
                .ReturnsAsync(new HashSet<int> { 7 });

    private BotProfileLinkTagHelper CreateHelper(bool hideBots = true, string ambientController = "Players")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HLStatsX:HideBotPlayers"] = hideBots.ToString() })
            .Build();

        var routeData = new RouteData();
        routeData.Values["controller"] = ambientController;
        var viewContext = new ViewContext
        {
            RouteData = routeData
        };

        return new BotProfileLinkTagHelper(_players.Object, new MemoryCache(new MemoryCacheOptions()), config)
        {
            ViewContext = viewContext
        };
    }

    private static (TagHelperContext ctx, TagHelperOutput output) Build(
        string action, object? id, string? controller = null)
    {
        var attrs = new TagHelperAttributeList { { "asp-action", action } };
        if (controller is not null) attrs.Add("asp-controller", controller);
        if (id is not null) attrs.Add("asp-route-id", id);

        var ctx = new TagHelperContext("a", attrs, new Dictionary<object, object>(), Guid.NewGuid().ToString());
        var output = new TagHelperOutput("a", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        return (ctx, output);
    }

    [Fact]
    public async Task ProcessAsync_RemovesLink_WhenTargetIsBot()
    {
        var (ctx, output) = Build("Profile", 7, "Players");

        await CreateHelper().ProcessAsync(ctx, output);

        output.TagName.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_RemovesLink_WhenControllerIsAmbientPlayers()
    {
        var (ctx, output) = Build("Profile", 7);

        await CreateHelper(ambientController: "Players").ProcessAsync(ctx, output);

        output.TagName.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_KeepsLink_WhenTargetIsNotBot()
    {
        var (ctx, output) = Build("Profile", 8, "Players");

        await CreateHelper().ProcessAsync(ctx, output);

        output.TagName.Should().Be("a");
    }

    [Fact]
    public async Task ProcessAsync_KeepsLink_WhenHideBotPlayersDisabled()
    {
        var (ctx, output) = Build("Profile", 7, "Players");

        await CreateHelper(hideBots: false).ProcessAsync(ctx, output);

        output.TagName.Should().Be("a");
        _players.Verify(p => p.GetBotPlayerIdsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_KeepsLink_ForOtherControllersProfileAction()
    {
        var (ctx, output) = Build("Profile", 7, "Clans");

        await CreateHelper().ProcessAsync(ctx, output);

        output.TagName.Should().Be("a");
    }

    [Fact]
    public async Task ProcessAsync_KeepsLink_ForOtherPlayerActions()
    {
        var (ctx, output) = Build("History", 7, "Players");

        await CreateHelper().ProcessAsync(ctx, output);

        output.TagName.Should().Be("a");
    }

    [Fact]
    public async Task ProcessAsync_CachesBotIdsBetweenCalls()
    {
        var helper = CreateHelper();

        for (var i = 0; i < 3; i++)
        {
            var (ctx, output) = Build("Profile", 7, "Players");
            await helper.ProcessAsync(ctx, output);
        }

        _players.Verify(p => p.GetBotPlayerIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
