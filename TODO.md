# HLStatsX.NET — Future Work

This file tracks significant planned enhancements beyond the current PHP feature-parity goal.

---

## 1. Multi-Database Platform Support (MSSQL & PostgreSQL)

### Current State

All four projects (Web, Infrastructure, Daemon, Awards) are coupled to MySQL via `Pomelo.EntityFrameworkCore.MySql 9.0.0`. EF Core is pinned at 9.x because Pomelo has no 10.x release yet. All database-specific work is in `src/HLStatsX.NET.Infrastructure/`.

### Blockers / MySQL-specific code to address

| Location | Issue | Affects |
|---|---|---|
| All `Program.cs` files | `UseMySql(connectionString, ServerVersion.AutoDetect(...))` | Web, Daemon, Awards |
| `AdminRepository.cs` | Backtick identifier quoting `` `tableName` `` | All raw SQL statements |
| `AdminRepository.cs:722-723` | `OPTIMIZE TABLE` / `ANALYZE TABLE` — MySQL-only | MSSQL has no equivalent; PostgreSQL uses `VACUUM ANALYZE` |
| `AdminRepository.cs:831-832` | `DELETE FROM \`t\` USING \`t\` INNER JOIN ...` — MySQL multi-table delete syntax | Needs rewriting as subquery/CTE |
| `AdminRepository.cs:870-871` | `DELETE c FROM ... LEFT JOIN ...` — MySQL alias delete | Compatible with MSSQL; needs rewrite for PostgreSQL |
| `AdminRepository.cs:963-1001` | `ALTER DATABASE ... DEFAULT CHARACTER SET ... COLLATE ...` — MySQL-only collation management | Entire `ResetCollations` feature is MySQL-specific |
| `appsettings.json` (all projects) | `CharSet=utf8mb4` connection string parameter | MySQL-only parameter |
| `.csproj` files | `Pomelo.EntityFrameworkCore.MySql` package reference | All four projects |

### Work Required

#### Step 1 — Configuration abstraction (~4 hours)
- Add a `DatabaseProvider` config option to `appsettings.json`: `"MySql"` | `"SqlServer"` | `"PostgreSql"`
- Read it in each `Program.cs` and conditionally call `UseMySql` / `UseSqlServer` / `UseNpgsql`
- Document all three connection string formats in `README.md` and `appsettings.json` comments

#### Step 2 — NuGet packages (~2 hours)
- Add `Microsoft.EntityFrameworkCore.SqlServer` (MSSQL support)
- Add `Npgsql.EntityFrameworkCore.PostgreSQL` (PostgreSQL support)
- With Pomelo no longer the sole provider, EF Core can be upgraded to 10.x for non-MySQL targets
- Add packages to all four project `.csproj` files; keep Pomelo for MySQL path

#### Step 3 — SQL dialect abstraction (~8–12 hours)
- Introduce an `ISqlDialect` interface (in Core or Infrastructure) with provider-specific implementations:
  - `QuoteIdentifier(string name)` — backticks (MySQL), square brackets (MSSQL), double-quotes (PostgreSQL)
  - `OptimizeTables(string tableList)` — `OPTIMIZE TABLE` vs `VACUUM ANALYZE` vs no-op
  - `DeleteJoinSyntax(...)` — provider-specific multi-table delete helpers
- Register the correct implementation based on `DatabaseProvider` config
- Update all `ExecuteSqlRawAsync` call sites in `AdminRepository` to use `ISqlDialect`

#### Step 4 — Feature flags for MySQL-only tools (~2 hours)
- Inject `DatabaseProvider` into admin views
- Conditionally hide the **Reset DB Collations** menu item and page when not running MySQL
- Show a "MySQL only" notice if accessed directly on another provider

#### Step 5 — EF Core model configuration review (~3 hours)
- Audit `HLStatsDbContext.OnModelCreating` for any MySQL-specific fluent API calls
- Verify column type mappings (`tinyint`, `int unsigned`, `text`) translate correctly for each provider
- Unsigned integers are not natively supported in MSSQL/PostgreSQL — EF Core maps them to `long`; confirm no overflow risk

#### Step 6 — Migrations (~4–6 hours)
- No EF migrations exist today (schema is maintained by the legacy PHP installer)
- If migrations are introduced, separate migration bundles are needed per provider
- Consider `--provider` CLI argument to select the migration project at generation time

#### Step 7 — Integration testing (~8–10 hours per provider)
- Spin up MSSQL (Docker: `mcr.microsoft.com/mssql/server`) and PostgreSQL containers
- Run the full repository test suite against each provider
- Verify admin bulk operations, reset stats, cleanup, and daemon event handling end-to-end

### Estimated Effort

| Task | Hours |
|---|---|
| Config abstraction + packages | ~6 |
| SQL dialect abstraction | ~10–14 |
| Feature flags | ~2 |
| Model/type review | ~3 |
| Migrations | ~4–6 |
| MSSQL integration testing | ~8–10 |
| PostgreSQL integration testing | ~8–10 |
| **Total** | **~41–51 hours** |

