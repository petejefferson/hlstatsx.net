# HLStatsX.NET — PHP-to-.NET Feature Gap Analysis
_Generated 2026-04-13_

---

## Overall Parity

| Scope | Completion |
|---|---|
| **Public stats pages** | **~55%** |
| **Admin panel** | **~15%** |
| **Overall (weighted 75/25)** | **~45%** |

Scoring is per-feature weighted by completeness of data/UI rendered relative to the PHP original.

---

## Prioritised Work Queue

Priority tiers:

| Tier | Rationale |
|---|---|
| **P1 — High** | Core navigation item visible in the top nav bar; users hit it directly |
| **P2 — Medium** | Linked from an already-implemented page; broken/missing links degrade existing pages |
| **P3 — Low** | Supplementary detail pages, admin tools, and legacy features |

---

### P1 — High Priority (Unimplemented nav bar / core pages)

#### 1. Clan Profile — Missing tabs (Weapons, Maps, Actions, Teams)
- **PHP refs:** `claninfo_weapons.php`, `claninfo_mapperformance.php`, `claninfo_actions.php`, `claninfo_teams.php`
- **Current .NET state:** `Clans/Profile` only shows the member list. No tabs.
- **Gap:** 4 of 5 clan-profile tabs missing; clan profile is ~30% complete.
- **What to build:** Add weapon stats, map performance, actions performed, and team breakdown to `Clans/Profile`.

#### 2. Weapon Detail — Player Leaderboard for a Weapon
- **PHP ref:** `weaponinfo.php`
- **Current .NET state:** `Weapons/Detail` shows 5 basic weapon stats only (code, kills, HS, modifier).
- **Gap:** PHP page shows a ranked, paginated, sortable table of _players_ using that weapon (kills + headshot counts). The .NET view is a stub.
- **What to build:** Query `hlstats_Events_Frags` grouped by killer for the weapon, render sortable paginated player list.

#### 3. Map Detail — Player Leaderboard for a Map
- **PHP ref:** `mapinfo.php`
- **Current .NET state:** `Maps/Detail` renders the basic `Map` entity from the DB with no player data.
- **Gap:** PHP page shows top players on the map (kills, headshots, HS%), plus optionally a map image. The .NET view is a stub.
- **What to build:** Query `hlstats_Events_Frags` grouped by killer for the map, render sortable paginated player list. Show map image if available.

#### 4. Actions / Action Detail
- **PHP refs:** `actions.php`, `actioninfo.php`
- **Current .NET state:** No `ActionsController`, no views, no service, no repository.
- **Gap:** Entire feature missing. Shows in the PHP nav bar.
- **What to build:** Full stack — entity, repository, service, controller, views for the actions leaderboard and per-action player rankings. Uses `hlstats_Actions`, `hlstats_PlayerActions`, `hlstats_PlayerPlayerActions`.

#### 5. Roles / Role Detail
- **PHP refs:** `roles.php`, `rolesinfo.php`
- **Current .NET state:** No `RolesController`, no views.
- **Gap:** Entire feature missing. Shows in the PHP nav bar.
- **What to build:** Full stack — roles leaderboard (`hlstats_Roles` + player aggregations) and per-role player list.

#### 6. Multi-Game Selector (Games List)
- **PHP refs:** `gameslist.php`, `contents.php`, `game.php`
- **Current .NET state:** App is hard-coded to `HLStatsX:DefaultGame` with no way for a visitor to switch game. There is no game-selector UI anywhere. If the DB contains multiple games, they are invisible.
- **Gap:** The PHP site auto-detects all active games and renders game icons in the header for switching. Single-game sites auto-redirect.
- **What to build:** A `GamesController.Index` and header partial that queries `hlstats_Games` (hidden=0), displays game icons and names, links each to `?game=<code>`. Auto-redirect if only one game. Plumb the selected game into a cookie or query string throughout the layout.

---

### P2 — Medium Priority (Broken links from existing pages / high-value detail pages)

#### 7. Daily Award Detail Page
- **PHP ref:** `dailyawardinfo.php`
- **Current .NET state:** On the home page and Awards/Index, award names link to `?mode=dailyawardinfo&award=<id>` in PHP. In the .NET rewrite these links go nowhere.
- **Gap:** Page shows the per-day winner history for a specific award (date, player, count).
- **What to build:** `Awards/DailyAwardDetail(int id)` — query `hlstats_PlayerAwards_Dau` for history, render table.

#### 8. Rank Detail Page (Players at a Rank)
- **PHP ref:** `rankinfo.php`
- **Current .NET state:** `Awards/Ranks` lists all ranks but rank names are not linked.
- **Gap:** PHP page shows all players currently at a given rank tier (sorted by skill).
- **What to build:** `Awards/RankDetail(int id)` — query `hlstats_Ranks` + `hlstats_Players` by kill range, render sortable paginated player list.

#### 9. Global Awards Tab on Awards Page
- **PHP ref:** `awards_global.php`
- **Current .NET state:** `Awards/Index` shows daily awards and lists all award names, but the "all-time global winners" section is missing.
- **Gap:** PHP awards page has a full tab showing the global all-time winner per award (stored in `hlstats_Awards.g_winner_id` / `g_winner_count`).
- **What to build:** Add `GetGlobalAwardWinnersAsync` to `IAwardService`/repo, render the global awards section in `Awards/Index`.

