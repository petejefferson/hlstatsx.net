# HLStatsX.NET.Daemon — Implementation State

Last updated: 2026-05-09

## What This Is

A .NET 10 Worker Service rewriting the Perl `hlstats.pl` daemon. UDP listener receives
Half-Life game server log packets, parses them, and writes stats to MySQL via EF Core.

Reference: `legacy/perl/scripts/hlstats.pl` + `HLstats_EventHandlers.plib`

## Completed Files

### Configuration
- `Configuration/DaemonOptions.cs` — global options (Mode, UseTimestamp, SkillMaxChange, etc.)
- `Configuration/ServerConfig.cs` — per-server config (MinPlayers, IgnoreBots, TkPenalty, etc.)

### Parsing
- `Parsing/ParsedLogLine.cs` — timestamp + event text struct
- `Parsing/LogLineParser.cs` — strips noise, parses `L MM/DD/YYYY - HH:MM:SS:` prefix
- `Parsing/PlayerInfo.cs` — parsed player identity record
- `Parsing/PlayerStringParser.cs` — `Name<uid><steamid><team>` → PlayerInfo
- `Parsing/SteamIdNormalizer.cs` — `[U:1:N]`→`STEAM_0:Y:Z`, strips `STEAM_X:` prefix
- `Parsing/BotDetector.cs` — detects BOT/0/00000000:N:0 unique IDs
- `Parsing/PropertiesParser.cs` — `(key "value")` property strings; bug fixed: key regex `[^\s)]+`

### State
- `State/PlayerSession.cs` — in-memory live player state (skill, kills, streaks, etc.)
- `State/ServerState.cs` — in-memory live server state + player registry
- `State/DaemonStateManager.cs` — loads all servers + options from DB; preserves sessions on reload

### Network
- `Network/ReceivedPacket.cs` — (string Raw, string SenderAddr) record struct
- `Network/UdpListener.cs` — async UDP receive loop + STDIN import mode; `IsStdinMode` property

### EventQueue
- `EventQueue/EventQueue.cs` — generic batch-insert buffer; flush at threshold or explicit call

### Skill
- `Skill/SkillResult.cs` — (int KillerSkill, int VictimSkill) record struct
- `Skill/SkillCalculator.cs` — modes 0-5, L4D variant; exact port of Perl calcSkill

### Event Handlers (ALL COMPLETE)
- `Events/EventContext.cs` — per-event shared context (server, unix time, map)
- `Events/PlayerDbService.cs` — load/create player from DB; flush session stats atomically
- `Events/ConnectHandler.cs` — connect: create session, load/create DB record, handle reconnects
- `Events/EnterGameHandler.cs` — enter game: set connect time, log EventEntry
- `Events/DisconnectHandler.cs` — disconnect: flush session, log EventDisconnect, remove player
- `Events/ChangeTeamHandler.cs` — team switch: update session + log EventChangeTeam
- `Events/ChangeRoleHandler.cs` — role change: update session, log EventChangeRole, increment role pick count
- `Events/ChangeNameHandler.cs` — name change: flush old name stats, ensure new alias, NameTrack mode
- `Events/FragHandler.cs` — kill: skill calc, kill streak, log EventFrag; TK branch: penalty, log EventTeamkill
- `Events/SuicideHandler.cs` — suicide penalty, log EventSuicide
- `Events/ChatHandler.cs` — chat logging to EventChat (say/say_team modes)
- `Events/PlayerActionHandler.cs` — single-player objectives: reward + log EventPlayerAction
- `Events/PlayerPlayerActionHandler.cs` — pvp actions: reward both players + log EventPlayerPlayerAction
- `Events/TeamBonusHandler.cs` — reward all team members (EventPlayerAction per player)
- `Events/MapChangeHandler.cs` — map loading/started: clear bots, reset streaks/teams/map state
- `Events/StatsmeHandler.cs` — StatsMe weapon shots/hits/damage log to EventStatsme
- `Events/EventRouter.cs` — main dispatch: regex chain (kill → statsme → suicide → player-verb-obj → player-verb → team → world); weapon modifier lookup; CS:GO STEAM USERID validated connect; pointcaptured / captured_loc team actions; Round_Win team action

### Trend
- `Trend/TrendTracker.cs` — writes to `hlstats_Trend` every 299 seconds per game

### Worker
- `DaemonWorker.cs` — **COMPLETE**: BackgroundService; UDP→parse→dispatch loop; trend timer; graceful shutdown flushes all player sessions
- `Program.cs` — **COMPLETE**: full DI registration; MySQL DbContextFactory; all handlers + router as singletons; UdpListener from appsettings.json `Daemon:*`

