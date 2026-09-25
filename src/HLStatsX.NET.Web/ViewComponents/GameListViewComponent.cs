using HLStatsX.NET.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HLStatsX.NET.Web.ViewComponents;

public class GameListViewComponent : ViewComponent
{
    private readonly IGameRepository _games;

    public GameListViewComponent(IGameRepository games) => _games = games;

    public async Task<IViewComponentResult> InvokeAsync(string? currentGame = null)
    {
        var games = await _games.GetAllAsync();
        ViewData["CurrentGame"] = currentGame;
        return View(games);
    }
}