#### 10. Player Awards History Page
- **PHP ref:** `playerawards.php`
- **Current .NET state:** Player profile shows earned awards, but there is no dedicated paginated history page listing every award win by date.
- **Gap:** PHP page shows a full historical table of awards earned (date, award name, count) linked from the profile.
- **What to build:** `Players/Awards(int id)` — query `hlstats_PlayerAwards_Dau` for the player, render sortable history table. Link from the player profile.

#### 11. Player Sessions Page
- **PHP ref:** `playersessions.php`
- **Current .NET state:** Not implemented at all.
- **Gap:** PHP page shows per-session stats (date, server, kills, deaths, skill change, duration). Linked from the player profile.
- **What to build:** `Players/Sessions(int id)` — query session-level aggregations from event log tables, render table. Link from profile.

#### 12. Chat Log (Global & Per-Server)
- **PHP refs:** `chat.php`, `chathistory.php`
- **Current .NET state:** Not implemented.
- **Gap:** `chat.php` shows recent in-game chat messages (from `hlstats_Events_Chat`) for the whole game or a specific server. `chathistory.php` shows top chatters and message statistics.
- **What to build:** `ChatController.Index` (recent messages, filterable by server) and `ChatController.History` (top chatters). Both are read-only queries against `hlstats_Events_Chat`.

---

### P3 — Low Priority (Admin panel, supplementary pages, legacy features)

#### 13. Admin — Site Options / Settings
- **PHP ref:** `admintasks/options.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP admin allows editing global config values stored in `hlstats_Options` (site URL, page size, delete days, etc.).
- **What to build:** `Admin/Options` GET/POST — load and save `hlstats_Options` rows.

#### 14. Admin — Game Management
- **PHP ref:** `admintasks/games.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP admin can add, edit, and delete games in `hlstats_Games`.
- **What to build:** `Admin/Games` CRUD — list, add, edit, delete game records.

#### 15. Admin — Server Create / Edit / Settings
- **PHP refs:** `admintasks/newserver.php`, `admintasks/serversettings.php`
- **Current .NET state:** Admin can list and delete servers but cannot create or edit them.
- **Gap:** PHP admin has add-new-server form and server-settings page (name, game, log key, etc.).
- **What to build:** `Admin/CreateServer` and `Admin/EditServer` forms backed by `IServerService.CreateServerAsync` / `UpdateServerAsync` (interfaces already exist).

#### 16. Admin — Actions / Ranks / Ribbons / Roles / Teams / Weapons / Awards Management
- **PHP refs:** `admintasks/actions.php`, `admintasks/ranks.php`, `admintasks/ribbons.php`, `admintasks/ribbons_trigger.php`, `admintasks/roles.php`, `admintasks/teams.php`, `admintasks/weapons.php`, `admintasks/awards_*.php`
- **Current .NET state:** None of these CRUD pages exist.
- **Gap:** PHP admin allows full CRUD on all game-configuration data.
- **What to build:** Individual admin CRUD controllers/views for each entity type. These are low-risk read-write pages against already-mapped tables.

#### 17. Admin — Player / Clan Edit Tools
- **PHP refs:** `admintasks/tools_editdetails.php`, `tools_editdetails_player.php`, `tools_editdetails_clan.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP admin can edit player details (ban, change name, merge, delete) and clan details.
- **What to build:** `Admin/EditPlayer(int id)` and `Admin/EditClan(int id)` forms. `BanPlayerAsync`/`UnbanPlayerAsync` are already in `IPlayerService`.

#### 18. Admin — Admin Tools (Optimize, Reset, Synchronize, IP Stats, Copy Settings)
- **PHP refs:** `admintasks/tools_*.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP admin has DB maintenance tools and statistical utilities.
- **What to build:** Individual admin tool pages. Lower priority than CRUD management pages.

