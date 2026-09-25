# Compare to PHP Site

Perform a comprehensive feature parity check between the current PHP HLStatsX implementation and the .NET rewrite. Analyse `legacy/php` as the authoritative spec, inspect the .NET source in `src/`. A running version of the PHP site can be found at http://localhost:5080/hlstats.php and the .NET site can be found at https://localhost:7017/.

## Steps

### 1. Read the existing status baseline

Read the feature status table from `CLAUDE.md` (section "PHP Reference — Feature Map"). This is the starting point — treat every "Done" entry as **unverified until proven** by the steps below.

### 2. Build the PHP feature inventory

List all `.php` files in `legacy/php/pages/` and `legacy/php/pages/admintasks/` and `legacy/php/pages/ingame/`. Cross-reference with the baseline table to catch any PHP pages not yet in the inventory.

### 3. Deep-verify each feature — read both sides

For **every** feature in the inventory (not just unknowns), do the following:

a. **Read the PHP source** — open the relevant `.php` file(s) in `legacy/php/pages/` and identify:
   - Each distinct tab, section, or data block rendered (e.g. "General", "Weapons", "Maps", "Sessions", "Awards" on a profile page)
   - Key SQL queries or data shown
   - Any sub-pages or linked actions (e.g. detail drilldowns)

b. **Read the .NET source** — open the corresponding controller action(s) in `src/HLStatsX.NET.Web/Controllers/` and the Razor view(s) in `src/HLStatsX.NET.Web/Views/`. Check:
   - Does a controller action exist for each PHP page/action?
   - Does the Razor view render each tab/section identified in step (a)?
   - Are service calls present for every major data block?

c. **List verified sections and missing sections explicitly** before assigning a status.

### 4. Classify each feature

Use strict criteria — err on the side of ⚠️ over ✅:

- ✅ **Fully implemented** — *every* major PHP section/tab is present in the .NET view, all key data is queried, no broken links. You must be able to name each verified section.
- ⚠️ **Partially implemented** — controller and at least one view exist, but one or more PHP sections/tabs are missing, data queries are absent, or links are broken. Name the specific gaps.
- ❌ **Not yet implemented** — no controller action, no views, or only a stub with no data.

### 4b. Assess background services and daemon

In addition to the web feature inventory, assess the two non-web components:

**HLStatsX.NET.Awards** (`src/HLStatsX.NET.Awards/`):
Compare against `legacy/perl/scripts/hlstats-awards.pl` and `hlstats-resolve.pl`. Check which of the following Perl subroutines have a .NET equivalent service:
- `DoInactive` → `PlayerActivityService`
- `DoAwards` → `AwardsCalculationService`
- `DoRibbons` → `RibbonsService`
- `DoGeoIP` → `GeoIpService` (MaxMind GeoLite2-City MMDB, `--geoip` flag, configurable path)
- `DoClans` → `ClansService`
- `DoPruning` → `PruningService`
- `DoOptimize` → `OptimizeService`
Also verify: scheduled daily runner, on-demand `--run-now` CLI flags.

**HLStatsX.NET Daemon** (replacement for `legacy/perl/scripts/hlstats.pl`):
Check `src/` for any daemon project. Note what exists and what remains (UDP listener, event parser, skill engine, player state management, bot filtering, etc.). Mark as WIP if no complete implementation exists.

Include a "🔧 Background Services & Daemon" section in both the markdown and HTML reports with progress bars and component-level status lists for both components.

### 5. Calculate parity percentages

Score each status: ✅ = 1.0, ⚠️ = 0.5, ❌ = 0.0. Calculate:
- Public stats pages score (weighted 75%)
- Admin panel score (weighted 25%)
- Overall weighted score
- Awards service completeness (separate, not weighted into overall)
- Daemon completeness (separate, not weighted into overall)

### 6. Write the markdown report

Write to `D:\source\hlstatsx.net\progress\feature-parity.md`. Include:
- Generation date **and time** (e.g. `2026-05-09 19:41 BST`) at the top — read the `<current_datetime>` from the system prompt for the exact value
- Overall parity summary table with scores
- "Changed since last report" section (compare to previous statuses in the existing file)
- Full feature-by-feature status table (✅ / ⚠️ / ❌) with a "Verified sections / Gaps" column
- Background services section (Awards + Daemon) with component-level detail
- Priority queue for remaining work
- "What's working well" summary

### 7. Write the HTML marketing report

Write to `D:\source\hlstatsx.net\progress\html\feature-parity.html`. Self-contained, visually polished:
- Inline CSS (no external dependencies)
- Progress bars showing overall parity percentages
- Feature status cards with emoji indicators (✅ ⚠️ ❌)
- A priority queue section
- **IMPORTANT — Preserve the existing CSS style exactly.** Before writing the file, read the existing `feature-parity.html` and copy its `<style>` block verbatim. Only update the HTML content (progress numbers, new-feature grids, feature tables, background services section, priority queue, strengths, **footer date and time**). The CSS uses the HLStatsX.NET.Web site palette (`--bg: #D0D4DC`, `--surface: #EAECF0`, `--highlight: #3A5570`, dark `--nav-bg: #161C28` topbar and footer) — do not replace it with a different theme.
- The `date-badge` span and the footer `<p>` tag must both include the **date and time** (e.g. `Updated 2026-05-09 19:41 BST`).
- Include a "🔧 Background Services & Daemon" section (after the overall progress cards) with progress-card style entries for `HLStatsX.NET.Awards` and `HLStatsX.NET Daemon`, using the `.component-status` class for the per-function checklist.

### 8. Update the feature status tables in the project instructions

After completing the analysis, update **both** files to reflect the new verified statuses:

- `CLAUDE.md` — the "PHP Reference — Feature Map" table (around line 219)
- `.github/copilot-instructions.md` — the "PHP Reference" table (around line 134)

Use the same row format already in those tables. Status values: `Done`, `Partial`, `Not started`, or `Done (~N%)` for near-complete features.

## Notes

- Always use the actual source code as ground truth — do not rely solely on `GAP-ANALYSIS.md` (may be outdated) or the existing status tables (unverified).
- Admin panel features are in `legacy/php/pages/admin.php` and `legacy/php/pages/admintasks/`.
- In-game pages are in `legacy/php/pages/ingame/` — treat as a separate feature category.
- Do not create any markdown files other than `progress/feature-parity.md`.
- Do not update `GAP-ANALYSIS.md` — it is deprecated in favour of `progress/feature-parity.md`.
- **`chathistory.php` is per-player chat history** (takes a player ID parameter), NOT a top-chatters leaderboard. It is implemented as `Chat/PlayerHistory`. Do not mark it as missing.
- **`livestats.php` has a dedicated .NET equivalent** at `Servers/Livestats` — do not mark it as missing.
- The `progress/feature-parity.md` file may still contain stale "since last report" sections from prior sprints — read only the most recent "New Since Last Report" section as the current delta; older sections are history.
- When calculating scores, use the per-feature completeness % values (not pure binary ✅/⚠️/❌) to stay consistent with the established scoring methodology (91% public / 85% admin / 90% overall baseline from 2026-04-24).