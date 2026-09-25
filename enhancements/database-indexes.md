# Database Index Enhancements

Analysis date: 2026-04-24  
Schema version: Percona Server 8.0.36-28  
Source: cross-reference of `reference/schema.sql` against all Infrastructure repository LINQ queries.

---

## Summary

The existing schema has reasonable single-column indexes on the most-used tables, but several high-traffic query patterns rely on filtering or sorting across multiple columns that no composite index covers. MySQL must pick one index and re-evaluate the rest in-memory, which becomes expensive as row counts grow.

The changes below are grouped by priority. Each entry explains the query pattern driving the recommendation, the gap in the current schema, and the SQL to fix it.

---

## Priority 1 — High impact, large tables

### 1. `hlstats_Players` — Leaderboard composite indexes

**Affected queries:** `PlayerRepository` — `GetLeaderboardAsync`, `GetRanksAsync`, all sorting variants.

**Problem:** The leaderboard filters on `game = X AND hideranking = 0 AND kills >= N` and then orders by `skill`, `kills`, `deaths`, `headshots`, `connection_time`, or `activity`. The table currently has three separate single-column indexes (`game`, `skill`, `kills`). MySQL can only use one of them per query — it typically picks `game`, then sorts the filtered result set in memory. On a table with tens of thousands of rows across multiple games this degrades with scale.

**Fix:** Two composite covering indexes — one for skill-sorted queries (the default leaderboard sort), one for kill-sorted queries (the second most common). Both include `hideranking` and `kills` in the key so the WHERE clause is satisfied entirely within the index before any row access occurs.

```sql
ALTER TABLE hlstats_Players
  ADD INDEX idx_leaderboard_skill (game, hideranking, kills, skill DESC),
  ADD INDEX idx_leaderboard_kills (game, hideranking, kills DESC);
```

---

### 2. `hlstats_Players_History` — Player history range scans

**Affected queries:** `PlayerRepository` — `GetHistoryDatesAsync`, `GetLeaderboardAsync` with `rankType` of `week`, `month`, or a specific date; `GetTrendDataAsync`.

**Problem:** The only index on this table is a UNIQUE key `(eventTime, playerId, game)` and a secondary `playerId` key. The UNIQUE key has `eventTime` as the leading column — it is optimal for uniqueness enforcement but the wrong order for the application's most common access pattern: "give me all history rows for player X in game Y, ordered by date". That query scans the `playerId` index to find the player's rows, then does a filesort. For the period-based leaderboard it is worse — it aggregates across many players filtered by `game` and a date range, which the current indexes cannot cover together.

**Fix:** Add a composite index with `playerId` and `game` leading, `eventTime` trailing so date-range scans on a specific player/game pair use the index directly.

```sql
ALTER TABLE hlstats_Players_History
  ADD INDEX idx_player_game_date (playerId, game, eventTime);
```

---

### 3. `hlstats_Players_Ribbons` — No indexes at all

**Affected queries:** `AwardRepository` — `GetPlayerRibbonsAsync`; `PlayerRepository` — player profile ribbon display.

**Problem:** This table has no indexes whatsoever — not even a primary key. Every query is a full table scan. As the daemon awards ribbons to thousands of players the table grows and every profile page load incurs a full scan.

**Fix:** Add a composite index on `(playerId, game)` which is the exact filter the application uses.

```sql
ALTER TABLE hlstats_Players_Ribbons
  ADD INDEX idx_player_game (playerId, game);
```

---

### 4. `geoLiteCity_Blocks` — No indexes at all

**Affected queries:** Geo-IP lookups during player connect processing and any future country-stats pages.

**Problem:** This table stores IP address ranges (`startIpNum`, `endIpNum`) mapped to location IDs. Range lookups of the form `startIpNum <= @ip AND endIpNum >= @ip` against an unindexed table on what is typically hundreds of thousands of rows result in full table scans for every player connect event the daemon processes.

**Fix:** Index `startIpNum` so MySQL can range-scan to the candidate rows before evaluating `endIpNum`.

```sql
ALTER TABLE geoLiteCity_Blocks
  ADD INDEX idx_startip (startIpNum);
```

---

## Priority 2 — Medium impact

### 5. `hlstats_Players_Awards` — Query direction mismatch with primary key

**Affected queries:** `AwardRepository` — `GetDailyAwardsAsync`, `GetPlayerAwardsAsync`.

