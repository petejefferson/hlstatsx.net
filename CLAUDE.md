# HLStatsX.NET — Claude Project Guide

## What This Project Is

A .NET 10 open-source rewrite of HLStatsX Community Edition — a PHP-based real-time player and clan statistics system for Half-Life engine games (Counter-Strike, Day of Defeat: Source, TF2, etc.). The goal is **complete feature parity with the PHP version**.

The original PHP source is the authoritative spec. It lives at **`legacy/php/`** (gitignored, not committed; see the README's "Legacy Reference Source" for how to fetch it). **Before implementing any feature, read the relevant PHP file first** to understand the intended behaviour, SQL queries, and edge cases.

The project will be released on GitHub for public use and contributions — code quality, patterns, and consistency matter.

## Solution Structure

```
src/
  HLStatsX.NET.Core/           # Domain entities, interfaces, models (no dependencies)
  HLStatsX.NET.Infrastructure/ # EF Core + MySQL implementations of Core interfaces
  HLStatsX.NET.Web/            # ASP.NET Core MVC app
  HLStatsX.NET.Awards/         # Worker service: daily awards, pruning, GeoIP maintenance
  HLStatsX.NET.Daemon/         # Worker service: real-time UDP log processor (hlstats.pl rewrite)
tests/
  HLStatsX.NET.Tests/          # xUnit unit tests (controllers, services)
.claude/commands/              # Project slash commands (see below)

PHP reference source (gitignored):   legacy/php/
Perl daemon reference (gitignored):  legacy/perl/scripts/
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
- **Entity columns must match the actual DB schema.** Only add properties that correspond to columns the PHP SQL queries actually SELECT. Never guess or add columns speculatively. Cross-check every entity property against the PHP SELECT list for that table — e.g. `hlstats_Countries` only has `flag` and `name`; adding a `code` property caused a runtime `MySqlException`. When in doubt, check `legacy/php/pages/admintasks/` for INSERT/UPDATE statements that reveal all real columns.

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

**Visual Studio build lock:** Pete develops with Visual Studio running the app in debug (F5). While VS has the app running, it holds a lock on output DLLs. `dotnet build` will fail with MSB3027/MSB3021 copy errors. Kill the web process before building using the PowerShell tool — do not ask Pete to stop VS:
```powershell
Stop-Process -Name "HLStatsX.NET.Web" -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 3
```
The Bash `taskkill` command does not reliably free the lock — always use PowerShell.

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

**Meter (bar graph) elements** must always use `<meter min="0" max="100" low="25" high="50" optimum="75" value="...">`. All five attributes are required — omitting `low`/`high`/`optimum` disables the green/yellow/red colour gradient. The colour CSS (green `#86CC00`, yellow `#FFDB1A`, red `#CC4600`) lives in `wwwroot/css/site.css` and applies globally; do not add inline colour styles to individual meters.

**Every table must have a `Rank` column as its first column.** Header: `<td style="text-align:right;">Rank</td>`. Data cell: `<td style="text-align:right;">@rowNum</td>` using a sequential loop counter. The header text is always "Rank" — never "#" or any other label.

**Every table must have sortable columns.** All data columns (except Rank and bar-graph ratio columns) must be clickable sort links using `SortUrl`/`Mark`. This applies to every table on every page — leaderboards, profile sub-tables, detail pages, etc.

For profile sub-tables (multiple sort pairs on one page), each `SortUrl` must preserve the sort state of all other tables, and sort URLs append `#tab-id` to return to the correct tab. Sort data in-memory in the controller using `private static IReadOnlyList<T> SortX(...)` helpers — do not add sort parameters to service/repository signatures.

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
- The `Profile` action makes ~22 service calls — see `PlayersControllerTests` for the complete mock list (includes `GetTrendDataAsync` and `GetGlobalAwardsAsync`)
- `PlayersController` now also injects `IWebHostEnvironment` (used by the `Sig` action) — tests must mock it
- Repository tests in `Repositories/` folder need a real DB — exclude with `FullyQualifiedName!~RepositoryTests`

## Slash Commands

| Command | Purpose |
|---|---|
| `/add-feature <name>` | Full workflow for implementing a new feature |
| `/php-ref <feature>` | Analyse the PHP source for a feature before implementing |
| `/build-check` | Run build + tests and report results |
| `/feature-commit` | Stage and commit the current completed feature |
| `/comparetophpsite` | Full parity check: PHP site vs .NET web app |
| `/comparetodaemon` | Full parity check: Perl daemon vs HLStatsX.NET.Daemon |

## PHP Live Reference

A local running instance of the original PHP site is the primary reference:
**http://localhost:5080/** (Docker — start from `legacy/php/`)

The .NET rewrite runs locally via Visual Studio (F5).

Use the PHP site before implementing any feature — it is the spec. The PHP source at `legacy/php/` is the authoritative code.

## Perl Daemon Reference

The original Perl daemon lives at `legacy/perl/scripts/` (gitignored, fetched from upstream; see the README). This is the canonical location — never reference the old live Perl installation elsewhere. Key files:

| File | Purpose |
|---|---|
| `hlstats.pl` | Main daemon: UDP listener, event dispatch loop, skill engine, server/player state |
| `HLstats_EventHandlers.plib` | All event handler subroutines (connect, kill, disconnect, chat, etc.) |
| `HLstats.plib` | Shared library: DB helpers, skill calc, clan detection, trend tracking |
| `HLstats_Player.pm` | Player object (session state, DB update, rank lookup) |
| `HLstats_Server.pm` | Server object (RCON, player count, config) |
| `HLstats_Game.pm` | Game object (weapon list, total player count) |
| `HLstats_GameConstants.plib` | Game type constants and code → ID mapping |
| `hlstats-awards.pl` | Nightly batch: awards, ribbons, GeoIP, pruning (**done** in HLStatsX.NET.Awards) |
| `hlstats-resolve.pl` | Background DNS resolver (**done** in HLStatsX.NET.Awards) |
| `proxy-daemon.pl` | UDP relay for forwarding log packets (optional) |
| `hlstats.conf` | Config file format reference |

## PHP Reference — Feature Map

Pages in `legacy/php/pages/` and their .NET status:

| PHP File(s) | Feature | Status |
|---|---|---|
| `players.php` | Player rankings | Done (~97%) |
| `playerinfo.php`, `playerinfo_general.php`, `playerinfo_*.php` | Player profile (incl. trend chart, forum sig, ribbons, global awards) | Done (~95%) |
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
| `livestats.php` | Live server stats (standalone) | Done (~97%) |
| `awards.php`, `awards_daily.php`, `awards_global.php` | Awards | Done (~95%) |
| `awards_ranks.php`, `awards_ribbons.php` | Ranks & ribbons listings | Done (~95%) |
| `rankinfo.php` | Rank detail | Done (~95%) |
| `ribboninfo.php` | Ribbon detail | Done (~97%) |
| `dailyawardinfo.php` | Daily award detail | Done (~97%) |
| `search.php`, `search-class.php` | Search | Done (~90%) |
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

## Daemon Reference — Feature Map

Subsystems in `legacy/perl/scripts/hlstats.pl` + `HLstats_EventHandlers.plib` and their .NET status in `src/HLStatsX.NET.Daemon/`.

### Core Engine

| Perl File(s) | Feature | Status |
|---|---|---|
| `hlstats.pl` | UDP listener (port 27500, configurable) | Done |
| `hlstats.pl` | STDIN import mode (`--stdin` for log replay) | Done |
| `hlstats.pl` | Log line parser (timestamp + event text) | Done |
| `hlstats.pl` | Player string parser (`Name<uid><steamid><team>`) | Done |
| `hlstats.pl` | Properties parser (`(key "value") …`) | Done |
| `hlstats.pl` | SteamID normalisation (`STEAM_X:Y:Z` → `STEAM_0:Y:Z`) | Done |
| `hlstats.pl` | Bot detection (`BOT` / `0` / `00000000:N:0` uniqueids) | Done |
| `hlstats.pl` | Multi-server in-memory state (per-server player registry) | Done |
| `hlstats.pl` | Server lookup + auto-registration from DB | Done — `AutoRegisterServerAsync` inserts server + copies game defaults; game set via `Daemon:AutoRegisterGame` appsetting |
| `hlstats.pl` | Server auto-detection via A2S_INFO query | Done — `A2SQueryService`; wired into `MapChangeHandler` as live map fallback |
| `hlstats.pl` | Per-server config loading from `hlstats_Servers_Config` | Done |
| `hlstats.pl` | Global config loading from `hlstats_Options` | Done |
| `hlstats.pl` | File-based config (`hlstats.conf` equivalent) | Done via appsettings.json |
| `hlstats.pl` | Event queue with batch INSERT (configurable queue size) | Done |
| `hlstats.pl` | Livestat updates (hlstats_Livestats per-player live row) | Done |
| `hlstats.pl` | Graceful shutdown / config reload (SIGINT / SIGHUP) | Done — SIGINT flushes sessions on shutdown; SIGHUP flushes + clears + reloads config (Linux/macOS only; Windows skips) |
| `hlstats.pl` | Proxy daemon protocol support (decode incoming proxy packets) | Deferred |
| `proxy-daemon.pl` | Proxy relay service (separate UDP forwarder process) | Deferred |

### Event Handlers

| Perl File(s) | Feature | Status |
|---|---|---|
| `HLstats_EventHandlers.plib` | Connect — record IP, hostname, rank-based kick | Done (rank kick deferred) |
| `HLstats_EventHandlers.plib` | Enter Game | Done |
| `HLstats_EventHandlers.plib` | Disconnect — flush session stats to DB | Done |
| `HLstats_EventHandlers.plib` | Team selection | Done |
| `HLstats_EventHandlers.plib` | Role/class selection | Done |
| `HLstats_EventHandlers.plib` | Name change | Done |
| `HLstats_EventHandlers.plib` | Kill (Frag) — skill calc, headshot, kill streak | Done |
| `HLstats_EventHandlers.plib` | Team kill — TK penalty | Done |
| `HLstats_EventHandlers.plib` | Suicide — penalty | Done |
| `HLstats_EventHandlers.plib` | Player action (objective bonuses) | Done |
| `HLstats_EventHandlers.plib` | Player-Player action (pvp bonuses/penalties) | Done |
| `HLstats_EventHandlers.plib` | Team bonus (reward all team members) | Done |
| `HLstats_EventHandlers.plib` | Map change / World triggered events | Done |
| `HLstats_EventHandlers.plib` | RCON command logging | Deferred |
| `HLstats_EventHandlers.plib` | Admin command logging | Deferred |
| `HLstats_EventHandlers.plib` | Statsme (weapon shots/hits/damage/kills) | Done |
| `HLstats_EventHandlers.plib` | Statsme2 (per-hitbox stats) | Deferred |
| `HLstats_EventHandlers.plib` | Latency/ping tracking | Deferred |
| `HLstats_EventHandlers.plib` | Chat logging | Done |

### Skill & State

| Perl File(s) | Feature | Status |
|---|---|---|
| `HLstats.plib` | Skill calculation — standard ELO-like (modes 0–4) | Done |
| `HLstats.plib` | Skill calculation — ZPS mode (mode 5, team-based) | Done |
| `HLstats.plib` | L4D skill calculation (difficulty-weighted) | Done |
| `HLstats.plib` | Kill streak tracking (per-life, all-time) | Done |
| `HLstats.plib` | Clan tag matching + auto-create clan from tag | Done |
| `HLstats.plib` | Stats trend tracking (every 5 min → `hlstats_Trend`) | Done |
| `HLstats.plib` | Bonus round detection + ignore window | Done |
| `HLstats.plib` | GeoIP lookup on connect (MaxMind MMDB) | Done |
| `hlstats-resolve.pl` | Background DNS resolver (player IPs → hostnames) | Done — `DnsResolveService` + `HostGroupClassifier` in `HLStatsX.NET.Awards` |

### RCON & Broadcasting (deferred — post-parity)

All RCON features are explicitly deferred. The daemon will process logs and write all DB stats correctly without them. RCON enables optional real-time in-game feedback (e.g. `"Player1 killed Player2 (+3 pts)"` appearing in chat, auto-kick, team balance), but is not required for stat accuracy. Implement after all event handlers and skill calculation are complete.

| Perl File(s) | Feature | Status |
|---|---|---|
| `TRcon.pm` | Half-Life/Source RCON client | Done — `SourceRconClient` (TCP, challenge/auth, split-end, socket recycling) |
| `BASTARDrcon.pm` | Alternative RCON client (HL1/GoldSrc) | Done — `GoldSrcRconClient` (UDP, challenge-response) |
| `HLstats_Server.pm` | RCON broadcasting — kill/death event messages in-game | Done — `FragHandler` via `ServerBroadcastService` |
| `HLstats_Server.pm` | RCON broadcasting — player stats on connect | Done — `EnterGameHandler` connect announce |
| `HLstats_Server.pm` | RCON broadcasting — next rank display | Deferred |
| `hlstats.pl` | Global chat relay (public chat between servers) | Deferred |
| `hlstats.pl` | Auto-team balance via RCON | Deferred |
| `hlstats.pl` | Min-rank player kick via RCON | Done — `ConnectHandler` rank kick |
| `hlstats.pl` | Global banning enforcement | Deferred |

### Awards / Maintenance (separate project)

| Perl File(s) | Feature | Status |
|---|---|---|
| `hlstats-awards.pl` | DoInactive — player activity scoring | Done |
| `hlstats-awards.pl` | DoAwards — daily + global award winners | Done |
| `hlstats-awards.pl` | DoRibbons — ribbon award assignment | Done |
| `hlstats-awards.pl` | DoGeoIP — batch GeoIP for unknown players | Done |
| `hlstats-awards.pl` | DoClans — reparse names → clan affiliations | Done |
| `hlstats-awards.pl` | DoPruning — delete old events/history/trend | Done |
| `hlstats-awards.pl` | DoOptimize — OPTIMIZE TABLE all tables | Done |
| `hlstats-resolve.pl` | DNS resolve background pass | Done |
