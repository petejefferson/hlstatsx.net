# HLStatsX.NET — Copilot Instructions

A .NET 10 rewrite of HLStatsX Community Edition — a PHP-based stats system for Half-Life engine games. Goal: **complete feature parity with the PHP version**. `legacy/php/` is the authoritative spec; read the relevant PHP file before implementing any feature.

## Workflows (Skills)

These project skills are available and should be invoked for the matching tasks — do not replicate their logic manually:

| Skill | When to use |
|---|---|
| `php-ref` | **Before implementing any feature** — analyses the PHP spec: what it renders, SQL queries, URL params, edge cases |
| `add-feature` | Full bottom-up implementation workflow (PHP ref → plan layers → implement → build → test → commit suggestion) |
| `build-check` | After any change — runs `dotnet build --no-incremental` + non-repository tests and reports results |
| `feature-commit` | Safely stage and commit a completed feature (reviews diff, matches commit style, guards against committing credentials) |

## Commands

```bash
dotnet build
dotnet run --project src/HLStatsX.NET.Web
dotnet test --filter "FullyQualifiedName!~RepositoryTests"          # excludes DB-dependent tests
dotnet test --filter "FullyQualifiedName~PlayersControllerTests"    # run a single test class
```

> Repository tests (`tests/.../Repositories/`) require a live MySQL connection — always exclude them in CI and when no DB is available.

> If `dotnet build` fails with MSB3027/MSB3021 copy errors, Visual Studio has the app locked — stop the VS debug session first.

## Architecture

Strict layering: **Core → Infrastructure → Web**

| Layer | Project | Contains |
|---|---|---|
| Core | `HLStatsX.NET.Core` | Entities, repository interfaces, service interfaces, shared models. No EF or ASP.NET Core references. |
| Infrastructure | `HLStatsX.NET.Infrastructure` | EF Core repository implementations, `HLStatsDbContext`, services. All DB queries live here. |
| Web | `HLStatsX.NET.Web` | ASP.NET Core MVC controllers, Razor views, view models. |

**Feature implementation order:** entity/interface → repository → service → view model → controller → view.

Controllers call **services only** — never repositories directly. Services are thin wrappers over repositories; put logic there only if it belongs between HTTP and the database.

## Database

- MySQL via `Pomelo.EntityFrameworkCore.MySql`
- Connection string key: `HLStats`
- `HLStatsDbContext` registered as `IDbContextFactory<HLStatsDbContext>` (singleton). Repositories call `_factory.CreateDbContext()` per method — required for concurrent `Task.WhenAll` because `DbContext` is not thread-safe.
- `QueryTrackingBehavior.NoTracking` is set globally.
- **EF Core is pinned to 9.x** — Pomelo has no 10.x release yet. Do not upgrade past `9.0.*`.
- Add new entities as `DbSet<T>` properties in `HLStatsDbContext`.

## Coding Standards

- All service/repository methods: `async Task<T>` with `CancellationToken ct = default`
- No `.Result` or `.Wait()` — `await` throughout
- No speculative abstractions for single uses
- Always filter by `game` parameter — never hardcode a game code

## Key Patterns

### Pagination

All paged queries return `PagedResult<T>`. In views:

```cshtml
<partial name="_Pagination" model="@PaginationModel.From(Model.Result, p => Url.Action("Index", new { page = p })!)" />
```

### Sortable Column Headers

Every list page defines local Razor functions (copy this pattern exactly):

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

- Always use `@(expr)` in HTML attributes — implicit `@expr` breaks when the expression contains string literals with double quotes.
- Use literal Unicode (`▼` `▲`) in C# strings, not `Html.Raw()` with HTML entities.
- `@{...}` blocks inside `@if {}` must appear **before** any HTML output in that scope.

### EF Core GroupBy

Never use `let` after `group...into` in query syntax — EF Core cannot translate it. Use method syntax instead:

```csharp
// Wrong: let after group...into causes untranslatable LINQ
group f by f.Weapon into g
let code = g.Key  // ← breaks EF Core translation

// Right: fully aggregate in Select first
.GroupBy(f => f.Weapon)
.Select(g => new { Code = g.Key, Count = g.Count() })
.OrderByDescending(x => x.Count)
.Select(x => x.Code)
.FirstOrDefaultAsync(ct)
```

