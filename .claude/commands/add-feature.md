# Add Feature: $ARGUMENTS

Follow this workflow to add the feature described above to HLStatsX.NET.

## Step 1 — Check the PHP reference
Look up the relevant file(s) in `legacy/php/pages/` to understand:
- What the page/feature displays
- The SQL queries it runs (these map directly to EF Core queries)
- **Exactly which columns each table SELECTs** — only add entity properties for columns that actually appear in PHP SELECT/INSERT/UPDATE statements. Never guess or add columns speculatively; a missing column causes a runtime `MySqlException`. Check `admintasks/` files for INSERT/UPDATE statements if you need to confirm all real columns for a table.
- Any edge cases or special logic
- URL parameters it accepts

## Step 2 — Plan the layer changes needed
For any new data access, the change must flow through all layers:
1. **Core entity** — does a new entity or model record need adding?
2. **IRepository** — new method signature in `Core/Interfaces/Repositories/`
3. **Repository** — EF Core implementation in `Infrastructure/Repositories/`
4. **IService** — new method signature in `Core/Interfaces/Services/`
5. **Service** — pass-through in `Infrastructure/Services/`
6. **ViewModel** — new or updated view model in `Web/Models/ViewModels/`
7. **Controller** — new or updated action in `Web/Controllers/`
8. **View** — Razor view in `Web/Views/`

Only add layers that are actually needed. Don't add entities or methods that aren't used.

## Step 3 — Implement bottom-up
Start at Core (entities/interfaces), then Infrastructure (repository), then Web (controller + view). Build after each layer.

## Step 4 — Apply the standard patterns
- Pagination: use `PagedResult<T>` and the `_Pagination` partial
- Meter/bar graphs: always use `<meter min="0" max="100" low="25" high="50" optimum="75" value="...">` — all five attributes required for the green/yellow/red colour gradient (CSS is global in `site.css`; no inline colour styles)
- Sortable columns: **every table must be sortable** — all data columns (except Rank and bar-graph ratio columns) must be clickable sort links using `SortUrl(field)` / `Mark(field)` local functions with `@(expr)` explicit syntax
- Rank column: **every table must have a `Rank` column as its first column** — header `<td style="text-align:right;">Rank</td>`, data cell `<td style="text-align:right;">@rowNum</td>` using a sequential loop counter; never use "#" as the header
- Async: all methods must be `async Task<T>` with `CancellationToken ct = default`
- Game filter: always filter by `game` parameter — never hardcode a game code
- CSS: use existing classes (`data-table`, `data-table-head`, `bg1`/`bg2`, `btn-small`, `form-text`)

## Step 5 — Write XML documentation

Add `///` XML doc comments to **every public or interface-facing type and member** introduced by the feature:

- **Interface methods** (`IRepository`, `IService`) — one-line `<summary>` describing what it returns and any key parameters.
- **Model records / DTOs** — `<summary>` on the type; `<param>` on each constructor parameter if the meaning isn't obvious from the name.
- **Controller actions** — `<summary>` stating the route and what it renders; note any non-obvious defaults or validation.
- **Repository implementations** — `<summary>` + note any sentinel values (e.g. empty-string → display string conversions) or EF Core translation caveats.

Skip `///` on private helpers, trivial pass-throughs, and Razor views (they are not public API surface).

## Step 6 — Write unit tests

Add tests to the appropriate file in `tests/HLStatsX.NET.Tests/Controllers/` (or `Services/` for service logic). Cover:

1. **Happy path** — valid input returns the expected view name and a correctly-typed model.
2. **Default parameters** — omitting optional params produces the expected defaults in the view model (e.g. `sortBy = "Connects"`, `desc = true`, `page = 1`).
3. **Boundary / alternate inputs** — non-default sort field, ascending order, non-first page.
4. **Invalid sort guard** — an unrecognised `sortBy` value is normalised to the default (not passed through raw).
5. **Empty result** — service returns an empty `PagedResult` → view still returned (no exception).

Mock all service dependencies with Moq. Use FluentAssertions throughout. Follow the existing `AdminControllerTests` pattern (shared `_controller` + `_adminMock` set up in the constructor, `MakeHttpContext()` helper for the `ControllerContext`).

## Step 7 — Build and test
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```
If VS is running, stop the debug session before building.

## Step 6 — Update the parity tracking files

After a successful build, mark the feature as done in all four tracking files:

### `CLAUDE.md` — PHP Reference — Feature Map table
Find the row for this feature and update its Status column (e.g. `Not started` → `Done` or `Done (~90%)`).

### `.github/copilot-instructions.md` — PHP Reference table
Same update as CLAUDE.md.

### `progress/feature-parity.md`
1. Update the date line at the top.
2. Add a **"New Since Last Report"** section immediately after the existing ones, with a table row for the feature just implemented.
3. Move the feature row from the **❌ Not Yet Implemented** section to the **✅ Fully Implemented** section (or **⚠️ Partially Implemented** if gaps remain).
4. Recalculate admin/public/overall parity percentages and update the Overall Parity table.
5. Update the **Priority Queue** — remove or reprioritise the feature just completed.

### `progress/html/feature-parity.html`
1. Update the date badge.
2. Add a **"New Since Last Report"** `<div class="section">` card with a `.new-card` entry for the feature.
3. Update the admin/overall progress bar numbers (`<div class="pct">` and `style="width:XX%"`).
4. Move the feature from the ❌ table to the ✅ table (or update completeness).
5. Update the priority queue `<ul class="priority-list">`.

> Preserve the existing CSS `<style>` block verbatim — only update HTML content.

## Step 7 — Update CLAUDE.md patterns
Add any new patterns, gotchas, or PHP reference files discovered during implementation to the relevant section of `CLAUDE.md`.

## Step 8 — Suggest a commit
Once the feature is complete and tests pass, suggest a commit message following the convention:
`Add <feature name>` or `Implement <feature name>`