---

## 2. Localisation (French, Spanish, German, Russian)

### Current State

No localisation infrastructure exists. All ~1,700 user-visible strings are hardcoded English in 121 Razor view files, 4 layout files, and scattered controller `TempData`/`ViewData` calls. Zero `.resx` files, no `IStringLocalizer` usage, no culture middleware.

### Work Required

#### Phase 1 — Infrastructure (~20–25 hours)
- Register localisation services in all `Program.cs` files:
  ```csharp
  builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
  builder.Services.AddControllersWithViews()
      .AddViewLocalization()
      .AddDataAnnotationsLocalization();
  ```
- Configure `RequestLocalizationOptions` with supported cultures: `en`, `fr`, `es`, `de`, `ru`
- Add `app.UseRequestLocalization()` middleware
- Implement culture selection: cookie-based (recommended) plus `Accept-Language` header fallback
- Add a language switcher UI component to `_Layout.cshtml`
- Create the `Resources/` folder structure:
  ```
  Resources/
    SharedResources.resx          ← navigation, pagination, common buttons
    Views/Players/Index.resx      ← per-view resource files
    Views/Players/Profile.resx
    Views/Clans/Index.resx
    ... (one per view folder)
    Views/Admin/Admin.resx
    Controllers/AdminController.resx  ← TempData success/error messages
  ```

#### Phase 2 — View string extraction (~80–100 hours)
- Replace all hardcoded strings in 121 `.cshtml` files with `@Localizer["key"]` calls
- Inject `IViewLocalizer` at the top of each view: `@inject IViewLocalizer Localizer`
- For shared strings (Prev/Next, Search, Save, Delete, etc.) use `IHtmlLocalizer<SharedResources>`
- Update all 4 layout files (`_Layout`, `_AdminLayout`, `_InGameLayout`, `_LivestatsLayout`)
- Approximate effort: 30–40 min per view file × 121 files

**Estimated string volumes by area:**

| Area | ~Strings |
|---|---|
| Admin panel (52 views) | 400–500 |
| Public leaderboards (69 views) | 750–900 |
| Shared layouts + partials | 100–120 |
| Controller messages (TempData) | 50–80 |
| **Total** | **~1,300–1,600** |

#### Phase 3 — Controller & service strings (~15–20 hours)
- Inject `IStringLocalizer<AdminController>` (and others) into controllers
- Migrate hardcoded `TempData["Success"]` / `TempData["Error"]` strings to localised keys
- Localise `ViewData["Title"]` page titles set in controllers
- Create `Controllers/AdminController.resx` (and others) with string keys

#### Phase 4 — Translation content (~90–140 hours)
- English baseline `.resx` files produced by Phases 1–3
- Use Google Translate API or Azure Translator for an initial machine-translation pass (~$20–30 for all 4 languages)
- Professional or community review pass per language
- **Russian note:** requires 3-form plural rules — use `IStringLocalizerFactory` with custom plural helpers for any count-bearing strings (e.g. "1 kill", "2 kills", "5 kills")

| Language | MT pass | Review | Total |
|---|---|---|---|
| French | ~4 hrs | ~12–16 hrs | ~16–20 hrs |
| Spanish | ~4 hrs | ~12–16 hrs | ~16–20 hrs |
| German | ~4 hrs | ~14–18 hrs | ~18–22 hrs |
| Russian | ~4 hrs | ~16–20 hrs | ~20–24 hrs |
| **Total** | ~16 hrs | ~54–70 hrs | **~70–86 hrs** |

#### Phase 5 — Database-driven content (~10–15 hours)
Action names, weapon names, rank/ribbon descriptions, and award names are stored in the database. These are not covered by `.resx` files. Options:
- Add `name_fr`, `name_es`, `name_de`, `name_ru` columns to the relevant tables (`hlstats_Games_Actions`, `hlstats_Weapons`, `hlstats_Ranks`, etc.) — simple but schema-heavy
- Introduce a generic `hlstats_Translations(entity_type, entity_id, language, text)` table — cleaner but requires a translation admin UI
- Recommend the translations table approach; scope as a separate sub-task

#### Phase 6 — Testing & QA (~15–20 hours)
- Test all views in each locale (automated smoke tests + manual review)
- Check for layout overflow — translated strings are often 20–40% longer than English
- Verify number and date formatting respects locale (EF Core returns raw numbers; formatting is in views)
- Confirm RTL readiness is not broken (not required for these 4 languages but worth checking for future-proofing)

### Total Estimated Effort

| Phase | Hours |
|---|---|
| 1 — Infrastructure | 20–25 |
| 2 — View extraction | 80–100 |
| 3 — Controller strings | 15–20 |
| 4 — Translations (4 languages) | 70–86 |
| 5 — Database content | 10–15 |
| 6 — Testing & QA | 15–20 |
| **Total** | **~210–266 hours** |

Minimum viable path (infrastructure + extraction + one language): ~3–4 weeks.  
Full four-language release: ~6–7 weeks.
