# Contributing to HLStatsX.NET

Welcome, and thank you for your interest in contributing. This document covers the architecture, conventions, and processes you need to know before writing or reviewing code.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Solution Structure](#solution-structure)
3. [Architecture Rules](#architecture-rules)
4. [Database](#database)
5. [Coding Standards](#coding-standards)
6. [Key Patterns](#key-patterns)
7. [Testing](#testing)
8. [The PHP Reference](#the-php-reference)
9. [Commit Style](#commit-style)
10. [Common Gotchas](#common-gotchas)

---

## Project Overview

HLStatsX.NET is a .NET 10 rewrite of HLStatsX Community Edition — a real-time player and clan statistics system for Half-Life engine games (Counter-Strike, Day of Defeat: Source, Team Fortress 2, etc.). The goal is **complete feature parity** with the original PHP implementation.

The original PHP source lives in `legacy/php/` and is the authoritative specification. If you are unsure what a feature should do, read the PHP code first.

---

## Solution Structure

```
src/
  HLStatsX.NET.Core/           # Domain entities, interfaces, shared models
  HLStatsX.NET.Infrastructure/ # EF Core repositories, services, DbContext
  HLStatsX.NET.Web/            # ASP.NET Core MVC controllers, Razor views, view models
tests/
  HLStatsX.NET.Tests/          # xUnit unit tests (controllers, services)
legacy/php/                    # Original PHP source — the spec, do not modify
```

---

## Architecture Rules

The project enforces a strict three-layer dependency order: **Core → Infrastructure → Web**.

| Layer | Project | Allowed dependencies |
|---|---|---|
| Core | `HLStatsX.NET.Core` | None — no EF, no ASP.NET Core |
| Infrastructure | `HLStatsX.NET.Infrastructure` | Core only |
| Web | `HLStatsX.NET.Web` | Core + Infrastructure (via DI) |

### What goes where

- **Core** — entities, repository interfaces, service interfaces, shared model records. No EF or HTTP dependencies.
- **Infrastructure** — EF Core implementations of the Core interfaces. All SQL queries live here.
- **Web** — MVC controllers, Razor views, view models. Controllers call **services only** — never repositories directly.
- **Services** — thin wrappers over repositories. Only add logic here if it genuinely belongs between the HTTP layer and the database (e.g. password hashing, permission checks). Do not duplicate database logic.

### Implementing a new feature

Work bottom-up through the layers:

1. **Entity** — add or update the entity in `Core/Entities/`. Map only columns that actually exist in the database (check the PHP `SELECT`/`INSERT` statements to verify — adding a non-existent column causes a runtime `MySqlException`).
2. **Repository interface** — add the method signature to the appropriate `IXxxRepository` in `Core/Interfaces/Repositories/`.
3. **Service interface** — expose the method via the appropriate `IXxxService` in `Core/Interfaces/Services/`.
4. **Repository implementation** — write the EF Core query in `Infrastructure/Repositories/`.
5. **Service implementation** — delegate to the repository in `Infrastructure/Services/`.
6. **View model** — define a view-specific DTO in `Web/Models/ViewModels/`.
7. **Controller** — call the service and map to the view model.
8. **View** — write the Razor view.

---

## Database

- MySQL via `Pomelo.EntityFrameworkCore.MySql`.
- Connection string key: `HLStats` in `appsettings.json`.
- **EF Core is pinned to 9.x** — Pomelo has no 10.x release yet. Do not upgrade EF Core past `9.0.*`.
- `HLStatsDbContext` is registered as an `IDbContextFactory<HLStatsDbContext>` **singleton**. Each repository method calls `_factory.CreateDbContext()` to get a short-lived context. This is required because `DbContext` is not thread-safe and the player profile page fires around 25 concurrent queries via `Task.WhenAll`.
- `QueryTrackingBehavior.NoTracking` is set globally — do not call `.AsTracking()` unless you have a specific reason.

### Entity column mapping

Only add properties that correspond to columns visible in the PHP SQL queries for that table. Adding a property with no backing column causes a runtime exception. When in doubt, check the PHP `SELECT` and `INSERT` statements in `legacy/php/pages/`.

---

## Coding Standards

### Async

All service and repository methods must be `async Task<T>` with a `CancellationToken ct = default` parameter:

```csharp
Task<Player?> GetByIdAsync(int id, CancellationToken ct = default);
```

Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`. Always `await`.

### Null handling

Return `null` from repository and service methods when a single item is not found (e.g. `GetByIdAsync` → `Player?`). Controllers handle the null case and return `NotFound()`.

### No speculative abstractions

Do not add helper classes, base classes, or extension methods for a single use. Add the abstraction when the second use arrives.

### Comments

Comment only where the name alone does not tell the full story. Use XML `<summary>` tags on public types and members. Do not write comments that just restate the code.

### Multi-game support

The app must work with every Half-Life engine game. Always filter by the `game` parameter in every query — never hardcode a game code.

---

## Key Patterns

### Pagination

All paged queries return `PagedResult<T>`. Use the `_Pagination` partial in views:

```cshtml
<partial name="_Pagination" model="@PaginationModel.From(Model.Result, p => Url.Action("Index", new { page = p })!)" />
```

### Sortable column headers

Every list page defines these local Razor functions:

```cshtml
@{
    string SortUrl(string field)
    {
        bool nextDesc = Model.SortBy == field ? !Model.Descending : true;
        return Url.Action("Index", new { sortBy = field, desc = nextDesc, game = Model.Game })!;
    }
    string Mark(string field) => Model.SortBy == field ? (Model.Descending ? " ▼" : " ▲") : "";
}
```

All data columns (except Rank and bar-graph columns) must be sortable. Use `@(SortUrl("field"))` and `@(Mark("field"))` — the explicit `@(expr)` form is always required inside HTML attributes.

**Rank column** — every table must have a Rank column as its first column. Header text is `Rank`, left-aligned.

### Meter elements

Percentage bar graphs must use the `<meter>` element with all five attributes:

```html
<meter min="0" max="100" low="25" high="50" optimum="75" value="@percent"></meter>
```

Omitting `low`, `high`, or `optimum` disables the green/yellow/red colour gradient. Do not add inline `style` — the colours are defined globally in `wwwroot/css/site.css`.

### CSS classes

| Class | Purpose |
|---|---|
| `data-table` | Main stats tables |
| `data-table-head` | Header row |
| `bg1` / `bg2` | Alternating row colours |
| `form-text` | Inline text inputs |
| `btn-small` | Small action buttons |
| `stats-table` | Secondary tables on profile pages |

---

## Testing

- Test framework: xUnit with Moq and FluentAssertions.
- Tests live in `tests/HLStatsX.NET.Tests/`.
- **Unit tests** mock all injected services. Controllers are tested against mocked services — never against real repositories.
- **Repository tests** require a live MySQL connection and are excluded from CI. They are in `tests/.../Repositories/` and must be excluded with the filter `FullyQualifiedName!~RepositoryTests` when no database is available.

Run tests (excluding repository tests):

```bash
dotnet test --filter "FullyQualifiedName!~RepositoryTests"
```

Run a specific test class:

```bash
dotnet test --filter "FullyQualifiedName~PlayersControllerTests"
```

---

## The PHP Reference

Before implementing any feature, read the relevant PHP file in `legacy/php/pages/`. The `legacy/` directory is not committed - see [Legacy Reference Source](README.md#legacy-reference-source) in the README for how to fetch it. The PHP code defines the correct SQL queries, URL parameters, edge cases, and display logic. The .NET version should match the PHP behaviour unless there is a clear technical reason not to.

The live PHP site is available at **https://tft.nervaware.co.uk/stats/hlstats.php** for visual reference.

---

## Commit Style

Commits follow conventional commit style. Keep the subject line short (≤72 characters) and use the imperative mood:

```
Add weapon detail page

Implements the weapon detail page matching PHP's weaponinfo.php.
Includes top killers leaderboard, headshot ratio bar, and sortable columns.
```

Use a blank line between the subject and the body. Reference issue numbers where applicable.

---

## Common Gotchas

### EF Core: no `let` after `group...into`

Using a `let` clause after a `group...into` in LINQ query syntax forces EF Core to carry the raw group object into the `Select`, producing an untranslatable expression. Use method syntax instead:

```csharp
// Wrong — EF Core cannot translate this
group f by f.Weapon into g
let code = g.Key
...

// Right — aggregate fully in Select first
.GroupBy(f => f.Weapon)
.Select(g => new { Code = g.Key, Count = g.Count() })
.OrderByDescending(x => x.Count)
.Select(x => x.Code)
.FirstOrDefaultAsync(ct)
```

### Razor: always use `@(expr)` in attributes

Implicit `@expr` breaks when the expression contains double-quoted strings. Always use the explicit `@(expr)` form inside HTML and tag-helper attributes:

```cshtml
<!-- Wrong -->
<a href="@Url.Action("Index", new { page = p })">

<!-- Right -->
<a href="@(Url.Action("Index", new { page = p }))">
```

### Razor: pre-compute variables before HTML output

`@{ ... }` code blocks inside `@if {}` must appear before any HTML output in that scope. Declare variables at the top of a block, not after a closing tag.

### Sort direction arrows

Use literal Unicode characters in C# strings (`▼` `▲`), not HTML entities via `Html.Raw()`.

### DbContext factory pattern

Always call `_factory.CreateDbContext()` at the start of each repository method. Do not store or reuse a context across calls — it is not thread-safe and will fail under concurrent `Task.WhenAll` loads.

### `appsettings.Development.json`

This file contains the local database password and must never be committed. It is listed in `.gitignore`.
