using HLStatsX.NET.Core.Entities;
using HLStatsX.NET.Core.Interfaces.Services;
using HLStatsX.NET.Core.Models;
using HLStatsX.NET.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HLStatsX.NET.Tests.Controllers;

public class SearchControllerTests
{
    private readonly Mock<ISearchService> _searchMock;
    private readonly IConfiguration _config;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _searchMock = new Mock<ISearchService>();
        _searchMock.Setup(s => s.GetVisibleGamesAsync(default)).ReturnsAsync([]);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HLStatsX:DefaultGame"]       = "cstrike",
                ["HLStatsX:HideBotPlayers"]    = "true"
            })
            .Build();

        _controller = new SearchController(_searchMock.Object, _config);
        // ViewData requires a TempData provider in controller tests
        _controller.ViewData ??= new ViewDataDictionary(
            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenQueryIsEmpty()
    {
        var result = await _controller.Index(null, null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeNull();
    }

    [Fact]
    public async Task Index_ReturnsViewWithNullModel_WhenQueryIsWhitespace()
    {
        var result = await _controller.Index("   ", null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeNull();
    }

    [Fact]
    public async Task Index_ReturnsSearchResults_WhenQueryProvided()
    {
        var results = new SearchResults
        {
            Query        = "FragLord",
            Players      = [new(1, "FragLord", null, null, "cstrike")],
            TotalPlayers = 1,
            Clans        = [],
            TotalClans   = 0,
        };
        _searchMock.Setup(s => s.SearchAsync("FragLord", "cstrike", null, 1, 50, default))
                   .ReturnsAsync(results);

        var result = await _controller.Index("FragLord", "cstrike");

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeOfType<SearchResults>()
            .Which.TotalPlayers.Should().Be(1);
    }

    [Fact]
    public async Task Index_PassesNullGame_WhenGameIsEmptyString()
    {
        var results = new SearchResults { Query = "ace", Players = [], Clans = [] };
        _searchMock.Setup(s => s.SearchAsync("ace", null, null, 1, 50, default))
                   .ReturnsAsync(results);

        var result = await _controller.Index("ace", "");

        result.Should().BeOfType<ViewResult>();
        _searchMock.Verify(s => s.SearchAsync("ace", null, null, 1, 50, default), Times.Once);
    }

    [Fact]
    public async Task Index_SetsViewDataValues()
    {
        var result = await _controller.Index(null, "dods");

        _controller.ViewData["game"].Should().Be("dods");
        _controller.ViewData["query"].Should().Be("");
    }
}