`OrderByDescending` must come after a fully-aggregated `Select`. If a join on the result is needed, do it as a second query after materialising the key.

### New Leaderboard Filters

Any new filter on the players leaderboard must propagate through the full stack: controller parameter → `IPlayerService` → `IPlayerRepository` → SQL WHERE/HAVING. It must also appear in `SortUrl`, the ranking-view form hidden fields, and the pagination URL. See `minKills` as the reference example.

## CSS Classes

| Class | Purpose |
|---|---|
| `data-table` | Main stats tables |
| `data-table-head` | Header row (also styles sort `<a>` tags via descendant selector) |
| `bg1` / `bg2` | Alternating row colours |
| `form-text` | Inline text inputs |
| `btn-small` | Small action buttons |
| `stats-table` | Secondary tables (weapons, maps on profile pages) |

## Tests

- xUnit + Moq + FluentAssertions
- Controller tests mock all injected services. `PlayersController` requires mocks for `IPlayerService`, `IAwardService`, `IConfiguration`, and `IWebHostEnvironment`.
- The `Profile` action makes ~22 service calls — see `PlayersControllerTests` for the full mock list.

## PHP Reference

| PHP File(s) | Feature | Status |
|---|---|---|
| `players.php` | Player rankings | Done (~97%) |
| `playerinfo.php`, `playerinfo_*.php` | Player profile | Done (~95%) |
| `playerhistory.php` | Player event history | Done (~97%) |
| `bans.php` | Banned players | Done (~95%) |
| `playersessions.php` | Player sessions | Done (~95%) |
| `playerawards.php` | Player awards detail | Done (~98%) |
| `clans.php` | Clan rankings | Done (~95%) |
| `claninfo.php`, `claninfo_*.php` | Clan profile | Done (~95%) |
| `weapons.php` | Weapon rankings | Done (~95%) |
| `weaponinfo.php` | Weapon detail | Done (~95%) |
| `maps.php` | Map rankings | Done (~95%) |
| `mapinfo.php` | Map detail | Done (~95%) |
| `servers.php` | Server list + detail | Done (~95%) |
| `livestats.php` | Live server stats | Done (~97%) |
| `awards.php`, `awards_daily.php`, `awards_global.php` | Awards | Done (~95%) |
| `awards_ranks.php`, `awards_ribbons.php` | Ranks & ribbons listings | Done (~95%) |
| `rankinfo.php` | Rank detail | Done (~95%) |
| `ribboninfo.php` | Ribbon detail | Done (~97%) |
| `dailyawardinfo.php` | Daily award detail | Done (~97%) |
| `search.php` | Search | Done (~90%) |
| `actions.php`, `actioninfo.php` | Actions/events | Done (~95%) |
| `roles.php`, `rolesinfo.php` | Roles | Done (~95%) |
| `chat.php`, `chathistory.php` | Chat log + player history | Done (~98%) |
| `contents.php`, `gameslist.php` | Games list landing page | Done (~98%) |
| `help.php` | Help / reference page | Done (~95%) |
| `game.php` | Game dashboard (single-game) | Done (~95%) |
| `countryclans.php`, `countryclansinfo.php` | Country stats | Done (~95%) |
| `ingame/` | In-game stats pages | Done (~90%) |
| `admin.php`, `admintasks/` (excl. `tools_synchronize.php`) | Admin panel | Done (~98%) |
| `admintasks/tools_ipstats.php` | Admin — IP stats (connect frequency by hostgroup) | Done (~95%) |
| `admintasks/voicecomm.php` | Admin — VoiceComm server management | Done (~95%) |
| `admintasks/tools_perlcontrol.php` | Admin — Daemon control (send RELOAD/KILL to daemon) | Done (~95%) |
| `admintasks/tools_resetdbcollations.php` | Admin — Reset DB collations | Done (~95%) |
| `admintasks/tools_synchronize.php` | Admin — VAC master sync | N/A — Obsolete (master servers defunct) |

Live PHP reference: **https://tft.nervaware.co.uk/stats/hlstats.php**  
Live .NET rewrite: **https://tft.nervaware.co.uk/stats**

## Announcement and Marketing Pages

- Announcement or marketing pages should be self-isolated under `/blog` and should not modify main project views or shared styles.
