# hlstats_Events_Frags — Scalability & Read Performance

Analysis date: 2026-04-24  
Current row count: ~24.3 million (AUTO_INCREMENT=24264679)  
Engine: InnoDB, Percona Server 8.0.36

---

## Current schema

```sql
CREATE TABLE `hlstats_Events_Frags` (
  `id`           int unsigned NOT NULL AUTO_INCREMENT,
  `eventTime`    datetime DEFAULT NULL,
  `serverId`     int unsigned NOT NULL DEFAULT '0',
  `map`          varchar(64) NOT NULL DEFAULT '',
  `killerId`     int unsigned NOT NULL DEFAULT '0',
  `victimId`     int unsigned NOT NULL DEFAULT '0',
  `weapon`       varchar(64) NOT NULL DEFAULT '',
  `headshot`     tinyint(1)  NOT NULL DEFAULT '0',
  `killerRole`   varchar(64) NOT NULL DEFAULT '',
  `victimRole`   varchar(64) NOT NULL DEFAULT '',
  `pos_x`        mediumint DEFAULT NULL,
  `pos_y`        mediumint DEFAULT NULL,
  `pos_z`        mediumint DEFAULT NULL,
  `pos_victim_x` mediumint DEFAULT NULL,
  `pos_victim_y` mediumint DEFAULT NULL,
  `pos_victim_z` mediumint DEFAULT NULL,
  `mapId`        int DEFAULT NULL,       -- unused by .NET app
  `roleId`       int DEFAULT NULL,       -- unused by .NET app
  `weaponId`     int DEFAULT NULL,       -- unused by .NET app
  PRIMARY KEY (`id`),
  KEY `serverId`   (`serverId`),
  KEY `headshot`   (`headshot`),
  KEY `map`        (`map`(5)),            -- ← 5 chars is nearly useless
  KEY `weapon16`   (`weapon`(16)),
  KEY `killerRole` (`killerRole`(8)),
  KEY `killerId`   (`killerId`, `eventTime`),
  KEY `victimId`   (`victimId`, `eventTime`),
  KEY `eventTime`  (`eventTime`)
)
```

---

## Query patterns against this table (from the .NET codebase)

All queries come from `PlayerStatsRepository`, `WeaponRepository`, and `MapRepository`.

| Query | Filter | Aggregate |
|---|---|---|
| Real kill count | `killerId = X` | COUNT |
| Real death count | `victimId = X` | COUNT |
| Real headshot count | `killerId = X AND headshot = 1` | COUNT |
| Kill/death matrix | `killerId = X GROUP BY victimId` | COUNT + headshot COUNT |
| Map performance (kills) | `killerId = X GROUP BY map` | COUNT + headshot COUNT |
| Map performance (deaths) | `victimId = X GROUP BY map` | COUNT |
| Server performance (kills) | `killerId = X JOIN servers GROUP BY serverId` | COUNT + headshot COUNT |
| Server performance (deaths) | `victimId = X GROUP BY serverId` | COUNT |
| Weapon stats per player | `killerId = X GROUP BY weapon` | COUNT + headshot COUNT |
| Role stats (kills) | `killerId = X GROUP BY killerRole` | COUNT |
| Role stats (deaths) | `victimId = X GROUP BY victimRole` | COUNT |
| Weapon detail — top killers | `weapon = X JOIN players WHERE game = Y GROUP BY killerId` | COUNT + headshot COUNT |
| Weapon kill totals | `weapon = X JOIN servers WHERE game = Y` | COUNT + headshot SUM |
| Map player leaderboard | `map = X JOIN players WHERE game = Y AND hideRanking=0 GROUP BY killerId` | COUNT + headshot SUM |
| Map total kills | `map = X JOIN servers WHERE game = Y` | COUNT |

**Critical observation:** not one query filters by `eventTime`. Every read query filters by `killerId`, `victimId`, `weapon`, or `map`. Time-bounded queries exist in the Perl daemon but not in the .NET read path.

---

## Problems with the current schema

### 1. The `map(5)` prefix index is effectively useless

Map names in Day of Defeat: Source include `dod_flash`, `dod_flanders`, `dod_anzio`, `dod_avalanche` — many share the same first 5 characters. The prefix index has near-zero selectivity for common prefixes and MySQL will skip it in favour of a full scan combined with another index. The map leaderboard query (`WHERE map = X`) against 24M rows with no usable index is an expensive operation.

### 2. `killerRole(8)` prefix has gaps

