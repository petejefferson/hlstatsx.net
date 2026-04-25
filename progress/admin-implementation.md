# Admin Panel — Implementation State
_Last updated: 2026-04-25_

This file tracks every admin task from the PHP original against its .NET implementation status.
Update status and notes as each task is completed.

---

## Status Key
- ✅ Done — fully implemented, matches PHP behaviour
- ⚠️ Partial — exists but missing features
- ❌ Not started
- 🚫 Skip — not applicable to .NET rewrite

---

## Admin Shell

| Item | Status | Notes |
|---|---|---|
| Admin area layout / sidebar nav | ✅ | `_AdminLayout.cshtml` — full sidebar with all sections, game selector dropdown |
| Auto-login bypass (hardcoded pete / acclevel 100) | ✅ | `AdminAutoLoginMiddleware` — auto-signs in "pete" for all `/Admin/*` requests |
| Login / logout | ✅ | Cookie auth with MD5 hash (kept for when bypass is removed) |
| Dashboard with stat cards + section quick-links | ✅ | Shows player/clan/server counts + nav cards to all admin sections |
| Access denied page | ✅ | |

---

## Section: General Settings

| PHP File | Feature | Status | Notes |
|---|---|---|---|
| `admintasks/options.php` | Site options editor | ✅ | All 10 option groups: Site, GeoIP, Awards, Ranking, Visual, Daemon, Point Calc, Paths, Proxy, Hit Counter |
| `admintasks/adminusers.php` | Admin user CRUD | ✅ | List, create, edit (password + acclevel), delete |
| `admintasks/games.php` | Games CRUD | ✅ | Add/edit/delete with cascade delete of all game data |
| `admintasks/clantags.php` | Clan tag CRUD | ✅ | Pattern + position (EITHER/START/END) |
| `admintasks/hostgroups.php` | Host group CRUD | ✅ | Pattern + group name |
| `admintasks/voicecomm.php` | Voice server CRUD | 🚫 | Legacy Teamspeak/Ventrilo — skipped |

---

## Section: Game Settings

| PHP File | Feature | Status | Notes |
|---|---|---|---|
| `admintasks/servers.php` | Server list + delete | ✅ | |
| `admintasks/newserver.php` | Create new server | ✅ | Fields: game, name, address, port, public address, rcon, sort order |
| `admintasks/serversettings.php` | Edit server + per-server config | ✅ | Inline config param table; copy-from-server; reset-to-defaults |
| `admintasks/actions.php` | Game actions CRUD | ✅ | All fields incl. all 4 action type flags, team, rewards |
| `admintasks/teams.php` | Teams CRUD | ✅ | Code, name, text/bg colour, hidden |
| `admintasks/roles.php` | Roles CRUD | ✅ | Code, name, hidden |
| `admintasks/weapons.php` | Weapons CRUD | ✅ | Code, name, points modifier |
| `admintasks/ranks.php` | Ranks CRUD | ✅ | Name, min/max kills, image |
| `admintasks/ribbons.php` | Ribbons CRUD | ✅ | Name, image, award code, award count, special |
| `admintasks/ribbons_trigger.php` | Ribbon triggers CRUD | ✅ | Ribbon dropdown, award code, count, type |
| `admintasks/awards_weapons.php` | Weapon-based awards | ✅ | type='W' — weapon/special code dropdown, name, verb |
| `admintasks/awards_plyractions.php` | Player action awards | ✅ | type='O' |
| `admintasks/awards_plyrplyractions.php` | Player-vs-player awards | ✅ | type='P' |
| `admintasks/awards_plyrplyractions_victim.php` | Victim awards | ✅ | type='V' |

---

## Section: Tools

| PHP File | Feature | Status | Notes |
|---|---|---|---|
| `admintasks/tools_editdetails.php` | Player/clan search for edit | ✅ | Search by name or numeric ID, type filter |
| `admintasks/tools_editdetails_player.php` | Edit player details | ✅ | All fields: name, email, homepage, flag, skill, kills/deaths/HS/suicides, ranking status, avatar block; IP address list |
| `admintasks/tools_editdetails_clan.php` | Edit clan details | ✅ | Name, tag, homepage, map region, hidden flag |
| `admintasks/tools_adminevents.php` | Admin event log viewer | ✅ | Paginated, filterable by type (Rcon/Admin); queries Events_Rcon + Events_Admin |
| `admintasks/tools_optimize.php` | DB optimize/analyze | ✅ | Runs OPTIMIZE + ANALYZE TABLE on all HLStatsX tables |
| `admintasks/tools_reset.php` | Stats reset | ✅ | All checkboxes: awards, history, skill, counts, map data, bans, events, delete players; game filter |
| `admintasks/tools_reset_2.php` | Clean up inactive players | ✅ | Delete players below kill threshold + empty clans |
| `admintasks/tools_settings_copy.php` | Copy game settings to another game | ✅ | Copies actions/teams/roles/weapons/ranks/ribbons/awards |
| `admintasks/tools_ipstats.php` | IP/ISP statistics | ❌ | Not yet implemented |
| `admintasks/tools_perlcontrol.php` | Perl daemon control | 🚫 | Not applicable to .NET rewrite |
| `admintasks/tools_resetdbcollations.php` | Fix DB collations | 🚫 | Legacy upgrade tool — skipped |
| `admintasks/tools_synchronize.php` | External ban list sync | 🚫 | Commented out in PHP — skipped |

---

## Remaining Work

| Item | Priority | Notes |
|---|---|---|
| IP/ISP statistics page | Low | `tools_ipstats.php` — shows breakdown by host/ISP |
| Options `@functions` pattern | Done | Rewrote to avoid async issues |
| Server edit — preserve `game` field | Verify | `EditServer` POST currently doesn't update game; needs check |

---

## Architecture Notes

- All admin routes under `/Admin/...`
- `AdminAutoLoginMiddleware` auto-signs in "pete" (acclevel 100) — remove for production
- Game context via `?game=xxx` query parameter; sidebar dropdown populates from `/Admin/GamesList` JSON
- All CRUD in `AdminController` + `IAdminService` → `AdminService` → `IAdminRepository` → `AdminRepository`
- `_AdminLayout.cshtml` provides sidebar nav with all sections
- New entities: `HostGroup`, `GameSupported` (with EF Core configurations and DbContext registration)
- Added `GameAction.ForTeamActions` + `GameAction.ForWorldActions` properties
- Added `Player.BlockAvatar` property
