# Architecture

This document describes RomsCollect's solution layout, layers, and the
technical choices behind them. It is aimed at anyone reading or
contributing to the codebase.

## Solution layout

```
RomsCollect.slnx
src/RomsCollect/                 WPF application (net10.0-windows7.0)
  App.xaml(.cs)                  Startup: theme, database migration, DI wiring
  MainWindow.xaml(.cs)           Main window shell (menu, sidebar, grid/wheel/list, detail panel)
  AssemblyInfo.cs                Assembly-level attributes (WPF theme info)
  Data/Migrations/*.sql          Versioned, idempotent schema migration scripts (embedded resources)
  Models/                        Plain data classes mapped 1:1 to database rows
  Services/
    Database/                   SqliteConnectionFactory, DatabaseMigrator, one repository per entity
    Scanning/                   Collection scanner, ROM hashing (duplicate detection)
    Media/                      Media-to-game linking (RomsCollect's own media folder convention)
    Import/                     ROM folder and metadata library collection importers
    Emulation/                  Emulator launching, command-line template expansion
    PortableDataPaths.cs        Resolves every data directory relative to the executable
  ViewModels/                   MVVM view models (CommunityToolkit.Mvvm)
  Views/
    Controls/                   Reusable UserControls embedded in the main window
    Windows/                    Modal/secondary windows (Settings, About, edit dialogs, ...)
  Converters/                   IValueConverter implementations used from XAML
  Helpers/                      LocalizationService, {loc:LocalizedString} markup extension
  i18n/en.json                  All user-facing strings
  Assets/                       Logo and application icon (png/icon)
tests/RomsCollect.Tests/         xUnit test project, mirrors the src/ structure
data/                            Portable data root (see README.md)
docs/                            This documentation
scripts/                         Build and scaffolding scripts
```

## Layers and dependency direction

RomsCollect follows a strict MVVM separation. Dependencies only ever
point inward, toward `Models` and `Services`:

```
Views (XAML)  →  ViewModels  →  Services  →  Models
     ↑                              ↓
     └──────── Converters ──────────┘
```

- **Views** contain no business logic. A view's code-behind only wires up
  events the XAML binding system cannot express (file pickers, opening
  child windows, confirming destructive actions) and forwards them to the
  view model or to a service.
- **ViewModels** expose observable state and `[RelayCommand]`-driven
  actions (via `CommunityToolkit.Mvvm`'s source generators) to the view.
  They hold no direct UI references and can be constructed and tested
  without a running application (see `tests/`).
- **Services** are the only layer allowed to touch the database, the file
  system, or an external process. Each database-backed entity has its own
  repository class under `Services/Database` (`GameRepository`,
  `SystemRepository`, `GameOwnershipRepository`, ...), following a plain
  repository pattern with parameterized Dapper queries — no ORM, no
  change tracker, no magic.
- **Models** are plain classes with public settable properties, mapped
  1:1 to database columns by Dapper's default convention-based mapping.

## Data layer

- **SQLite** via `Microsoft.Data.Sqlite`, accessed through **Dapper** for
  lightweight, explicit SQL. There is no Entity Framework Core dependency
  — this was a deliberate choice to keep the data layer small, auditable,
  and fully understood rather than generated.
- **Migrations** are hand-written, idempotent `.sql` scripts under
  `Data/Migrations/`, embedded as assembly resources and applied in order
  by `DatabaseMigrator` against a `SchemaVersion` table. Before applying
  any pending migration to a database that already has data,
  `DatabaseMigrator` copies the file to a backup directory first.
- **Connections** are short-lived: `SqliteConnectionFactory.
  CreateOpenConnection()` opens one connection per repository call, used
  inside a `using` block. This keeps the code simple and avoids
  connection-lifetime bugs at the cost of some overhead, which is
  negligible for a single-user desktop application against a local
  SQLite file.

## Portability

Every path RomsCollect ever reads from or writes to is resolved through
`Services/PortableDataPaths.cs`, relative to `AppContext.BaseDirectory`
(the executable's own folder) unless a test overrides the base directory.
Nothing is written to `%APPDATA%`, `%LOCALAPPDATA%`, or the registry. The
project file enables `SelfContained` and `PublishSingleFile` so a
published build has no dependency on a .NET runtime being installed on
the target machine.

## Internationalization

All user-facing text lives in `i18n/en.json`, a flat key/value JSON file
copied next to the executable at build time (not embedded), so a
translation can be added or edited without recompiling. `Helpers/
LocalizationService.cs` loads the active language file and exposes it by
key, falling back to the raw key when a translation is missing (so a gap
is always visible rather than silently blank). XAML resolves strings
through the `{loc:LocalizedString Key}` markup extension (`Helpers/
LocalizedStringExtension.cs`); C# code resolves them through the static
`App.Localization["Key"]` indexer.

## Offline guarantee

RomsCollect performs no network I/O at runtime: no `HttpClient`, no
`WebClient`, no sockets, no update checker, no telemetry. The only
exception, by design, is `Process.Start` with `UseShellExecute = true`
used to open a user-entered download-link URL or a manual file in the
system's default browser/application — an explicit action the user
confirms, which hands off to a process entirely outside RomsCollect.

## UI framework

The UI is built with **WPF** and the **WPF-UI** (Lepo.co) component
library for Fluent/Mica theming, dark/light mode, and Fluent controls
(`FluentWindow`, `TitleBar`, `Button`, `TextBox`). Layout follows a game-browsing shell (menu bar, system sidebar with an
accent-bar selection indicator, a switchable grid/wheel/tabular-catalog
center pane, and a quick detail panel) combined with a full collector's
cataloging workflow (tabbed edit dialog, tabular catalog view with
configurable columns and grouping).

## Emulator launching

`Services/Emulation/GameLauncher.cs` resolves the default
`EmulatorProfile` configured for a game's system, validates that both the
emulator executable and the ROM file exist on disk before ever calling
`Process.Start` (returning an explicit, user-facing error instead of
failing silently otherwise), expands the profile's command-line template
(substituting `{RomPath}`, quoted to survive spaces and special
characters), and asynchronously awaits the emulator process's exit to
record a `PlayHistory` entry with the session's actual duration.
RomsCollect never downloads, bundles, or updates an emulator itself —
only pre-filled command-line templates for common emulators are provided.

## Testing

Unit tests (`tests/RomsCollect.Tests/`) use xUnit and mirror the
`Services/` structure. Repository and migration tests run against a
freshly migrated SQLite database in an isolated temporary directory per
test (`SqliteRepositoryTestBase`), cleaned up afterward; connection
pooling is explicitly cleared on disposal since
`Microsoft.Data.Sqlite` pools connections per connection string, which
would otherwise keep the temporary database file locked past the test's
end.
