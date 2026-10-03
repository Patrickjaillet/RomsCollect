# Third-Party Notices

RomsCollect is distributed under the GNU General Public License v3.0 or
later (GPL-3.0-or-later). See `LICENSE` at the repository root for the
full license text.

This file lists every third-party dependency bundled with or linked by
RomsCollect, together with its license. **Every dependency listed here
must be compatible with GPL-3.0-or-later distribution.** Before adding
a new dependency, verify its license here and update this table in the
same commit.

## Compatibility policy

- Permissive licenses (MIT, BSD-2/3-Clause, Apache-2.0, ISC) are
  compatible and may be used freely, statically or dynamically linked.
- LGPL-2.1/3.0 and MPL-2.0 dependencies are compatible when used as a
  separate, dynamically linked component (no static linking that would
  merge their source into a GPL-incompatible combined work).
- **Never accepted:** any dependency under GPL-2.0-only, AGPL, SSPL,
  BUSL, a proprietary/commercial EULA, or a "non-commercial only"
  clause, unless it is used only as an optional, out-of-process external
  tool that RomsCollect never links against.
- Any ROM, BIOS, box art, screenshot, video, or manual is **user-supplied
  content** and is never bundled with the application; RomsCollect
  ships with an empty `data/Roms` and `data/Media` tree.

## .NET / NuGet packages

| Package | Version | License | Usage | Compatible |
| --- | --- | --- | --- | --- |
| WPF-UI | 4.3.0 | MIT | Fluent UI controls, theming (`FluentWindow`, `TitleBar`, dark theme) | Yes |
| WPF-UI.Abstractions | 4.3.0 (transitive) | MIT | Shared contracts for WPF-UI | Yes |
| Microsoft.Data.Sqlite | 10.0.12 | MIT | SQLite ADO.NET provider, local database access | Yes |
| Microsoft.Data.Sqlite.Core | 10.0.12 (transitive) | MIT | Core of the SQLite provider | Yes |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.12 (transitive) | Apache-2.0 | Native SQLite bundle | Yes |
| SQLitePCLRaw.core | 2.1.12 (transitive) | Apache-2.0 | SQLite P/Invoke core | Yes |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.12 (transitive) | Public Domain / Apache-2.0 | Native SQLite binary | Yes |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.12 (transitive) | Apache-2.0 | SQLite provider glue | Yes |
| Dapper | 2.1.89 | Apache-2.0 | Lightweight SQL mapping over ADO.NET | Yes |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | MVVM helpers (`ObservableObject`, `RelayCommand`, source generators) | Yes |

None of the above dependencies perform any network access; all are used
strictly as local, offline libraries. Re-run
`dotnet list package --include-transitive` after every dependency change
and update this table in the same commit.

Test-only packages (`coverlet.collector`, `Microsoft.NET.Test.Sdk`,
`xunit`, `xunit.runner.visualstudio`, referenced only by
`tests/RomsCollect.Tests`) are intentionally omitted from this table:
they are never linked into or shipped with the published `RomsCollect.exe`
and therefore carry no distribution-license obligation.

This table was cross-checked against `src/RomsCollect/RomsCollect.csproj`
on 2026-09-26 — the four direct package references and their exact pinned
versions match this list.

## Native / bundled tools

RomsCollect does not bundle any emulator, BIOS, or ROM. It only stores
**user-configured launch paths** to emulators the user already has
installed. No emulator binary is redistributed with this project.

`Services/Input/XInputInterop.cs` P/Invokes `xinput1_4.dll` for gamepad
navigation in the wheel/carousel view (`Services/Input/GamepadService.cs`).
This is a system DLL that ships with Windows 10/11 itself — nothing is
bundled, downloaded, or redistributed by RomsCollect; it is loaded from
the OS at runtime exactly like any other Win32 API, with no network
access involved.

## Fonts / icons

| Asset | Source | License | Compatible |
| --- | --- | --- | --- |
| _(to be filled in when the icon and UI iconography are chosen)_ | | | |

Any icon font or icon pack used in the UI (e.g. Fluent System Icons)
must be listed here with its exact license before being embedded in
`Assets/`.

## Audit checklist (kept in sync with ROADMAP.md constraints)

- [x] Every row above has a confirmed license (not "likely MIT").
- [x] No dependency requires network access at runtime (violates the
      100% offline constraint).
- [x] No dependency pulls in a GPL-2.0-only or AGPL transitive package.
- [x] `dotnet list package --include-transitive` reviewed after every
      dependency change, and this file updated accordingly.
- [x] Table cross-checked against `RomsCollect.csproj` direct
      `PackageReference` entries (versions match exactly).