#### 19. Admin — Host Groups & Clan Tag Management
- **PHP refs:** `admintasks/hostgroups.php`, `admintasks/clantags.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP admin can define host groups and auto-assign clan tags.

#### 20. Live Stats as a Standalone Page
- **PHP ref:** `livestats.php`
- **Current .NET state:** Livestats are embedded in Home and Servers/Detail but there is no direct `?mode=livestats` page.
- **Gap:** PHP has a dedicated standalone live-stats URL that can be queried directly (e.g., from the in-game MOTD).
- **What to build:** `Servers/Livestats(int id)` as a standalone, minimal HTML page or JSON endpoint.

#### 21. Teamspeak / Ventrilo Voice Comm Pages
- **PHP refs:** `teamspeak.php`, `ventrilo.php`, `voicecomm_serverlist.php`
- **Current .NET state:** Not implemented.
- **Gap:** PHP shows connected voice-comm users (TeamSpeak/Ventrilo). Legacy feature; most servers have moved to Discord.
- **Priority:** Very low — skip unless explicitly requested.

---

## Feature-by-Feature Status Table

| Feature | PHP File(s) | .NET Status | Completeness |
|---|---|---|---|
| Home / game dashboard | `game.php`, `contents.php` | ✅ Implemented | ~95% |
| Player leaderboard | `players.php` | ✅ Implemented | ~95% |
| Player profile | `playerinfo.php` + sub-files | ✅ Implemented | ~95% |
| Player skill history chart | `playerhistory.php` | ✅ Implemented | ~90% |
| Player forum sig image | `sig.php` | ✅ Implemented | ~95% |
| Clan leaderboard | `clans.php` | ✅ Implemented | ~90% |
| Weapon leaderboard | `weapons.php` | ✅ Implemented | ~90% |
| Map leaderboard | `maps.php` | ✅ Implemented | ~90% |
| Server list + detail | `servers.php` | ✅ Implemented | ~90% |
| Rank list | `awards_ranks.php` | ✅ Implemented | ~85% |
| Ribbon list | `awards_ribbons.php` | ✅ Implemented | ~85% |
| Ribbon detail | `ribboninfo.php` | ✅ Implemented | ~80% |
| Awards (daily + list) | `awards.php`, `awards_daily.php` | ✅ Implemented | ~75% |
| Banned players | `bans.php` | ✅ Implemented | ~90% |
| Search | `search.php` | ✅ Implemented | ~90% |
| Country leaderboard | `countryclans.php` | ✅ Implemented | ~90% |
| Country profile | `countryclansinfo.php` | ✅ Implemented | ~85% |
| Admin login / dashboard | `adminauth.php`, `admin.php` | ✅ Implemented | ~65% |
| Admin users CRUD | `admintasks/adminusers.php` | ✅ Implemented | ~70% |
| Admin servers (list+delete) | `admintasks/servers.php` | ⚠️ Partial | ~40% |
| Clan profile | `claninfo.php` + sub-files | ⚠️ Partial | ~30% |
| Weapon detail (player list) | `weaponinfo.php` | ⚠️ Stub | ~20% |
| Map detail (player list) | `mapinfo.php` | ⚠️ Stub | ~20% |
| Global awards section | `awards_global.php` | ⚠️ Partial | ~10% |
| Actions leaderboard | `actions.php` | ❌ Not started | 0% |
| Action detail | `actioninfo.php` | ❌ Not started | 0% |
| Roles leaderboard | `roles.php` | ❌ Not started | 0% |
| Role detail | `rolesinfo.php` | ❌ Not started | 0% |
| Multi-game selector | `gameslist.php`, `contents.php` | ❌ Not started | 0% |
| Daily award detail | `dailyawardinfo.php` | ❌ Not started | 0% |
| Rank detail (players at rank) | `rankinfo.php` | ❌ Not started | 0% |
| Player sessions | `playersessions.php` | ❌ Not started | 0% |
| Player award history page | `playerawards.php` | ❌ Not started | 0% |
| Chat log (live) | `chat.php` | ❌ Not started | 0% |
| Chat history / top chatters | `chathistory.php` | ❌ Not started | 0% |
| Admin — server create/edit | `admintasks/newserver.php` | ❌ Not started | 0% |
| Admin — site options | `admintasks/options.php` | ❌ Not started | 0% |
| Admin — games management | `admintasks/games.php` | ❌ Not started | 0% |
| Admin — actions/ranks/ribbons/roles/teams/weapons CRUD | `admintasks/actions.php` etc. | ❌ Not started | 0% |
| Admin — player/clan edit tools | `admintasks/tools_editdetails_*.php` | ❌ Not started | 0% |
| Admin — DB tools | `admintasks/tools_*.php` | ❌ Not started | 0% |
| Teamspeak / Ventrilo | `teamspeak.php`, `ventrilo.php` | ❌ Skip (legacy) | — |
| In-game pages | `ingame/` | ❌ Not started | 0% |

---

## Summary of What's Done Well

The rewrite has a strong foundation:
- All major read-only leaderboard pages (Players, Clans, Maps, Weapons, Countries) are fully functional.
- The **Player Profile** is very thorough — 22+ concurrent queries covering all sub-tabs from the PHP original.
- Pagination, sortable columns, and game-parameter propagation are consistent across all implemented pages.
- The **Home page** faithfully mirrors the PHP dashboard including livestats, server load chart, and daily awards.
- **Forum signature image** generation (SkiaSharp PNG) matches the PHP GD implementation.
- **Admin authentication** with cookie-based claims is in place.

## Key Gaps by Impact

1. **Multi-game selector** — Without it, the rewrite only works for admins who can edit config. Visitors cannot switch games. This is a launch blocker for multi-game sites.
2. **Clan profile tabs** — Clan profile is the second most-visited detail page after player profile. Currently ~30% complete.
3. **Weapon/Map detail pages** — Listed in the leaderboard but clicking through shows almost no information. Broken UX.
4. **Actions & Roles** — Both appear in the PHP nav bar. Entire features absent from .NET nav.
5. **Chat log** — High-visibility real-time feature used daily on active communities.
6. **Admin panel depth** — Only login, user list, and server list exist. All configuration and tooling missing.
