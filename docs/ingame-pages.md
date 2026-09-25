# In-Game Pages

Lightweight, no-chrome HTML pages designed to be displayed in the Half-Life engine's MOTD browser window. They share the same database and CSS as the main site but use a minimal `_InGameLayout` with no navigation bar or game selector — just a slim header and a "Go Back" link.

All routes are under `/ingame/`.

---

## Browsing directly

Any page can be opened in a regular browser for development or testing:

```
https://your-site/ingame/motd?game=dods
https://your-site/ingame/players?game=dods
https://your-site/ingame/statsme?game=dods&player=1234
```

The `game` parameter defaults to `HLStatsX:DefaultGame` from `appsettings.json` if omitted.

---

## Page reference

### Server / global pages

| Route | Parameters | Description |
|---|---|---|
| `/ingame/motd` | `game`, `players` (default 10), `clans` (default 3) | MOTD shown on server join: top players, top clans, server list |
| `/ingame/players` | `game`, `sortBy`, `desc`, `page` | Top-25 player leaderboard (paginated) |
| `/ingame/clans` | `game`, `sortBy`, `desc`, `page`, `minMembers` (default 3) | Clan rankings |
| `/ingame/servers` | `game` | All participating servers |
| `/ingame/status` | `game`, `server_id` | Live status for one server (defaults to first server) |
| `/ingame/load` | `game`, `server_id` | Aggregate stats across all servers |
| `/ingame/bans` | `game`, `sortBy`, `desc`, `page` | Banned players |
| `/ingame/help` | `game`, `server_id` | In-game command reference + server list |
| `/ingame/actions` | `game`, `sortBy`, `desc` | Server action/event leaderboard |

### Player-specific pages

These require identifying the player. Pass either `player` (numeric DB ID) or `uniqueid` (Steam ID — `STEAM_0:Y:Z` or `STEAM_1:Y:Z` format; normalised automatically).

| Route | Additional parameters | Description |
|---|---|---|
| `/ingame/statsme` | — | Player summary: rank, kills, K:D, accuracy |
| `/ingame/kills` | `killLimit` (default 5) | Top victims and headshot count |
| `/ingame/weapons` | — | Weapon usage breakdown |
| `/ingame/accuracy` | — | Per-weapon shots/hits/accuracy (Statsme data) |
| `/ingame/targets` | — | Hit-position distribution by target player |
| `/ingame/maps` | — | Map performance breakdown |

### Detail drilldowns

| Route | Required parameter | Description |
|---|---|---|
| `/ingame/claninfo` | `clan` (clan ID) | Clan profile with member list |
| `/ingame/weaponinfo` | `weapon` (weapon code, e.g. `awp`) | Top killers with that weapon |
| `/ingame/mapinfo` | `map` (map name, e.g. `dod_anzio`) | Top players on that map |
| `/ingame/actioninfo` | `action` (action code) | Top achievers for one action |

---

## Daemon integration

When the daemon receives an in-game chat command (e.g. `statsme`, `top20`, `clans`) it builds a URL and pushes it to the player's MOTD window via RCON `say` or a Source `cl_showmotd` command.

The base URL is read from the `HLStatsURL` key in `hlstats_Servers_Config` for the relevant server. Set it to the root of your .NET site:

```sql
UPDATE hlstats_Servers_Config
SET value = 'https://your-site/stats'
WHERE keyname = 'HLStatsURL' AND serverId = 1;
```

The daemon appends the path directly:

| Chat command | URL sent to player |
|---|---|
| `statsme` | `{HLStatsURL}/ingame/statsme?game=dods&player={id}` |
| `top20` / `top10` / `top5` | `{HLStatsURL}/ingame/players?game=dods` |
| `clans` | `{HLStatsURL}/ingame/clans?game=dods` |
| `cheaters` / `bans` | `{HLStatsURL}/ingame/bans?game=dods` |
| `status` | `{HLStatsURL}/ingame/status?game=dods&server_id={id}` |
| `load` | `{HLStatsURL}/ingame/load?game=dods&server_id={id}` |
| `servers` | `{HLStatsURL}/ingame/servers?game=dods` |
| `accuracy` | `{HLStatsURL}/ingame/accuracy?game=dods&player={id}` |
| `targets` | `{HLStatsURL}/ingame/targets?game=dods&player={id}` |
| `kills` | `{HLStatsURL}/ingame/kills?game=dods&player={id}` |
| `actions` | `{HLStatsURL}/ingame/actions?game=dods&player={id}` |
| `weapons` | `{HLStatsURL}/ingame/weapons?game=dods&player={id}` |
| `help` | `{HLStatsURL}/ingame/help?game=dods` |

> **Note:** RCON broadcasting is not yet implemented in `HLStatsX.NET.Daemon`. The pages are fully functional and can be browsed directly, but the daemon will not push URLs to players until RCON support is added.

### PHP URL mapping

The PHP site used `ingame.php?mode=X`. The .NET equivalent is `/ingame/X`:

| PHP | .NET |
|---|---|
| `ingame.php?mode=statsme&player=N` | `/ingame/statsme?player=N` |
| `ingame.php?mode=status&server_id=N` | `/ingame/status?server_id=N` |
| `ingame.php?mode=players` | `/ingame/players` |
| *(and so on for all modes)* | |

---

## Layout

All in-game views use `_InGameLayout.cshtml` instead of `_Layout.cshtml`. The layout adds:

- `Cache-Control: no-cache` meta tag (important — prevents stale stats in the MOTD window)
- A 36 px dark header bar with a "Go Back" link (`javascript:history.back()`)
- No navigation, no game selector, no footer

Do not use `asp-layout` or `Layout = "_Layout"` in any in-game view — they must stay on `_InGameLayout`.