**Problem:** The primary key is `(awardTime, awardId, playerId, game)`. The application queries by `game` and `awardId` (to list winners for an award) or by `game` and `playerId` (to list a player's awards). Neither of these patterns matches the leading column `awardTime`, so the PK is not used for those lookups and MySQL falls back to a full scan of the clustered index.

**Fix:** Add a secondary index with `game` and `awardId` leading, `awardTime` trailing (DESC for most-recent-first ordering).

```sql
ALTER TABLE hlstats_Players_Awards
  ADD INDEX idx_game_award_date (game, awardId, awardTime DESC);
```

---

### 6. `hlstats_Trend` — Separate indexes instead of composite

**Affected queries:** `PlayerRepository` — `GetTrendDataAsync`; future server trend/sparkline queries.

**Problem:** The table has two separate single-column indexes: `game` and `timestamp`. Trend queries always filter by `game` AND order or range-scan by `timestamp` together. MySQL must choose one index, then sort or filter the result in memory.

**Fix:** Replace the separate indexes with a single composite, which covers the filter and the sort in one pass.

```sql
ALTER TABLE hlstats_Trend
  DROP INDEX `game`,
  DROP INDEX `timestamp`,
  ADD INDEX idx_game_timestamp (game, timestamp);
```

---

### 7. `hlstats_Events_Frags` — Weapon-per-player aggregations

**Affected queries:** `WeaponRepository` — `GetWeaponKillersAsync`; `PlayerStatsRepository` — player profile weapon breakdown tab.

**Problem:** The existing `weapon16` index is a 16-character prefix index on the `weapon` column alone. The weapon detail page and player weapon stats both group kills by `(killerId, weapon)` or `(victimId, weapon)`. The composite `(killerId, eventTime)` index covers kill counts per player but not grouped by weapon. MySQL ends up reading all kills for a player and grouping in memory.

**Fix:** Add composite indexes on `(killerId, weapon)` and `(victimId, weapon)` using a 32-character prefix — long enough to distinguish all known weapon codes without indexing the full column.

```sql
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_weapon (killerId, weapon(32)),
  ADD INDEX idx_victim_weapon (victimId, weapon(32));
```

---

### 8. `hlstats_Events_PlayerActions` and `hlstats_Events_PlayerPlayerActions` — Action detail aggregation

**Affected queries:** `ActionRepository` — `GetActionTopPlayersAsync` (action detail page — top players per action).

**Problem:** Both tables have separate single-column indexes on `actionId` and `playerId`. The action detail query filters by `actionId` and then groups by `playerId` to count events per player. With separate indexes MySQL uses the `actionId` index to find matching rows, then sorts/groups the result by `playerId` in memory.

**Fix:** Composite index with `actionId` leading so the WHERE clause narrows the row set before the `playerId` grouping.

```sql
ALTER TABLE hlstats_Events_PlayerActions
  ADD INDEX idx_action_player (actionId, playerId);

ALTER TABLE hlstats_Events_PlayerPlayerActions
  ADD INDEX idx_action_player (actionId, playerId);
```

---

### 9. `hlstats_Events_Frags` — Headshot filter per killer

**Affected queries:** `PlayerStatsRepository` — headshot count for player profile.

**Problem:** The existing `headshot` single-column index has very low cardinality (only two values: 0 or 1) so MySQL typically ignores it in favour of the `(killerId, eventTime)` index. Headshot-rate queries filter by both `killerId = X` and `headshot = 1` — the composite index below lets both conditions be satisfied inside the index.

**Fix:**

```sql
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_headshot (killerId, headshot);
```

---

## Priority 3 — Minor / future-proofing

### 10. `hlstats_Players` — Country leaderboard filter

**Affected queries:** `CountryRepository` — `GetCountryLeaderboardAsync`.

**Problem:** The country leaderboard filters by `game`, `hideranking = 0`, and `flag != ''`, then aggregates. The existing `game` index covers the first condition; `hideranking` and `flag` filtering happens in memory.

```sql
ALTER TABLE hlstats_Players
  ADD INDEX idx_country_leaderboard (game, hideranking, flag);
```

---

### 11. `hlstats_Players` — Clan member aggregation

**Affected queries:** `ClanRepository` — `GetClanProfileAsync` aggregates kills/deaths/skill across all members of a clan with `hideranking = 0`.

**Problem:** The existing `playerclan (clan, playerId)` index finds clan members but does not include `hideranking` or `kills`, so filtering and aggregating requires visiting all member rows.

```sql
ALTER TABLE hlstats_Players
  ADD INDEX idx_clan_ranking (clan, hideranking, kills);
```

---

## Complete SQL script

The statements below can be run as a single migration. All are `ADD INDEX` operations (non-blocking on Percona/InnoDB with `ALGORITHM=INPLACE`) except the `hlstats_Trend` change which drops two existing indexes first.

```sql
-- -------------------------------------------------------
-- HLStatsX.NET — Index enhancements
-- Safe to run on a live Percona/InnoDB instance.
-- DROP INDEX statements on hlstats_Trend are the only
-- destructive steps; the replaced indexes are superseded
-- by the new composite.
-- -------------------------------------------------------

-- Priority 1

ALTER TABLE hlstats_Players
  ADD INDEX idx_leaderboard_skill   (game, hideranking, kills, skill DESC),
  ADD INDEX idx_leaderboard_kills   (game, hideranking, kills DESC),
  ADD INDEX idx_country_leaderboard (game, hideranking, flag),
  ADD INDEX idx_clan_ranking        (clan, hideranking, kills);

ALTER TABLE hlstats_Players_History
  ADD INDEX idx_player_game_date (playerId, game, eventTime);

ALTER TABLE hlstats_Players_Ribbons
  ADD INDEX idx_player_game (playerId, game);

ALTER TABLE geoLiteCity_Blocks
  ADD INDEX idx_startip (startIpNum);

-- Priority 2

ALTER TABLE hlstats_Players_Awards
  ADD INDEX idx_game_award_date (game, awardId, awardTime DESC);

ALTER TABLE hlstats_Trend
  DROP INDEX `game`,
  DROP INDEX `timestamp`,
  ADD  INDEX idx_game_timestamp (game, timestamp);

ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_weapon  (killerId, weapon(32)),
  ADD INDEX idx_victim_weapon  (victimId, weapon(32)),
  ADD INDEX idx_killer_headshot (killerId, headshot);

ALTER TABLE hlstats_Events_PlayerActions
  ADD INDEX idx_action_player (actionId, playerId);

ALTER TABLE hlstats_Events_PlayerPlayerActions
  ADD INDEX idx_action_player (actionId, playerId);
```