Role codes such as `machine_gunner` are longer than 8 characters; the prefix `machine_` is shared by any hypothetical `machine_*` variant. Even at 8 chars `rifleman` is exactly the limit — any addition breaks the index. A full-column index is small (role codes are short, low cardinality) and more reliable.

### 3. The `headshot` single-column index is ignored by MySQL

Cardinality of 2 (0 or 1). MySQL's optimizer will never pick a single-column index with two distinct values on a 24M row table — it would be slower than a full scan in almost every scenario. The index exists but wastes space on every INSERT and provides no benefit on any read.

### 4. The three int FK columns (`weaponId`, `mapId`, `roleId`) are populated but never used

The Perl daemon writes integer IDs alongside the varchar codes. The .NET application queries exclusively by varchar (`weapon`, `map`, `killerRole`). Integer FK lookups and indexes are dramatically smaller and faster than varchar — a `weaponId` int index entry is 4 bytes vs ~64 bytes for a `weapon` varchar entry. Switching the application to use these columns for filtering is the single highest-ROI change available.

### 5. Position columns are heatmap-only dead weight on every other query

`pos_x`, `pos_y`, `pos_z`, `pos_victim_x`, `pos_victim_y`, `pos_victim_z` — six `mediumint` columns, each 3 bytes of data plus InnoDB row overhead. These are only read by the heatmap feature (not yet implemented in .NET). On a 24M row table they represent ~400–500 MB of InnoDB page space that every non-heatmap query must page through when doing full-row reads.

### 6. No partition pruning is possible for archival or maintenance

The table will grow indefinitely as long as the server runs. There is no mechanism to drop old event data without a slow DELETE sweep. Time-based partitioning would allow dropping old partitions instantly.

### 7. `id` is `int unsigned` — ceiling at ~4.29 billion

At 24M rows now, this is not immediately urgent, but a busy server can accumulate tens of millions of frags per year. Worth planning the migration to `bigint unsigned` before it becomes critical.

---

## Recommendations

### Recommendation A — Fix the broken prefix indexes immediately

Low risk, high impact, no structural changes required.

```sql
-- Drop the useless indexes
ALTER TABLE hlstats_Events_Frags
  DROP INDEX `headshot`,
  DROP INDEX `map`,
  DROP INDEX `killerRole`,
  DROP INDEX `weapon16`;

-- Replace with correct-length or full-column indexes
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_map        (map),          -- full map name; cardinality is low but correct
  ADD INDEX idx_killerrole (killerRole),   -- short strings, full column
  ADD INDEX idx_weapon     (weapon);       -- covers weapon = X filter directly
```

**Why not just increase the prefix length?** Full-column indexes on short, repeated strings (map names, role codes, weapon codes) are only marginally larger than prefix indexes while being completely correct. The optimizer can use them for equality, range, and GROUP BY. Prefix indexes cannot be used for sorting or covering.

---

### Recommendation B — Add composite covering indexes for the hot read paths

These are the indexes that will have the most visible impact on page load times. A covering index lets MySQL answer the entire query from the index pages without touching the table rows.

```sql
-- Weapon stats per player: WHERE killerId = X GROUP BY weapon (with headshot count)
-- Covers GetWeaponStatsAsync, GetFavoriteWeaponAsync
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_weapon_hs (killerId, weapon, headshot);

-- Map performance per player: WHERE killerId/victimId = X GROUP BY map (with headshot count)
-- Covers GetMapPerformanceAsync (both queries)
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_map_hs (killerId, map, headshot),
  ADD INDEX idx_victim_map    (victimId, map);

-- Role stats: WHERE killerId/victimId = X GROUP BY killerRole/victimRole
-- Covers GetRoleSelectionAsync (both queries)
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_role (killerId, killerRole),
  ADD INDEX idx_victim_role (victimId, victimRole);

-- Headshot count for real stats: WHERE killerId = X AND headshot = 1 (COUNT)
-- Covers GetRealStatsAsync headshot query
-- The existing (killerId, eventTime) doesn't cover headshot filtering
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_headshot (killerId, headshot);

-- Weapon detail page top killers: WHERE weapon = X GROUP BY killerId
-- Covers WeaponRepository.GetWeaponKillersAsync
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_weapon_killer_hs (weapon, killerId, headshot);

-- Map player leaderboard: WHERE map = X GROUP BY killerId (with headshot)
-- Covers MapRepository.GetPlayerLeaderboardAsync and GetMapTotalKillsAsync
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_map_killer_hs (map, killerId, headshot);

-- Server performance per player: WHERE killerId/victimId = X GROUP BY serverId
-- Covers GetServerPerformanceAsync (both queries)
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_server_hs (killerId, serverId, headshot),
  ADD INDEX idx_victim_server    (victimId, serverId);
```

