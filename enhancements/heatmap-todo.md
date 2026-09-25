# Heatmap Feature — TODO & Implementation Plan

## Status: In Progress — Blocked

Code is committed and builds cleanly. The feature is not yet functional because two
external prerequisites are unmet. Do not continue implementation until both are resolved.

---

## Blockers

### 1. No coordinate data in the database

`hlstats_Events_Frags.pos_x/pos_y/pos_z` are all zero for every dods frag.
The Perl daemon only writes these columns when it finds `(attacker_position "X Y Z")`
tokens in the game server log. That token only appears when the server has:

```
mp_logdetail 3
```

Set this in `server.cfg` on the DoDS server. After the next map start, new kill events
will include position data and the columns will start populating.

**Historical data cannot be back-filled.** Only kills after this change will have coords.
Run this query periodically to confirm data is accumulating:

```sql
SELECT
    COUNT(*) AS total_frags,
    SUM(CASE WHEN pos_x IS NOT NULL AND pos_x != 0 THEN 1 ELSE 0 END) AS has_kill_coords,
    SUM(CASE WHEN pos_victim_x IS NOT NULL AND pos_victim_x != 0 THEN 1 ELSE 0 END) AS has_death_coords
FROM hlstats_Events_Frags ef
JOIN hlstats_Servers s ON s.serverId = ef.serverId
WHERE s.game = 'dods';
```

### 2. Missing overhead map images

The images in `wwwroot/hlstatsimg/games/dods/maps/` are gameplay screenshots —
not top-down/radar views. A heatmap overlay requires a perfectly top-down image
of the map at a consistent scale so coordinate transforms can align with geometry.

The original PHP implementation stored these in a separate `/heatmaps/` directory.
These images appear to be missing or were never sourced for dods. Options:

- **Extract from game files:** Source engine games ship radar images (used for the
  in-game minimap) in `Counter-Strike Source/cstrike/resource/overviews/` or similar.
  DoDS radar images live in `Day of Defeat Source/dod/resource/overviews/*.bmp`.
  These are the canonical calibrated overhead images; the game's `*.txt` files in the
  same folder contain `pos_x`, `pos_y`, and `scale` values — which map directly to
  what `hlstats_Heatmap_Config` needs.

- **Source from the community:** Sites like HLTV and community stat sites host
  pre-extracted radar images for popular maps.

**This is the key missing piece.** Without proper overhead images and matching
calibration values, the heatmap will not align with the map regardless of how much
coordinate data exists.

---

## Resuming Work — Ordered Steps

1. Set `mp_logdetail 3` on the DoDS server and let data accumulate (days to weeks).

2. Extract radar images from the DoDS game files for each active map:
   - `dod/resource/overviews/{mapname}.bmp` → convert to PNG/JPG
   - Place in `wwwroot/hlstatsimg/games/dods/heatmaps/{mapname}.jpg`
   - Read the matching `{mapname}.txt` to get `pos_x`, `pos_y`, `scale` values.

3. Insert `hlstats_Heatmap_Config` rows using values from the radar `.txt` files:
   ```sql
   INSERT INTO hlstats_Heatmap_Config
     (map, game, xoffset, yoffset, flipx, flipy, rotate, days, scale)
   VALUES
     ('dod_jagd', 'dods', <pos_x from txt>, <pos_y from txt>, 0, 1, 0, 30, <scale from txt>);
   ```
   Note: the `.txt` `scale` is units-per-pixel; `hlstats_Heatmap_Config.scale` uses the
   same convention. Verify alignment by checking a known landmark on the radar image.

4. Update the heatmap container in `Detail.cshtml` to use the radar image path
   (`/hlstatsimg/games/dods/heatmaps/{mapname}.jpg`) instead of the maps thumbnail.

5. Verify the calibrated overlay aligns — kills should cluster visibly in high-traffic
   areas (choke points, capture points). Adjust xoffset/yoffset if off.

6. Consider adding per-player heatmap tab on the Player Profile page.

---

## What Is Already Built

| File | Purpose |
|---|---|
| `Core/Entities/HeatmapConfig.cs` | Maps `hlstats_Heatmap_Config` |
| `Core/Interfaces/Repositories/IHeatmapRepository.cs` | Repository interface |
| `Core/Models/HeatmapModels.cs` | `HeatPoint` record |
| `Infrastructure/Repositories/HeatmapRepository.cs` | Raw ADO.NET, grid-binned SQL |
| `Infrastructure/Data/Configurations/HeatmapConfigConfiguration.cs` | EF config |
| `Web/Controllers/HeatmapController.cs` | `GET /Heatmap/Data` JSON API |
| `Web/Controllers/MapsController.cs` | Updated (heatmap repo removed, was added then simplified) |
| `Web/Views/Maps/Detail.cshtml` | Canvas overlay with kills/deaths tabs |
| `wwwroot/js/heatmap.min.js` | heatmap.js v2.0.5 bundled locally |
| `wwwroot/js/mapHeatmap.js` | Fetch + coordinate transform + auto-cal fallback |
| `EventFrag` entity | pos_x/y/z, pos_victim_x/y/z columns mapped |

The API returns data with or without a config row. Without a config row the JS
auto-calibrates (stretches coord range to fill the image) — useful for testing once
coordinate data exists, but not a substitute for proper calibration.
