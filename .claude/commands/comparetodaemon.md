# Compare to Perl Daemon

Perform a comprehensive feature parity check between the original Perl daemon (`legacy/perl/scripts/`) and the .NET rewrite (`src/HLStatsX.NET.Daemon/`).

## Steps

### 1. Read the existing status baseline

Read the "Daemon Reference — Feature Map" table from `CLAUDE.md`. Treat every "Done" entry as **unverified until proven** by the steps below.

### 2. Build the Perl feature inventory

Read the following Perl source files in full and extract every named subroutine and major subsystem:
- `legacy/perl/scripts/hlstats.pl` — core loop, config, server management
- `legacy/perl/scripts/HLstats_EventHandlers.plib` — all `doEvent_*` subroutines
- `legacy/perl/scripts/HLstats.plib` — shared helpers, skill calc, clan detection, trend
- `legacy/perl/scripts/HLstats_Player.pm` — player session object
- `legacy/perl/scripts/HLstats_Server.pm` — server state + RCON dispatch
- `legacy/perl/scripts/HLstats_Game.pm` — game object
- `legacy/perl/scripts/TRcon.pm` — Source RCON protocol
- `legacy/perl/scripts/BASTARDrcon.pm` — GoldSrc RCON protocol
- `legacy/perl/scripts/proxy-daemon.pl` — UDP relay

Cross-reference with the baseline table to catch anything not yet in the inventory.

### 3. Deep-verify each feature — read both sides

For **every** feature in the inventory:

a. **Read the Perl source** — identify the subroutine(s) responsible, key DB writes, in-memory state updates, and any configurable behaviour (skill mode, ignore_bots, bonus round, etc.)

b. **Read the .NET source** — open the corresponding class(es) in `src/HLStatsX.NET.Daemon/`. Check:
   - Does a handler/class exist for this event or subsystem?
   - Does it replicate all the DB writes the Perl code performs?
   - Are configurable options wired through from `appsettings.json` + DB options?

c. **List verified behaviours and missing behaviours** before assigning a status.

### 4. Classify each feature

- ✅ **Fully implemented** — every Perl behaviour present in .NET, all DB writes accounted for
- ⚠️ **Partially implemented** — handler exists but one or more DB writes or config paths are missing. Name the specific gaps.
- ❌ **Not yet implemented** — no corresponding .NET class or method

### 5. Assess event handler completeness

List every `doEvent_*` subroutine from `HLstats_EventHandlers.plib` and its .NET status. For each:
- What DB tables does the Perl code write?
- Does the .NET handler write to all the same tables?
- Are skill points calculated correctly (correct mode, min/max caps, weapon modifier)?

### 6. Assess the core engine

Check the following subsystems separately from event handlers:

| Subsystem | Perl implementation | .NET status |
|---|---|---|
| UDP listener | `IO::Socket::INET` + `IO::Select` 2s timeout | ? |
| STDIN import | `--stdin` flag, `<STDIN>` loop | ? |
| Log line parser | Regex: `MM/DD/YYYY - HH:MM:SS: <text>` | ? |
| Player string parser | `getPlayerInfo` (`Name<uid><steamid><team>`) | ? |
| Properties parser | `getProperties` (`(key "value") ...`) | ? |
| SteamID normaliser | `[U:1:N]` → `STEAM_0:Y:Z` and strip `STEAM_X:` | ? |
| Bot detector | `botidcheck` (`BOT`/`0`/`00000000:N:0`) | ? |
| Server state | `%g_servers` hash of `HLstats_Server` objects | ? |
| Player state | `%g_players` per server, `HLstats_Player` objects | ? |
| Config loader | `readDatabaseConfig()` — both `hlstats_Options` and per-server `hlstats_Servers_Config` | ? |
| Event queue | `recordEvent` → `flushEventTable` (batch INSERT) | ? |
| Skill engine | `calcSkill` (modes 0–5), `calcL4DSkill` | ? |
| Clan detection | `getClanId` — regex match on `hlstats_ClanTags` | ? |
| Trend tracker | `track_hlstats_trend` — every 5 min to `hlstats_Trend` | ? |
| Kill streak | `endKillStreak` — per-life counter + `kill_streak_N` actions | ? |
| GeoIP on connect | `GeoIP2::Database::Reader` MMDB lookup | ? |
| DNS resolver | `hlstats-resolve.pl` background pass | ? |
| Proxy protocol | `PROXY Key=X addr:port` packet prefix | ? |
| Signal handling | `SIGHUP` → reload config, `SIGINT` → graceful flush + exit | ? |

### 7. Write the markdown report

Write to `D:\source\hlstatsx.net\progress\daemon-parity.md`. Include:
- Generation date and time at the top
- Overall completeness percentage (✅ = 1.0, ⚠️ = 0.5, ❌ = 0.0)
- Core engine status table
- Event handler status table (one row per `doEvent_*` sub)
- RCON/broadcasting status
- Specific gaps with Perl line references where helpful
- Priority queue for remaining work

### 8. Update the Daemon feature table in CLAUDE.md

After completing the analysis, update the "Daemon Reference — Feature Map" tables in `CLAUDE.md` to reflect verified statuses. Use: `Done`, `Done (~N%)`, `Partial`, `Not started`, or `N/A`.

## Notes

- The authoritative Perl source is **only** in `legacy/perl/scripts/`. Do not reference any external Perl installation.
- `hlstats-awards.pl` and `hlstats-resolve.pl` are already fully ported to `HLStatsX.NET.Awards` — skip them unless the Awards section of CLAUDE.md shows a discrepancy.
- Skill calculation has six modes (0–4 standard + mode 5 ZPS) plus a separate `calcL4DSkill` for Left 4 Dead. Verify all branches if the skill engine is implemented.
- The event queue flushes when `queue.Count > g_event_queue_size` (default 10). Verify the .NET equivalent flushes under the same condition.
- `game_status` on `hlstats_Servers` (used by the web server list) is set by the daemon during map change events — confirm this DB write is present.
