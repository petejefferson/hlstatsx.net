# HLStatsX.NET — Feature Parity Report

**Generated:** 2026-05-16 (rev 17 — config section Maps renamed to Geo; scores unchanged)  
**Methodology:** Source code ground truth — every feature verified against the PHP source in `legacy/php/pages/` and the .NET implementation in `src/HLStatsX.NET.Web/`.

---

## Overall Parity Summary

| Area | Score | Notes |
|---|---|---|
| Public Stats Pages (75% weight) | 97% | Steady |
| Admin Panel (25% weight) | 98% | Steady — 15 admin pages swept and confirmed complete |
| **Web Frontend (weighted)** | **97%** | Steady |
| HLStatsX.NET.Awards | 100% | Feature-complete |
| HLStatsX.NET.Daemon | 96% | A2S_INFO done; RCON clients + broadcasting wired into all event handlers |
| **Overall** | **~97%** | Steady |

---

## Changed Since Last Report (2026-05-16 rev 17)

### Config rename: HLStatsX:Maps → HLStatsX:Geo

The `Maps` section under `HLStatsX` in `appsettings.json` was renamed to `Geo` to better reflect that it holds geography/GeoIP-related configuration (currently `GoogleMapsApiKey`). Updated in `appsettings.json`, `ClansController.cs`, and `Home/Index.cshtml`.

**Score unchanged: 97% overall.**

---

## Changed Since Last Report (2026-05-13 rev 16)

### A2S_INFO Query + Full RCON Subsystem

