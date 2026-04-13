# HLStatsX.NET — Claude Project Guide

## What This Project Is

A .NET 10 open-source rewrite of HLStatsX Community Edition — a PHP-based real-time player and clan statistics system for Half-Life engine games (Counter-Strike, Day of Defeat: Source, TF2, etc.). The goal is **complete feature parity with the PHP version**.

The original PHP source is preserved in `OriginalPHP/` and is the authoritative spec. **Before implementing any feature, read the relevant PHP file first** to understand the intended behaviour, SQL queries, and edge cases.

The project will be released on GitHub for public use and contributions — code quality, patterns, and consistency matter.

## Solution Structure

```
src/
  HLStatsX.NET.Core/           # Domain entities, interfaces, models (no dependencies)
  HLStatsX.NET.Infrastructure/ # EF Core + MySQL implementations of Core interfaces
  HLStatsX.NET.Web/            # ASP.NET Core MVC app
tests/
  HLStatsX.NET.Tests/          # xUnit unit tests (controllers, services)
OriginalPHP/                   # Reference PHP source — the spec for all features
.claude/commands/              # Project slash commands (see below)
```

## Architecture

Strict layering: **Core → Infrastructure → Web**

- **Core** — entities, repository interfaces, service interfaces, shared models. No EF or ASP.NET Core references.
- **Infrastructure** — EF Core repository implementations. All database queries live here.
- **Web** — MVC controllers, Razor views, view models. Controllers call services only — never repositories directly.
- **Services** — thin orchestration wrappers over repositories. Only add logic that belongs between HTTP and database.

When adding a feature, changes flow bottom-up: entity/interface → repository → service → view model → controller → view.

## Database

- MySQL via `Pomelo.EntityFrameworkCore.MySql`
- Connection string key: `HLStats` in `appsettings.json` / `appsettings.Development.json`
- `HLStatsDbContext` in `Infrastructure/Data/` — add new `DbSet<T>` here when adding entities
- **EF Core is pinned to 9.x** — Pomelo hasn't released a 10.x version yet. Do not upgrade EF Core past `9.0.*` until Pomelo 10 is available. All other packages target .NET 10.

## Configuration

```json
{
  "ConnectionStrings": {
    "HLStats": "Server=...;Database=hlstatsx;User=...;Password=...;CharSet=utf8mb4;"
  },
  "HLStatsX": {
    "DefaultGame": "dods",
    "DefaultPageSize": 50,
    "SiteName": "HLStatsX.NET"
  }
}
```

`appsettings.Development.json` overrides the connection string locally and **must never be committed** — it contains the dev DB password.

## Running & Building

```bash
dotnet build
dotnet run --project src/HLStatsX.NET.Web
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```

**Visual Studio build lock:** Pete develops with Visual Studio running the app in debug (F5). While VS has the app running, it holds a lock on output DLLs. `dotnet build` will fail with MSB3027/MSB3021 copy errors. This is not a code error — stop the VS debug session first. The Core and Infrastructure projects will compile successfully regardless; only the copy step fails.

Repository tests require a live MySQL connection and will fail in CI or without a local DB — always exclude them with the filter above.

## Deployment Target

- **Docker container** (planned) — no IIS-specific dependencies
- **HTTPS** handled by Nginx as a reverse proxy in front of the container
- Currently developed and tested via Visual Studio locally

## Multi-Game Support

The app must work with all Half-Life engine games. Current dev/test data is Day of Defeat: Source (`dods`). Always implement features game-agnostically — filter by the `game` parameter everywhere, never hardcode a game code.

## Coding Standards

- All service/repository methods must be `async Task<T>` with `CancellationToken ct = default`
- No `.Result` or `.Wait()` — use `await` throughout
- No speculative abstractions or helpers for single uses
- No docstrings or comments on unchanged code
- Keep it correct and clean first; optimise later

## Key Patterns

### Pagination

Use `PagedResult<T>` for all paged queries. In views use the `_Pagination` partial:

```cshtml
<partial name="_Pagination" model="@PaginationModel.From(Model.Result, p => Url.Action("Index", new { page = p })!)" />
```

### Sortable Column Headers

Every list page uses local Razor functions:

```cshtml
@{
    string SortUrl(string field)
    {
        bool nextDesc = Model.SortBy == field ? !Model.Descending : true;
        return Url.Action("Index", new { sortBy = field, desc = nextDesc, game = Model.Game })!;
    }
    string Mark(string field) => Model.SortBy == field ? (Model.Descending ? " ▼" : " ▲") : "";
}
```

Use `@(SortUrl("field"))` and `@(Mark("field"))` — explicit `@(expr)` is required inside HTML attributes.

### Razor Gotchas

- Always use `@(expr)` explicit syntax in HTML attributes and tag helper attributes — implicit `@expr` breaks when the expression contains string literals with double quotes
- Use literal Unicode characters (`▼` `▲`) in C# strings, not HTML entities + `Html.Raw()`
- `@{...}` code blocks inside `@if {}` must appear **before** any HTML output in that scope — pre-compute variables at the top of the block, not after a closing tag

### EF Core GroupBy Gotchas