## Tests (84 passing)
- `Daemon/Parsing/LogLineParserTests.cs` — 11 tests
- `Daemon/Parsing/SteamIdNormalizerTests.cs` — 7 tests
- `Daemon/Parsing/BotDetectorTests.cs` — 7 tests
- `Daemon/Parsing/PlayerStringParserTests.cs` — 14 tests
- `Daemon/Parsing/PropertiesParserTests.cs` — 13 tests
- `Daemon/Skill/SkillCalculatorTests.cs` — 17 tests

Run: `dotnet test tests/HLStatsX.NET.Tests/ --filter "FullyQualifiedName!~RepositoryTests&FullyQualifiedName~Daemon"`

## Known Build Fixes Applied
- `DaemonStateManager.cs`: ambiguity between `Configuration.ServerConfig` and `Core.Entities.ServerConfig`
  resolved with `using EntityServerConfig = HLStatsX.NET.Core.Entities.ServerConfig;`
- `LogLineParser.cs`: range expression `cleaned[(match.Index + match.Length)..]` — parens required
- `PropertiesParser.cs`: key regex changed from `\S+` to `[^\s)]+` to stop greedy `)` consumption
- `TrendTracker.cs`: `HLStatsX.NET.Daemon.Trend` namespace conflicts with `Core.Entities.Trend`;
  resolved with `using TrendEntity = HLStatsX.NET.Core.Entities.Trend;`
- `DaemonWorker.cs`: `EventContext.EventTimeUtc` is a computed property (not settable); removed from
  object initializer (it derives automatically from `EventUnix`)

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": { "HLStats": "..." },
  "Daemon": {
    "BindAddress": "0.0.0.0",
    "Port": 27500,
    "StdinMode": false
  }
}
```

Local override goes in `appsettings.Development.json` (never committed).

## Key Patterns

### DB Access in Handlers
Handlers receive `IDbContextFactory<HLStatsDbContext>` and open short-lived contexts.
For batch inserts use `EventQueue<T>`. For single-row updates use direct EF saves.

### Skill Calc Call Pattern
```csharp
var result = SkillCalculator.Calc(
    server.Config.SkillMode,
    killer.Skill, killer.Kills,
    victim.Skill, victim.Deaths,
    weaponModifier,
    options.SkillMaxChange, options.SkillMinChange,
    options.PlayerMinKills, options.SkillRatioCap,
    killer.Team);
killer.Skill = result.KillerSkill;
victim.Skill = result.VictimSkill;
```

### Player Lookup Pattern
```csharp
var player = server.LookupPlayer(userId, uniqueId)
          ?? server.FindByUniqueId(uniqueId);
```

### EventRouter Dispatch Order (mirrors Perl elsif chain)
1. Kill regex (hot path first)
2. Statsme weaponstats
3. Statsme latency/time (skip — deferred)
4. CS:GO bracket-coords suicide
5. Player-verb-obj (connect/join/role/name/triggered/say/suicide)
6. Player-verb (entered/disconnected/kicked/STEAM validated)
7. Team triggered (pointcaptured/captured_loc/general team action)
8. Admin plugin lines (skip — deferred)
9. World verb "obj" (World triggered/Loading map/Started map)

## Remaining / Deferred

### Not yet started (post-parity)
- Server auto-registration for unknown servers (currently skipped)
- RCON broadcasting (all explicitly deferred)
- DNS resolver integration
- Proxy daemon protocol support
- Statsme2 (per-hitbox stats) — deferred

### Completed (post-core)
- `Events/ClanService.cs` — clan tag matching: quotemeta → first-word capture → A/X wildcards → START/END/EITHER; creates clans; ClanId stored in PlayerSession and flushed via FlushSessionAsync
- `Geo/GeoIpLookup.cs` — real-time GeoIP on connect; opens GeoLite2-City MMDB once; writes flag/country/city/state/lat/lng to hlstats_Players; controlled by UseGeoIpBinary option
- `Events/LivestatService.cs` — hlstats_Livestats row lifecycle: REPLACE INTO on connect, UPDATE on flush, DELETE on disconnect; ServerId added to PlayerSession

### EF Entities Needed (not yet in Core)
Check `legacy/perl/scripts/HLstats_EventHandlers.plib` INSERT statements for column lists.
(These are referenced in handlers already — verify they exist in `HLStatsDbContext`.)

## CLAUDE.md Daemon Feature Map Status
Update rows when wiring daemon to production:
- Core Engine: most → "Done" except GeoIP, DNS, RCON, proxy
- Event Handlers: all → "Done"
- Skill & State: SkillCalculator, KillStreak, TrendTracker → "Done"
- RCON: all remain "Deferred"