| Component | Perl Reference | .NET Implementation | Score Change |
|---|---|---|---|
| A2S_INFO server query | `hlstats.pl` `queryServer()` | `A2SQueryService` — sends 25-byte UDP packet, parses binary response (null-terminated strings, little-endian ints); wired into `MapChangeHandler` as live map fallback | Not started → **Done** |
| Source RCON client | `TRcon.pm` | `SourceRconClient` — TCP, challenge/auth (handles Source's junk pre-response), split-end packet detection, socket recycled every 100 commands, retry on socket error, `SemaphoreSlim` thread safety | Deferred → **Done** |
| GoldSrc RCON client | `BASTARDrcon.pm` | `GoldSrcRconClient` — UDP, two-step challenge-response (`challenge rcon` then `rcon <n> "<pass>" <cmd>`), 5-byte header strip, 0.5 s timeout | Deferred → **Done** |
| RCON broadcast service | `HLstats_Server.pm` `dorcon` / `messageAll` / `messageMany` | `ServerBroadcastService` — connection pool keyed by `address:port`; selects Source vs GoldSrc from `ServerConfig.GameEngine`; `MessageAllAsync` (force flag), `MessagePlayerAsync` (guards `DisplayEvents` + `IsBot`), `KickPlayerAsync` (engine-specific command), semicolon sanitisation | Deferred → **Done** |
| Kill/TK broadcast | `HLstats_EventHandlers.plib` frag handler | `FragHandler` — broadcasts kill message to killer and victim via `MessagePlayerAsync` when `BroadcastEvents` enabled | Deferred → **Done** |
| Connect announce | `HLstats_EventHandlers.plib` enter-game handler | `EnterGameHandler` — broadcasts `"{name} (Pos {rank} with {kills} kills) has connected [from {country}]"` when `ConnectAnnounce` enabled | Deferred → **Done** |
| Min-rank kick | `HLstats_EventHandlers.plib` connect handler | `ConnectHandler` — queries player rank after connect; kicks with `kickid`/`kick #id` if rank exceeds `MinRank` threshold | Deferred → **Done** |
| Action/team broadcasts | `PlayerActionHandler`, `PlayerPlayerActionHandler`, `TeamBonusHandler` | All three wired: broadcasts `"{name} got N points for {action}"` / `"Your team got N points for {action}"` when `BroadcastEvents && BroadcastPlayerActions` | Deferred → **Done** |

**Still deferred:** global chat relay, auto-team balance, next-rank-to-achieve display, global ban enforcement, RCON/admin event logging, Statsme2, latency tracking, proxy daemon protocol.

**30 new unit tests** (641 total): `A2SQueryServiceTests` (binary parse), `ServerBroadcastServiceTests` (all guard conditions + command format + pool disposal), `SourceRconClientPacketTests` (wire format), `GoldSrcRconClientProtocolTests` (challenge parsing).

**Daemon score: 91% → 96%**

---

## Changed Since Last Report (2026-05-13 rev 15)

### Full PHP File Inventory Sweep — No New Gaps

Performed a complete inventory of all `.php` files in `legacy/php/pages/`, `legacy/php/pages/admintasks/`, and `legacy/php/pages/ingame/` and cross-checked each against the .NET implementation. Findings:

| PHP File | Finding |
|---|---|
| `voicecomm_serverlist.php` | Sub-component included by `game.php` (renders voice server list on game dashboard). Covered by the existing Game Dashboard implementation. Not a separate feature. |
| `profile.php` | SQL query profiler for PHP development — reads `hlstats_sql_web_profile` and `hlstats_sql_daemon_profile`. Development tool, not applicable to .NET. |
| `updater.php` | PHP DB schema updater (applies incremental SQL patches from `updater/` directory). Completely superseded by EF Core migrations. Not applicable. |
| `admintasks/tools_reset_2.php` | "Cleanup Inactive" admin tool — confirmed mapped to `Admin/Tools/Cleanup` action in `AdminController.cs`. Previously listed generically as "Cleanup Inactive". No gap. |
| `admintasks/awards_plyractions.php`, `awards_plyrplyractions.php`, `awards_plyrplyractions_victim.php`, `awards_weapons.php` | All four award-type admin CRUD pages covered by the generic `Awards/{type}` route in `AdminController.cs` (W/P/O/V types). Confirmed no gap. |
| `admintasks/servers.php` | Server list admin page — covered by `Admin/Servers` route (same CRUD as `newserver.php`/`serversettings.php`). Confirmed no gap. |
| All 21 `ingame/*.php` files | Re-verified against `InGameController.cs` and 19 views. 19 functional pages present; `footer.php` and `header.php` are layout partials handled by `_InGameLayout.cshtml`. Confirmed no gap. |

**Score unchanged: 97% overall.** HTML report footer date and daemon progress card (stale 88% → corrected 91%) also fixed.

---

## Changed Since Last Report (2026-05-12 rev 14)

### Web Page Sweep — Gaps Closed

Full sweep of all 90%–95% web pages against PHP source. Confirmed phantom gaps and closed real ones:

| Page | PHP Reference | Gap Found | Fix | Score Change |
|---|---|---|---|---|
| Player Rankings | `players.php` | `last_skill_change` trend arrow missing on Points column | Added `LastSkillChange` to `Player` entity + EF mapping; `PlayerLeaderboardRow.LastSkillChange` set in `MapToRows`; view shows ▲ (green) / ▼ (red) next to Points | 95% → **97%** |
| Clan Rankings | `clans.php` | `last_skill_change` trend arrow missing on Avg. Points column; only autocomplete was previously noted | Added `AvgLastSkillChange = AVG(last_skill_change)` to `ClanRepository` query + `ClanLeaderboardRow`; view shows ▲/▼ next to Avg. Points | 90% → **95%** |
| Awards | `awards_daily.php`, `awards_global.php` | Daily Awards tab shows no date; missing "Daily Awards (date)" header matching PHP; award image fallback hides instead of showing `award.png` | Added `GetAwardDateInfoAsync` to `IAwardRepository`/`IAwardService`/`AwardsController`; `AwardsIndexViewModel` carries `DailyAwardsDate + AwardsNumDays`; view shows "Daily Awards (Monday 12 May)"; onerror tries `/hlstatsimg/award.png` before hiding | 90% → **95%** |
| Server List | `servers.php` (list) | Missing `steam://connect/addr:port` Join link per server row | Added `(Join)` link with `steam://` URL to each server row | 90% → **93%** |

### Phantom Gaps Confirmed

| PHP File | Noted Gap | Outcome |
|---|---|---|
| `playerinfo_general.php` | "Rank-change arrow on profile header; per-server playtime history not confirmed" | No rank-change arrow exists in PHP; no per-server playtime table. Both phantom. |
| `clans.php` | "PHP inline Autocompleter.js omitted" noted as primary gap | Autocomplete is the only intentionally omitted feature; all columns match PHP. |
| `servers.php` (list) | "PHP pings each server for live status" | PHP server list goes straight to detail (no standalone list); the .NET list is an enhancement. Noted gap was inaccurate. Live pinging (A2S_INFO) is a daemon feature deferred separately. |

---

## Changed Since Last Report (2026-05-11 rev 13)

### Player Event History — Two Missing Event Types Fixed

`playerhistory.php` was misidentified in the parity report as "Player Skill History" with a GD chart description — it is actually the **Player Event History** log (kills, deaths, connects, name changes, etc.). The .NET implementation was correct but missing two of the 16 PHP event types:

| Event | PHP Table | Fix | Score Change |
|---|---|---|---|
| Team Bonus | `hlstats_Events_TeamBonuses` | Added `EventTeamBonus` entity + EF config + DbSet; added to `GetEventHistoryAsync` with description "My team received a points bonus of N for triggering X" | |
| Name Change | `hlstats_Events_ChangeName` | Added `EventChangeName` entity (oldName, newName) + EF config + DbSet; added to `GetEventHistoryAsync` with description "I changed my name from X to Y" | |

Combined: `playerhistory.php`: **90% → 97%** (parity report description also corrected)

### Server Creation — Game Defaults Initialised

`AdminRepository.AddServerAsync` now copies `hlstats_Games_Defaults` into `hlstats_Servers_Config` after inserting a new server, and seeds an empty `Mod` row — matching the PHP `newserver.php` and the daemon's `AutoRegisterServerAsync`. Previously new admin-created servers started with empty config requiring a manual Reset.

`admintasks/newserver.php`: **90% → 95%** (mod-selection dropdown and `hlstats_Mods_Defaults` copy omitted — not needed in .NET workflow)

### Admin CRUD Sweep — 15 Pages Confirmed Complete

Full read of every PHP admin CRUD and tool page against the .NET implementation. 15 pages swept from stale 90% to confirmed 95%:

| PHP File | Finding |
|---|---|
| `adminauth.php` | No gaps — login/logout/access denied fully implemented |
| `admintasks/games.php` | No gaps — all fields (code, name, realgame, hidden); full CRUD |
| `admintasks/clantags.php` | No gaps — pattern and position fields; full CRUD |
| `admintasks/hostgroups.php` | No gaps — pattern and name fields; full CRUD |
| `admintasks/actions.php` | No gaps — all fields including player/team points and action type; full CRUD |
| `admintasks/teams.php` | No gaps — code, name, colour, hidden; full CRUD |
| `admintasks/roles.php` | No gaps — code, name, hidden; full CRUD |
| `admintasks/weapons.php` | No gaps — code, name, modifier; full CRUD |
| `admintasks/ranks.php` | No gaps — image, minKills, maxKills, rankName; full CRUD |
| `admintasks/ribbons.php`, `ribbons_trigger.php` | No gaps — all ribbon and trigger fields; full CRUD for both |
| `admintasks/awards_*.php` | No gaps — all award types (W/P/O/V/T) with code/name/verb; full CRUD |
| `admintasks/tools_editdetails_clan.php` | No gaps — clan edit form fields; full CRUD |
| `admintasks/tools_adminevents.php` | No gaps — event log with type filter, pagination |
| `admintasks/tools_reset.php` | No gaps — all reset operations present |
| `admintasks/tools_optimize.php` | No gaps — OPTIMIZE + ANALYZE TABLE |
| `admintasks/tools_settings_copy.php` | No gaps — copy settings between games functional |
| `admintasks/tools_ipstats.php` | No gaps — host group stats with drill-down |

**Note on `admintasks/options.php`:** PHP includes a `MailPath` field (path to sendmail binary). Omitted in .NET — not applicable since .NET uses SMTP, not sendmail. No functional loss.

---

## Changed Since Last Report (2026-05-11 rev 12)

### Gaps Closed

| Feature | PHP Reference | Fix | Score Change |
|---|---|---|---|
| Weapon Leaderboard | `weapons.php` | Confirmed weapon images are already rendered in the Weapon column. Added `onerror` fallback: tries `.png` if `.gif` is missing, then replaces broken icon with bold weapon name — matches PHP `weaponimg` fallback behaviour. Removed hardcoded `width="90" height="17"` so images render at natural size. | 90% → **95%** |
| Player Edit Tools | `admintasks/tools_editdetails_player.php`, `tools_editdetails.php` | Confirmed PHP has no Merge player feature — the gap was phantom. Added player/clan search results to the Edit Details admin page: typing a name now shows matching players and clans with direct Edit links (up to 50 per type, filtered by type selector). | 85% → **95%** |

---

## Changed Since Last Report (2026-05-11 rev 11)

### Gaps Closed from Rev 10 Sweep

| Feature | PHP Reference | Fix | Score Change |
|---|---|---|---|
| Player Chat History | `chathistory.php` | Added `DeleteDays` to `PlayerChatViewModel`; `ChatController.PlayerHistory` now fetches `GetDeleteDaysAsync` in parallel; view header shows "Chat History (Last N Days)" | 95% → **98%** |
| Games List / Landing | `contents.php` | Added `DeleteDays` to `GamesListData`; `GameService.GetGamesListAsync` fetches it; `GamesList.cshtml` shows "Data expires after N days" row in General Statistics | 95% → **98%** |
| Livestats Standalone | `livestats.php` | Added `MapCtWins` / `MapTsWins` to `Server` entity; team footer row now shows "(N wins)" for teams with a non-zero win count | 93% → **97%** |

### New Unit Tests

- `ChatControllerTests.PlayerHistory_PassesDeleteDaysFromService` — theory over 30/90/180 days
- `HomeControllerTests.Index_ShowsGamesList_WhenNoGameAndMultipleGames` — verifies `GamesList` view + `DeleteDays` in model
- `HomeControllerTests.Index_RedirectsToSingleGame_WhenGamesListHasOneEntry` — single-entry list → treat as explicit game

---

## Changed Since Last Report (2026-05-11 rev 10)

### Score Corrections — Parity Sweep of 7 Pages

Read each PHP source file against the .NET implementation; corrected stale 90% scores.

| Feature | PHP Reference | Finding | Score Change |
|---|---|---|---|
| Player Awards History | `playerawards.php` | Fully implemented: all columns, awardId drill-down, sort/pagination. No gaps. | 90% → **98%** |
| Ribbon Detail | `ribboninfo.php` | Fully implemented: ribbon image/name, award threshold filter, sortable player table, back-link. No gaps. | 90% → **97%** |
| Daily Award Detail | `dailyawardinfo.php` | Fully implemented: award image with onerror fallback, verb-suffixed count, sort/pagination. No gaps. | 90% → **97%** |
| Player Chat History | `chathistory.php` | Fully implemented. Minor gap: PHP section header says "Last X Days" (from `DeleteDays` option); .NET omits qualifier. | 90% → **95%** |
| Country Leaderboard | `countryclans.php` | Fully implemented. Activity filter functionally equivalent (`ActivityScore >= 0` = stored activity; matches PHP per-player threshold). No real gaps. | 90% → **95%** |
| Games List / Landing | `contents.php`, `gameslist.php` | Fully implemented. Minor gap: PHP general-stats block says "Event history expires after N days"; .NET omits this line. | 90% → **95%** |
| Livestats Standalone | `livestats.php` | Fully implemented. Minor gap: PHP team footer row shows per-team round wins (`map_ct_wins`/`map_ts_wins`); .NET footer omits this. | 90% → **93%** |

### Also Fixed — Stale Scores in Background Services

- `HLStatsX.NET.Daemon` section header updated from 88% to 91% (auto-reg + SIGHUP were done in rev 8 but the section description wasn't updated).

---

## Changed Since Last Report (2026-05-11 rev 9)

### Newly Completed / Improved

| Feature | PHP Reference | Change |
|---|---|---|
| Country Profile — member location map | `countryclansinfo.php` | Added Leaflet map with player location markers (lat/lng from GeoIP); `GetMemberLocationsAsync` added to repository/service chain |
| Country Profile — Rank + leaderboard rank columns | `countryclansinfo.php` | Added sequential Rank column (first col) and MmRank (player's overall leaderboard rank) to Members table; data was already fetched but not displayed |
| Maps list — Heatmap link | `maps.php` | Added Heatmap column with 🔥 link to Map Detail `#tab-heatmap` for each map |
| Help page — mode-conditional tracking text | `help.php` | Player tracking explanation now reads `Mode` from `hlstats_Options` and shows mode-specific text: NameTrack / LAN / Steam (Normal) — matches PHP conditional blocks |

### Score Impact

- `countryclansinfo.php`: **85% → 95%** — member map + table columns.
- `maps.php`: **90% → 95%** — heatmap link column.
- `help.php`: **85% → 95%** — mode-conditional tracking text.
- Public Stats Pages: **96% → 97%**.

---

## Changed Since Last Report (2026-05-11 rev 8)

### Newly Completed

| Feature | Perl Reference | Change |
|---|---|---|
| Server auto-registration | `hlstats.pl` `addServerToDB` | `AutoRegisterServerAsync` in `DaemonStateManager`: inserts new server row + copies `hlstats_Games_Defaults` config; game code from `Daemon:AutoRegisterGame` appsetting; called when `AllowOnlyConfigServers=false` and sender is unknown |
| SIGHUP config reload | `hlstats.pl` `HUP_handler` | `PosixSignalRegistration` in `DaemonWorker`: flushes all active player sessions, clears in-memory player registries, calls `LoadAsync`; Windows silently skips registration; checked at top of each packet loop iteration via `volatile bool _reloadRequested` |

### Score Impact

- `HLStatsX.NET.Daemon`: **88% → 91%** — two core-engine items promoted to Done.

---

## Changed Since Last Report (2026-05-11 rev 7)

### Newly Completed

| Feature | Perl Reference | Change |
|---|---|---|
| Background DNS Resolver | `hlstats-resolve.pl` | `DnsResolveService` in `HLStatsX.NET.Awards`: reverse DNS lookup for unresolved IPs in `hlstats_Events_Connects`; host group classification via `hlstats_HostGroups` patterns + domain heuristic; `--resolve` CLI flag; `HostGroupClassifier` separately testable |

### Score Impact

- `HLStatsX.NET.Awards` confirmed at 100% — DNS resolver is the final piece of `hlstats-resolve.pl`.
- Daemon parity score unchanged (DNS pass was attributed to Awards in prior score).

---

## Changed Since Last Report (2026-05-11)

### Newly Completed

| Feature | PHP Reference | Change |
|---|---|---|
| In-Game Pages — all 19 functional pages | `ingame/` (21 files, 2 are layout partials) | InGameController + 19 views: motd, players, clans, claninfo, statsme, kills, weapons, accuracy, targets, maps, servers, status, bans, help, weaponinfo, mapinfo, actions, actioninfo, load |
| Weapon Detail — DeleteDays qualifier | `weaponinfo.php` | "Last N Days" now shown in summary header (from `DeleteDays` option) |
| Map Detail — DeleteDays qualifier | `mapinfo.php` | "Last N Days" now shown in summary header |
| Player Sessions — comprehensive implementation | `playersessions.php` | Full 12-column table, skill change arrows (▲/▼), DeleteDays footer, sortable, paginated |
| Actions Leaderboard + Detail — full parity | `actions.php`, `actioninfo.php` | Achievers + Victims tables with DeleteDays, both PlayerActions and PlayerPlayerActions event types, sortable, paginated |
| Roles Leaderboard + Detail — full parity | `roles.php`, `rolesinfo.php` | Role images, all percentage columns with meter bars, DeleteDays, sortable, paginated |
| Admin User Management — full parity | `admintasks/adminusers.php` | AccLevel dropdown (Administrator/Restricted/No Access matching PHP 0/80/100 values); access level help text; password encryption note; edit form confirmed present |
| Admin Dashboard — full parity | `admin.php` | 5 stat cards, General Settings, per-game cards with all settings links (Servers/Actions/Teams/Roles/Weapons/Ranks/Ribbons + all 4 award types with ?game= pre-filled), Tools section with 9 tools + descriptions, Servers summary table |
| VoiceComm Server Management | `admintasks/voicecomm.php` | Full CRUD for `hlstats_Servers_VoiceComm`: list (type/name/address/ports), create, edit, delete; serverType select (TeamSpeak/Ventrilo) |
| Daemon Control | `admintasks/tools_perlcontrol.php` | Send RELOAD/KILL UDP packets to Perl or .NET daemon; configurable host/port; 5-second receive timeout; displays bytes sent, packets received, response |
| Reset DB Collations | `admintasks/tools_resetdbcollations.php` | Run-on-DB and print-SQL modes; converts all tables to utf8mb4/utf8mb4_unicode_ci via raw ADO.NET ALTER TABLE |
| Server Detail — Historical Load Graphs | `servers.php` detail | Range tab buttons (24h / 1 Week / 1 Month / 1 Year); AJAX fetch → Chart.js canvas update; down-sampling matches PHP `show_graph.php` `avg_step` constants |
| Game Dashboard — Per-server chart range switching | `game.php` | 24h / 1 Week / 1 Month / 1 Year buttons per server; each server's buttons scoped independently; reuses `/Servers/LoadChart` endpoint |
| Parity correction — chat.php | `chat.php` | "Top chatters leaderboard" phantom gap removed — PHP source has no such section; confirmed against live site and `chat.php` source |
| Parity correction — claninfo.php | `claninfo.php` | "`tag_style` colour" phantom gap removed — `tag_style` column does not exist in PHP source, DB schema, or live site output |

### Score Impact

- `weaponinfo.php`: **85% → 95%** — DeleteDays qualifier matches PHP summary header.
- `mapinfo.php`: **85% → 95%** — DeleteDays qualifier + overhead map image + interactive Canvas heatmap (exceeds PHP).
- `playersessions.php`: **90% → 95%** — verified 12-column parity, skill change arrows, DeleteDays footer.
- `actions.php` + `actioninfo.php`: **90% → 95%** — comprehensive implementation with both event table types verified.
- `roles.php` + `rolesinfo.php`: **90% → 95%** — all columns including meter bars, role images verified.
- In-Game Pages: **0% → 90%** — 19 new pages implemented (previously Not Started).
- `admintasks/adminusers.php`: **70% → 95%** — edit form was already implemented; parity report was stale; polished AccLevel to PHP dropdown values.
- `admin.php` (dashboard): **65% → 95%** — full rewrite with stat cards, per-game settings cards, tools with descriptions, servers table.
- `admintasks/voicecomm.php`: **0% → 95%** — new; full CRUD for VoiceComm servers.
- `admintasks/tools_perlcontrol.php`: **0% → 95%** — new; UDP control packets to Perl and .NET daemons.
- `admintasks/tools_resetdbcollations.php`: **0% → 95%** — new; ALTER TABLE collation converter.
- Public Stats Pages: **94% → 95%** — improvements outweigh the lower ingame average being folded in.
- Admin Panel: **88% → 98%** — four new features: dashboard, VoiceComm, Daemon Control, Reset Collations.
- `servers.php` (Server Detail): **85% → 95%** — historical load graphs with 4 time ranges (24h / 1 week / 1 month / 1 year), PHP-parity down-sampling.
- `game.php` (Game Dashboard): **92% → 95%** — per-server chart range switching added.
- `chat.php`: **90% → 95%** — phantom gap corrected; PHP has no top chatters section.
- `claninfo.php`: **90% → 95%** — phantom gap corrected; `tag_style` does not exist in PHP.
- Public Stats Pages: **95% → 96%** — chat and clan profile corrections.
- **Overall**: **96% → 97%**.

---

## Full Feature-by-Feature Status

### Public Stats Pages

| PHP File(s) | Feature | Verified Sections | Gaps | Status |
|---|---|---|---|---|
| `players.php` | Player Rankings | Leaderboard table, sortable columns, pagination, rank types (total/week/month/date), minKills filter, activity bars, country flags | PHP has inline Autocompleter.js live search (omitted); .NET uses Search page | Done (~95%) |
| `playerinfo.php` + sub-files | Player Profile | General tab: stats, flags, clan, connection time, aliases, ribbons, rank, sig; Teams & Actions; Weapons; Maps & Servers; Kill Stats; Trend chart; Forum sig link; Steam avatar | Rank-change arrow on profile header; per-server playtime history in General tab not confirmed | Done (~95%) |
| `playerhistory.php` | Player Event History | All 16 event types: Connect, Disconnect, Entry, Kill, Kill (HS), Death, Team Kill, Friendly Fire, Suicide, Role, Team, Action, Action+ (initiator), Action- (victim), Team Bonus, Name Change; sortable; paginated; "Last N Days" header; back link | None identified | Done (~97%) |
| `bans.php` | Banned Players | Full 10-column table: Player (flag + link), Ban Date, Skill, Activity bar, Kills, Deaths, HS, K:D, HS:K, Accuracy; sortable; paginated | None identified | Done (~95%) |
| `playersessions.php` | Player Sessions | Full 12-column table: Date, Skill Change (▲/▼ arrow), Points, Time (Xd HH:MM:SSh), Kills, Deaths, K:D, HS, HS:K, Suicides, TKs, Kill Streak; sortable; paginated; DeleteDays footer; back link | None identified | Done (~95%) |
| `playerawards.php` | Player Awards | Award history table with award name/date/count columns; awardId drill-down mode shows per-day occurrences; sort, pagination | None identified | Done (~98%) |
| `clans.php` | Clan Rankings | Clan table, sortable, paginated, activity bars, country flags | PHP inline Autocompleter.js omitted | Done (~90%) |
| `claninfo.php` + sub-files | Clan Profile | Members (paginated), Weapons, Map Performance, Actions, Teams, Roles, Leaflet member map | None identified — `tag_style` does not exist in PHP source or DB schema | Done (~95%) |
| `weapons.php` | Weapon Rankings | Weapon table, kill count, HS%, sortable, paginated; per-row weapon image with `.png` fallback then bold-name fallback | None identified | Done (~95%) |
| `weaponinfo.php` | Weapon Detail | Player leaderboard (Player, kills, headshots, HpK), weapon image, total kills/headshots, "Last N Days" qualifier, back link; sortable; paginated | None identified | Done (~95%) |
| `maps.php` | Map Rankings | Map table, kill count, player count, sortable, paginated; Heatmap link column | None identified | Done (~95%) |
| `mapinfo.php` | Map Detail | Player leaderboard (Player, kills, headshots, HpK), "Last N Days" qualifier, overhead map image, interactive Canvas heatmap (Kills/Deaths tabs + zoom/pan); back link; sortable; paginated | Map download link absent (optional `map_dlurl` option) | Done (~95%) |
| `servers.php` | Server List | Server table with address, map, players, kills, HS; global stats header; load chart | PHP pings each server for live status; .NET uses DB flag | Done (~90%) |
| `servers.php` (detail) | Server Detail | Live players table (Livestats), daily awards, per-server load chart with 4 time ranges (24h / 1 week / 1 month / 1 year), team breakdown | None identified | Done (~95%) |
| `livestats.php` | Livestats Standalone | Standalone live players page; server dropdown; sortable table; auto-refresh; team footer shows round wins | None identified | Done (~97%) |
| `awards.php` + tabs | Awards — all tabs | Daily Awards, Global Awards, Ranks, Ribbons tabs; links to detail pages | Award type icons on list absent; column count not option-driven | Done (~90%) |
| `awards_ranks.php` | Ranks Listing | Grid layout, player counts, links to RankDetail | Column count fixed (not from `awardrankscols` option) | Done (~95%) |
| `awards_ribbons.php` | Ribbons Listing | Class grouping, player counts, links to RibbonDetail | Column count fixed (not from `awardribbonscols` option) | Done (~95%) |
| `rankinfo.php` | Rank Detail | Paginated player table, sortable, back-link | None identified | Done (~95%) |
| `ribboninfo.php` | Ribbon Detail | Ribbon image/name header, paginated sortable player table (rank, flag, player link, daily-award count, award name), award threshold filter, back-link | None identified | Done (~97%) |
| `dailyawardinfo.php` | Daily Award Detail | Award history with date, winner (flag + link), verb-suffixed count; award image with onerror fallback; sort, pagination | None identified | Done (~97%) |
| `search.php` | Search | Player/clan search; paginated results; matched alias display | PHP shows real-time Autocompleter.js suggestions — .NET is plain form submit | Done (~90%) |
| `actions.php` | Actions Leaderboard | Total earned header, action/earned/reward table, sortable, paginated, back link | None identified | Done (~95%) |
| `actioninfo.php` | Action Detail | Achievers table (DeleteDays, player/count/skill bonus), Victims table for PlayerPlayerActions (separate sortable+paginated); sortable; paginated | TeamBonuses fallback edge case omitted | Done (~95%) |
| `roles.php` | Roles Leaderboard | Role images, Picked/%/ratio (meter), Kills/%/ratio (meter), Deaths/%/ratio (meter), K:D; all sortable; back link | None identified | Done (~95%) |
| `rolesinfo.php` | Role Detail | Role image, total kills/headshots, "Last N Days", player kill table; sortable; paginated; back link | None identified | Done (~95%) |
| `chat.php` | Chat Log | Server chat log with server dropdown, text filter, sortable table, pagination; team/squad message prefix | None identified — PHP chat.php has no top chatters section | Done (~95%) |
| `chathistory.php` | Player Chat History | Per-player chat history, text filter, sortable columns, pagination, team/squad prefix, back-link; "Last N Days" header qualifier | None identified | Done (~98%) |
| `countryclans.php` | Country Leaderboard | Country table, all columns (Avg Points, Members, Activity bar, Connection Time, Kills, Deaths, K:D), `minMembers` filter, activity filter, sortable, paginated | None identified | Done (~95%) |
| `countryclansinfo.php` | Country Profile | Member list (paginated); country stats; Leaflet member map; Rank + MmRank columns | None identified | Done (~95%) |
| `contents.php`, `gameslist.php` | Games List / Landing | Game cards with icon, name, stats; top player + top clan links; general stats (players/clans/servers/kills/last kill/event history expiry); auto-selects single game; game tab bar in nav | None identified | Done (~98%) |
| `help.php` | Help Page | Weapons reference table, game actions reference table; mode-conditional tracking text (NameTrack/LAN/Steam) | None identified | Done (~95%) |
| `game.php` | Game Dashboard | Participating servers table; global stats header; aggregate trend chart; per-server load charts (24h/week/month/year); VoiceComm block; Leaflet player location map; Livestats live players table; Daily Awards | Google Maps replaced with Leaflet/OSM | Done (~95%) |
| `playerinfo.php` (sig action) | Player Forum Signature PNG | SkiaSharp PNG, 11 backgrounds, per-background colour logic, rank + kill overlay | Minor gradient rendering vs PHP GD | Done (~95%) |

### In-Game Pages

| PHP File | .NET Endpoint | Feature | Status |
|---|---|---|---|
| `ingame/motd.php` | `GET /ingame/motd` | Top players, top clans, server list — configurable counts | Done (~90%) |
| `ingame/players.php` | `GET /ingame/players` | Player rank list; sortable; paginated | Done (~90%) |
| `ingame/kills.php` | `GET /ingame/kills` | Per-player kill stats (top victims, headshots); configurable limit | Done (~90%) |
| `ingame/maps.php` | `GET /ingame/maps` | Map performance for a player | Done (~90%) |
| `ingame/weapons.php` | `GET /ingame/weapons` | Weapon stats for a player | Done (~90%) |
| `ingame/clans.php` | `GET /ingame/clans` | Clan rank list; sortable; paginated | Done (~90%) |
| `ingame/claninfo.php` | `GET /ingame/claninfo` | Clan detail with member list | Done (~90%) |
| `ingame/accuracy.php` | `GET /ingame/accuracy` | Weapon accuracy table for a player (shots/hits from statsme) | Done (~90%) |
| `ingame/targets.php` | `GET /ingame/targets` | Kill target list for a player | Done (~90%) |
| `ingame/statsme.php` | `GET /ingame/statsme` | Player stats summary (skill, rank, accuracy, kill totals) | Done (~90%) |
| `ingame/status.php` | `GET /ingame/status` | Server status (live players, total kills, total players) | Done (~90%) |
| `ingame/bans.php` | `GET /ingame/bans` | Banned players list | Done (~90%) |
| `ingame/help.php` | `GET /ingame/help` | Help/reference page with server list | Done (~90%) |
| `ingame/weaponinfo.php` | `GET /ingame/weaponinfo` | Weapon detail — player kill leaderboard by weapon | Done (~90%) |
| `ingame/mapinfo.php` | `GET /ingame/mapinfo` | Map detail — player leaderboard by map | Done (~90%) |
| `ingame/actions.php` | `GET /ingame/actions` | Actions leaderboard | Done (~90%) |
| `ingame/actioninfo.php` | `GET /ingame/actioninfo` | Action detail — achievers list | Done (~90%) |
| `ingame/servers.php` | `GET /ingame/servers` | Server list | Done (~90%) |
| `ingame/load.php` | `GET /ingame/load` | Server load / global stats | Done (~90%) |
| `ingame/footer.php` | Layout partial | Layout partial — handled by `_InGameLayout.cshtml` | N/A |
| `ingame/header.php` | Layout partial | Layout partial — handled by `_InGameLayout.cshtml` | N/A |

### Admin Panel

| PHP File(s) | Feature | Status |
|---|---|---|
| `adminauth.php` | Login / Logout / Access Denied | Done (~95%) |
| `admin.php` | Admin Dashboard | Done (~95%) — stat cards, General Settings, per-game cards with all settings links, Tools with descriptions |
| `admintasks/adminusers.php` | User Management | Done (~95%) — list, create, edit (password + AccLevel dropdown), delete; all matching PHP parity |
| `admintasks/options.php` | Site Options | Done (~95%) — MailPath omitted (PHP sendmail path; not applicable in .NET) |
| `admintasks/games.php` | Games CRUD | Done (~95%) |
| `admintasks/newserver.php`, `serversettings.php` | Servers CRUD + Settings | Done (~95%) — game defaults now copied on server create; mod-selection dropdown omitted (not needed) |
| `admintasks/clantags.php` | Clan Tags CRUD | Done (~95%) |
| `admintasks/hostgroups.php` | Host Groups CRUD | Done (~95%) |
| `admintasks/actions.php` | Actions CRUD | Done (~95%) |
| `admintasks/teams.php` | Teams CRUD | Done (~95%) |
| `admintasks/roles.php` | Roles CRUD | Done (~95%) |
| `admintasks/weapons.php` | Weapons CRUD | Done (~95%) |
| `admintasks/ranks.php` | Ranks CRUD | Done (~95%) |
| `admintasks/ribbons.php`, `ribbons_trigger.php` | Ribbons + Triggers CRUD | Done (~95%) |
| `admintasks/awards_*.php` | Awards CRUD (all types) | Done (~95%) |
| `admintasks/tools_editdetails_player.php` | Player Edit Tools | Done (~95%) — Merge player confirmed absent from PHP; Edit Details search results added |
| `admintasks/tools_editdetails_clan.php` | Clan Edit Tools | Done (~95%) |
| `admintasks/tools_adminevents.php` | Admin Events Log | Done (~95%) |
| `admintasks/tools_reset.php` | DB Reset | Done (~95%) |
| `admintasks/tools_optimize.php` | DB Optimize | Done (~95%) |
| `admintasks/tools_settings_copy.php` | Copy Game Settings | Done (~95%) |
| `admintasks/tools_ipstats.php` | IP Stats | Done (~95%) |
| `admintasks/voicecomm.php` | VoiceComm Server Management | Done (~95%) — full CRUD for hlstats_Servers_VoiceComm; list, create, edit, delete |
| `admintasks/tools_perlcontrol.php` | Daemon Control | Done (~95%) — form to send RELOAD/KILL UDP packets; works with Perl daemon and .NET daemon |
| `admintasks/tools_resetdbcollations.php` | Reset DB Collations | Done (~95%) — run-on-DB and print-SQL modes; converts all tables to utf8mb4_unicode_ci |
| `admintasks/tools_synchronize.php` | VAC Master Sync | **N/A — Obsolete** (master servers defunct) |

### Legacy Voice — Not Started / Low Priority

| PHP File(s) | Notes |
|---|---|
| `teamspeak.php`, `teamspeak_class.php`, `teamspeak_query.php` | Connected Teamspeak channel/user listing. Legacy platform — most communities use Discord. |
| `ventrilo.php`, `ventrilostatus.php` | Ventrilo connected users. Legacy platform. |

---

## Background Services

### HLStatsX.NET.Awards — 100%

Replaces `hlstats-awards.pl` and `hlstats-resolve.pl`.

| Perl Sub | .NET Service | Status |
|---|---|---|
| `DoInactive` | `PlayerActivityService` | Done |
| `DoAwards` | `AwardsCalculationService` | Done |
| `DoRibbons` | `RibbonsService` | Done |
| `DoGeoIP` | `GeoIpService` (MaxMind GeoLite2-City) | Done |
| `DoClans` | `ClansService` + `ClanPatternMatcher` | Done |
| `DoPruning` | `PruningService` (all 20 event tables) | Done |
| `DoOptimize` | `OptimizeService` | Done |
| `hlstats-resolve.pl` | `DnsResolveService` + `HostGroupClassifier` | Done |
| Scheduled daily run | `AwardsWorker` (configurable `RunAt`) | Done |
| On-demand CLI | `--run-now [--inactive\|--awards\|--ribbons\|--prune\|--clans\|--optimize\|--geoip\|--resolve\|--all]` | Done |

### HLStatsX.NET.Daemon — 96%

Replaces `hlstats.pl` and `HLstats_EventHandlers.plib`.

**Core Engine (~98%):** UDP listener, STDIN import mode, log line / player string / properties parsers, SteamID normalisation, bot detection, multi-server player registry, server + global config loading from DB, event queue with batch INSERT, graceful shutdown, A2S_INFO server query (map fallback).

**Not done in core:** proxy daemon protocol, proxy-daemon.pl relay — deferred.

**Event Handlers (18/22 done):** Connect, Enter Game, Disconnect, Team/Role/Name change, Kill (Frag + TK branch), Suicide, Player Action, Player-Player Action, Team Bonus, World triggered, Map change, Statsme, Chat. All 18 now include RCON broadcast where applicable.

**Deferred event handlers:** RCON events, Admin events, Statsme2, Latency tracking.

**Skill & State (~97%):** All 6 skill modes (0–5) + L4D variant, kill streak tracking, clan tag matching + auto-create, GeoIP on connect, Livestat lifecycle, trend tracking, bonus round detection.

**RCON subsystem (~75%):** `SourceRconClient` (TCP, Source protocol), `GoldSrcRconClient` (UDP, GoldSrc), `ServerBroadcastService` (connection pool, engine selection, message sanitisation). Kill/TK, connect announce, rank kick, and action/team-bonus broadcasts all wired. Still deferred: global chat relay, auto-team balance, next-rank display, global ban enforcement.

---

## Priority Queue

_Empty — no known gaps remaining in implemented features._

---

## What's Working Well

- **In-game pages** — All 19 functional in-game pages are now live at `/ingame/*`. Lightweight no-chrome HTML for the Half-Life in-game MOTD browser: player ranks, clan ranks, weapon stats, map stats, accuracy, kill targets, statsme, server status, bans, actions, and more.
- **Game dashboard** — Feature-complete: VoiceComm block, per-server load charts, Leaflet player map, Livestats, Daily Awards all present.
- **Player profile** — 26+ concurrent DB queries across 5 tabs; matches PHP original comprehensively.
- **Clan profile** — All tabs: Members (paginated), Weapons, Maps, Actions, Teams, Roles, and Leaflet member location widget.
- **Admin panel** — 98% complete: full CRUD for all entity types (including user management with password + access level editing), site options, player/clan tools, events log, DB reset/optimize/copy, cleanup, IP Stats, VoiceComm server management, daemon control, and reset DB collations. Only the obsolete VAC sync and legacy TeamSpeak/Ventrilo pages remain.
- **Awards background service** — Feature-complete (100%). Scheduled runs + full `--run-now` CLI flag set.
- **Daemon** — Production-capable: 18/22 game event handlers done, all 6 skill modes verified, clan tag matching, GeoIP, Livestat lifecycle, trend tracking. Full RCON subsystem (Source + GoldSrc protocols, broadcasting wired into all handlers). 641 unit tests.
- **Consistent patterns** — Pagination, sortable columns, and game parameter propagation are consistent across all 50+ implemented pages.
- **Interactive heatmap** — Canvas-based kill/death heatmap on Map Detail exceeds the PHP original.
