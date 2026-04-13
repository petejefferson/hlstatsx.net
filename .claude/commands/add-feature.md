# Add Feature: $ARGUMENTS

Follow this workflow to add the feature described above to HLStatsX.NET.

## Step 1 — Check the PHP reference
Look up the relevant file(s) in `OriginalPHP/pages/` to understand:
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
- Sortable columns: use `SortUrl(field)` / `Mark(field)` local functions with `@(expr)` explicit syntax
- Async: all methods must be `async Task<T>` with `CancellationToken ct = default`
- Game filter: always filter by `game` parameter — never hardcode a game code
- CSS: use existing classes (`data-table`, `data-table-head`, `bg1`/`bg2`, `btn-small`, `form-text`)

## Step 5 — Build and test
```bash
dotnet build
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```
If VS is running, stop the debug session before building.

## Step 6 — Update CLAUDE.md
Add any new patterns, gotchas, or PHP reference files discovered during implementation to the relevant section of `CLAUDE.md`.

## Step 7 — Suggest a commit
Once the feature is complete and tests pass, suggest a commit message following the convention:
`Add <feature name>` or `Implement <feature name>`