- **Never use `let` after `group...into` in query syntax.** A `let` clause following a `group...into` forces EF Core to carry the raw group object into the Select, producing an untranslatable expression (`g = g` in the LINQ tree). Error: *"Translation of 'Select' which contains grouping parameter without composition is not supported."*

  **Wrong:**
  ```csharp
  group f by f.Weapon into g
  orderby g.Count() descending
  let code = g.Key          // ← captures raw g, EF Core can't translate
  join w in db.Weapons on code equals w.Code into wg
  ```

  **Right:** use method syntax — aggregate fully in `Select` first, then project:
  ```csharp
  .GroupBy(f => f.Weapon)
  .Select(g => new { Code = g.Key, Count = g.Count() })
  .OrderByDescending(x => x.Count)
  .Select(x => x.Code)
  .FirstOrDefaultAsync(ct)
  ```
  If a join on the result is needed, do it as a second query after materialising the key.

- **`OrderByDescending` must come after a fully-aggregated `Select`**, not before a Select that still references the group. The pattern `GroupBy → Select(aggregations) → OrderByDescending → Select(projection)` translates cleanly.

### Search

`SearchController` accepts `q`, `game`, `st` (`"player"`, `"clan"`, or empty for both), and `page`. Player search queries `hlstats_PlayerNames` (all aliases), returning `MatchedName` — the alias that matched the query.

### Historical Rankings (Players)

`rankType` values: `"total"` (all-time), `"week"`, `"month"`, `yyyy-MM-dd`. Period data aggregates from `hlstats_Players_History`. `PlayerLeaderboardRow` unifies total and period results. Accuracy shows `"N/A"` for historical views (history table has no shots/hits).

## CSS Classes

| Class | Purpose |
|---|---|
| `data-table` | Main stats tables |
| `data-table-head` | Header row — also styles sort link `<a>` tags via descendant selector |
| `bg1` / `bg2` | Alternating row colours |
| `form-text` | Inline text inputs |
| `btn-small` | Small action buttons |
| `stats-table` | Secondary stats tables (weapons, maps on profile pages) |

### Leaderboard Filter Parameters

The Players/Index leaderboard accepts these URL parameters (all preserved across sort, pagination, and ranking-view changes):

| Parameter | Default | Notes |
|---|---|---|
| `game` | config default | Game code |
| `sortBy` | `"skill"` | Column to sort |
| `desc` | `true` | Sort direction |
| `rankType` | `"total"` | `"total"`, `"week"`, `"month"`, or `"yyyy-MM-dd"` |
| `minKills` | `1` | Minimum kills threshold — applied as `kills >= minKills` in DB |
| `page` | `1` | Page number |

`minKills` flows all the way from the controller through `IPlayerService` → `IPlayerRepository` → the query WHERE/HAVING clause. Any new leaderboard filter must propagate the same way and be included in `SortUrl`, the ranking-view form hidden fields, and the pagination URL.

## Tests

- Controller tests mock all services injected into the controller — the `Players/Index` action requires mocks for `GetRanksAsync`, `GetHistoryDatesAsync`, and `GetLeaderboardAsync`
- The `Profile` action makes ~20 service calls — see `PlayersControllerTests` for the complete mock list
- Repository tests in `Repositories/` folder need a real DB — exclude with `FullyQualifiedName!~RepositoryTests`

## Slash Commands

| Command | Purpose |
|---|---|
| `/add-feature <name>` | Full workflow for implementing a new feature |
| `/php-ref <feature>` | Analyse the PHP source for a feature before implementing |
| `/build-check` | Run build + tests and report results |
| `/feature-commit` | Stage and commit the current completed feature |

## PHP Live Reference

A live running instance of the original PHP site is available for visual and behaviour reference:
**https://tft.nervaware.co.uk/stats/hlstats.php**

Use this before implementing any feature to verify expected layout, data format, and edge cases.

## PHP Reference — Feature Map

Pages in `OriginalPHP/pages/` and their .NET status:

| PHP File(s) | Feature | Status |
|---|---|---|
| `players.php` | Player rankings | Done |
| `playerinfo.php`, `playerinfo_general.php`, `playerinfo_*.php` | Player profile | Done |
| `playerhistory.php` | Player skill history | Done |
| `bans.php` | Banned players | Done |
| `clans.php` | Clan rankings | Done |
| `claninfo.php`, `claninfo_*.php` | Clan profile | Partial |
| `weapons.php` | Weapon rankings | Done |
| `weaponinfo.php` | Weapon detail | Not started |
| `maps.php` | Map rankings | Done |
| `mapinfo.php` | Map detail | Not started |
| `servers.php` | Server list | Done |
| `awards.php`, `awards_daily.php`, `awards_global.php` | Awards | Partial |
| `awards_ranks.php`, `awards_ribbons.php` | Ranks & ribbons | Partial |
| `search.php`, `search-class.php` | Search | Done |
| `actions.php`, `actioninfo.php` | Actions/events | Not started |
| `roles.php`, `rolesinfo.php` | Roles | Not started |
| `chat.php`, `chathistory.php` | Chat log | Not started |
| `livestats.php` | Live server stats | Not started |
| `gameslist.php`, `game.php` | Games list | Not started |
| `countryclans.php`, `countryclansinfo.php` | Country stats | Not started |
| `rankinfo.php` | Rank detail | Not started |
| `ribboninfo.php` | Ribbon detail | Not started |
| `playersessions.php` | Player sessions | Not started |
| `playerawards.php` | Player awards detail | Not started |
| `ingame/` | In-game stats pages | Not started |
| `admin.php`, `admintasks/` | Admin panel | Partial |