---

### Recommendation C — Use integer FK columns instead of varchar for filtering (medium-term)

This is a .NET code change, not a schema change. The table already has `weaponId`, `mapId`, and `roleId` populated by the daemon. Switching the repository queries to filter on these integer columns instead of the varchar columns would:

- Reduce index size by ~10× (4-byte int vs ~40-byte average varchar)
- Enable MySQL to use smaller, denser index pages — more fits in the buffer pool
- Make joins to `hlstats_Weapons`, `hlstats_Maps_Counts`, and `hlstats_Roles` trivial FK joins instead of varchar equality joins

**What this requires:**
1. Verify the daemon actually populates `weaponId`, `mapId`, `roleId` for all rows (check for NULLs).
2. Update EF Core entity mappings to use the int columns.
3. Update repository queries to filter by `WeaponId`, `MapId`, `RoleId`.
4. Add integer FK indexes:

```sql
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_weaponid_killer (weaponId, killerId, headshot),
  ADD INDEX idx_mapid_killer    (mapId, killerId, headshot),
  ADD INDEX idx_killer_mapid    (killerId, mapId, headshot),
  ADD INDEX idx_victim_mapid    (victimId, mapId);
```

Once this is done, the `weapon`, `map`, and `killerRole` varchar indexes can all be dropped, significantly reducing write overhead and storage.

---

### Recommendation D — Vertical partition: move position columns to a separate table

The six position columns (`pos_x`, `pos_y`, `pos_z`, `pos_victim_x`, `pos_victim_y`, `pos_victim_z`) are only ever read by the heatmap feature. Every other query forces InnoDB to load full rows including these columns.

**Estimated storage saving:** 6 columns × 3 bytes each × 24M rows = ~430 MB of data, plus InnoDB page overhead. Moving these to a child table means the main table rows are shorter — more rows fit per 16 KB InnoDB page — and all non-heatmap queries become faster due to improved buffer pool utilisation.

```sql
-- New table for heatmap data only
CREATE TABLE hlstats_Events_Frags_Positions (
  `fragId`       int unsigned NOT NULL,
  `pos_x`        mediumint DEFAULT NULL,
  `pos_y`        mediumint DEFAULT NULL,
  `pos_z`        mediumint DEFAULT NULL,
  `pos_victim_x` mediumint DEFAULT NULL,
  `pos_victim_y` mediumint DEFAULT NULL,
  `pos_victim_z` mediumint DEFAULT NULL,
  PRIMARY KEY (`fragId`)
) ENGINE=InnoDB;

-- Migrate existing data (run during off-peak hours — one-time operation)
INSERT INTO hlstats_Events_Frags_Positions
  SELECT id, pos_x, pos_y, pos_z, pos_victim_x, pos_victim_y, pos_victim_z
  FROM hlstats_Events_Frags
  WHERE pos_x IS NOT NULL OR pos_y IS NOT NULL OR pos_z IS NOT NULL;

-- After verifying data, remove from main table
ALTER TABLE hlstats_Events_Frags
  DROP COLUMN pos_x,
  DROP COLUMN pos_y,
  DROP COLUMN pos_z,
  DROP COLUMN pos_victim_x,
  DROP COLUMN pos_victim_y,
  DROP COLUMN pos_victim_z;
```

The Perl daemon would need updating to write to both tables. The heatmap feature (not yet implemented in .NET) would JOIN the two tables.

---

### Recommendation E — Range partitioning by year for archival (long-term)

InnoDB RANGE partitioning on `eventTime` allows:
- Instant partition drops instead of slow DELETEs when archiving old data
- Partition pruning for any future time-bounded queries
- Online partition management without locking the whole table

**Important constraints:**
- MySQL requires the partition key to be part of the primary key. The current PK is just `id`. To partition by `eventTime` the PK must become `(id, eventTime)` or `(eventTime, id)`. The former is preferred — it keeps the existing auto-increment semantics and just adds `eventTime` to guarantee uniqueness per partition.
- `eventTime` is currently nullable. It must be made `NOT NULL` with a default before partitioning.

```sql
-- Step 1: make eventTime NOT NULL (verify no NULLs exist first)
SELECT COUNT(*) FROM hlstats_Events_Frags WHERE eventTime IS NULL;

ALTER TABLE hlstats_Events_Frags
  MODIFY COLUMN eventTime datetime NOT NULL DEFAULT '2000-01-01 00:00:00';

-- Step 2: change primary key to include eventTime
ALTER TABLE hlstats_Events_Frags
  DROP PRIMARY KEY,
  ADD PRIMARY KEY (id, eventTime);

-- Step 3: add partitions (adjust years to match your actual data range)
ALTER TABLE hlstats_Events_Frags
  PARTITION BY RANGE (YEAR(eventTime)) (
    PARTITION p2020 VALUES LESS THAN (2021),
    PARTITION p2021 VALUES LESS THAN (2022),
    PARTITION p2022 VALUES LESS THAN (2023),
    PARTITION p2023 VALUES LESS THAN (2024),
    PARTITION p2024 VALUES LESS THAN (2025),
    PARTITION p2025 VALUES LESS THAN (2026),
    PARTITION p2026 VALUES LESS THAN (2027),
    PARTITION pfuture VALUES LESS THAN MAXVALUE
  );
```

**After partitioning, archiving a year is instant:**
```sql
ALTER TABLE hlstats_Events_Frags
  REORGANIZE PARTITION p2020 INTO (
    PARTITION p2020 VALUES LESS THAN (2021)  -- or DROP PARTITION p2020
  );
-- Or simply:
ALTER TABLE hlstats_Events_Frags DROP PARTITION p2020;
```

**Limitation to be aware of:** The .NET queries filter by `killerId` or `weapon`, not `eventTime`, so partition pruning won't apply to them. The main benefit is operational — archival, table maintenance, and OPTIMIZE TABLE operations become proportional to the active partition size rather than the full 24M rows.

---

### Recommendation F — Plan for `id` column overflow

The `id` column is `int unsigned` with a maximum of 4,294,967,295. At 24M rows today the ceiling is ~180× away, but a server generating 5M frags/year would hit it in ~850 years — not urgent. However, migrating `int` to `bigint` on a 24M row table is a full table rebuild (ALTER locks the table). Doing it now costs ~5 minutes; doing it at 200M rows costs proportionally more.

```sql
-- Low urgency, but straightforward to do proactively during a maintenance window
ALTER TABLE hlstats_Events_Frags
  MODIFY COLUMN id bigint unsigned NOT NULL AUTO_INCREMENT;
```

---

## Recommended execution order

| Step | Change | Risk | Benefit |
|---|---|---|---|
| 1 | Recommendation A — fix prefix indexes | Low | High |
| 2 | Recommendation B — add composite covering indexes | Low | High |
| 3 | Recommendation F — bigint id | Low | Preventive |
| 4 | Recommendation C — switch to int FK columns (code change) | Medium | High |
| 5 | Recommendation D — vertical partition positions | Medium | Medium |
| 6 | Recommendation E — time-based partitioning | High | Operational |

Steps 1–3 are pure index additions and can be run online on Percona InnoDB with `ALGORITHM=INPLACE`. Steps 4–6 require code changes, data migration, or structural table changes and should be planned during a maintenance window.

---

## Complete SQL for steps 1–3

```sql
-- -------------------------------------------------------
-- hlstats_Events_Frags — index fixes and additions
-- All ADD INDEX operations are non-blocking on Percona InnoDB.
-- -------------------------------------------------------

-- Remove broken/low-value indexes
ALTER TABLE hlstats_Events_Frags
  DROP INDEX `headshot`,
  DROP INDEX `map`,
  DROP INDEX `killerRole`,
  DROP INDEX `weapon16`;

-- Add corrected single-column indexes
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_map        (map),
  ADD INDEX idx_killerrole (killerRole),
  ADD INDEX idx_weapon     (weapon);

-- Add composite covering indexes for hot read paths
ALTER TABLE hlstats_Events_Frags
  ADD INDEX idx_killer_weapon_hs  (killerId, weapon, headshot),
  ADD INDEX idx_killer_map_hs     (killerId, map, headshot),
  ADD INDEX idx_victim_map        (victimId, map),
  ADD INDEX idx_killer_role       (killerId, killerRole),
  ADD INDEX idx_victim_role       (victimId, victimRole),
  ADD INDEX idx_killer_headshot   (killerId, headshot),
  ADD INDEX idx_weapon_killer_hs  (weapon, killerId, headshot),
  ADD INDEX idx_map_killer_hs     (map, killerId, headshot),
  ADD INDEX idx_killer_server_hs  (killerId, serverId, headshot),
  ADD INDEX idx_victim_server     (victimId, serverId);

-- Bigint id migration (Recommendation F)
ALTER TABLE hlstats_Events_Frags
  MODIFY COLUMN id bigint unsigned NOT NULL AUTO_INCREMENT;
```
