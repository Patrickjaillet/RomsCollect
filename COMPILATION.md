# COMPILATION — RomsCollect

This document explains how to build RomsCollect from source, for both
development and the final portable release. It compiles every
build-related fact found in the repository (project files, build scripts,
and the roadmap's build-related decisions) into a single English
reference.

## 1. Requirements

- **.NET 10 SDK**, with the WPF / Windows Desktop workloads available
  (WPF compilation requires either a Windows machine, or a .NET SDK
  installation that includes the Windows Desktop targeting pack).
- **Target platform:** Windows 10/11, x64 only (`win-x64`). RomsCollect is
  a WPF application and does not target any other OS or architecture.
- No other external tool is required: there is no Node.js, no separate
  database engine, and no installer toolchain — SQLite is used through
  the `Microsoft.Data.Sqlite` NuGet package only.

## 2. Solution layout

```
RomsCollect.slnx                        Solution file (two projects)
  src/RomsCollect/RomsCollect.csproj    Application (WPF, net10.0-windows7.0)
  tests/RomsCollect.Tests/*.csproj      Unit/integration tests (xUnit)
```

- `RomsCollect.slnx` is the modern XML solution format (`.slnx`), used
  instead of the legacy `.sln`.
- The app project's `TargetFramework` is `net10.0-windows7.0`
  (`SupportedOSPlatformVersion` 7.0), `OutputType` is `WinExe`,
  `UseWPF` is `true`.

## 3. NuGet dependencies

Declared directly in `src/RomsCollect/RomsCollect.csproj`:

| Package | Version | License |
|---|---|---|
| WPF-UI (Lepo.co) | 4.3.0 | MIT |
| Microsoft.Data.Sqlite | 10.0.12 | MIT |
| Dapper | 2.1.89 | Apache-2.0 |
| CommunityToolkit.Mvvm | 8.4.2 | MIT |

Declared in `tests/RomsCollect.Tests/RomsCollect.Tests.csproj`:

| Package | Version |
|---|---|
| Microsoft.NET.Test.Sdk | 17.14.1 |
| xunit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.4 |
| coverlet.collector | 6.0.4 |

Every dependency's license is checked before it is added, and logged in
`THIRD_PARTY_NOTICES.md` at the repository root; only GPL-3.0-compatible
licenses are accepted. Do not add a new package without updating that
file first.

## 4. Building for development

From the repository root, with the .NET 10 SDK on the `PATH`:

```bash
dotnet build RomsCollect.slnx
```

To run the application directly (debug, framework-dependent):

```bash
dotnet run --project src/RomsCollect/RomsCollect.csproj
```

To run the test suite:

```bash
dotnet test RomsCollect.slnx
```

## 5. Portable release build

RomsCollect is compiled as a **self-contained, single-file, portable**
executable — no installer, no dependency on a .NET runtime installed on
the target machine, and no writes outside its own folder (no
`%APPDATA%`, no registry). This is enforced both by MSBuild properties in
`RomsCollect.csproj` and by the dedicated build scripts below.

### 5.1 Relevant `.csproj` properties

```xml
<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<InvariantGlobalization>false</InvariantGlobalization>
<ApplicationIcon>Assets\icon\RomsCollect.ico</ApplicationIcon>
```

In `Release` configuration only, PDB generation is disabled:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
</PropertyGroup>
```

By default the compiler embeds the full local build-machine source path
(e.g. `C:\Users\<name>\...`) into the PDB. Since the portable release is
meant to be redistributed, this would leak the builder's Windows username
inside the shipped files. Disabling PDB generation for `Release` removes
that path with no functional impact — the `Debug` configuration used
during development is unaffected and still produces a full PDB for
debugging. This is also why `build_portable.ps1` / `.sh` grep the output
folder for `C:\Users\` after publishing: it is a defensive check for
exactly this kind of leak.

- `i18n/**/*.json` is copied next to the executable at
  `i18n/<file>.json` (`CopyToOutputDirectory=PreserveNewest`).
- `Assets/png/logo.png` and `Assets/icon/RomsCollect.ico` are copied next
  to the executable at `assets/png/logo.png` and
  `assets/icon/RomsCollect.ico` — **not** embedded in the assembly — so
  the About window can load them at runtime from a path relative to
  `AppContext.BaseDirectory`, per the 100%-portable constraint.
- SQL migration scripts (`Data/Migrations/*.sql`) are embedded as
  resources in the assembly.

### 5.2 Build scripts

Two equivalent, reproducible build scripts are provided:

- `scripts/build_portable.ps1` (Windows / PowerShell)
- `scripts/build_portable.sh` (Bash)

Usage:
- powershell -ExecutionPolicy Bypass -File .\build_portable.ps1

```powershell
scripts\build_portable.ps1                # Release build (default)
scripts\build_portable.ps1 -Configuration Debug
```

```bash
scripts/build_portable.sh                 # Release build (default)
scripts/build_portable.sh Debug           # other configuration
```

Both scripts perform the same steps:

1. Check that `dotnet` is on the `PATH` and that
   `src/RomsCollect/RomsCollect.csproj` exists; abort with an error
   otherwise.
2. Remove any previous `dist/RomsCollect-win-x64/` output.
3. Run:
   ```bash
   dotnet publish src/RomsCollect/RomsCollect.csproj \
     --configuration <Configuration> \
     --runtime win-x64 \
     --self-contained true \
     --output <temp publish dir> \
     -p:PublishSingleFile=true \
     -p:IncludeNativeLibrariesForSelfExtract=true
   ```
4. Assemble `dist/RomsCollect-win-x64/` with only what should ship:
   - `RomsCollect.exe`
   - `i18n/` (copied from the publish output)
   - `assets/` (copied from the publish output)
   - an empty portable `data/` tree: `data/Database/`, `data/Bios/`,
     `data/Saves/`, `data/Screenshots/`, `data/Themes/`, `data/Logs/`,
     `data/Backup/`
   - no debug `.pdb` files and no intermediate build artifacts
   - `data/Roms/<system>/` and `data/Media/<system>/` are **not**
     pre-created: the system list is extensible from the UI (Settings >
     Systems), and these folders are created by the application itself
     on first launch or when a system is added.
5. Run output checks and print warnings (not hard failures, except for
   the missing-executable case) if:
   - `RomsCollect.exe` is missing from the dist folder (this one **does**
     abort the script)
   - `i18n/en.json` is missing
   - `assets/png/logo.png` or `assets/icon/RomsCollect.ico` is missing
   - any file in the dist folder contains a hard-coded user path
     (`C:\Users\...`), which would violate the portability/offline
     constraint
6. Print the final folder size and the command to zip it for delivery,
   e.g. `Compress-Archive` (PowerShell) or `zip -r` (Bash).

> Note: the Bash script can run the `dotnet publish` step on any OS with
> the .NET 10 SDK installed, but compiling a WPF project still requires
> the Windows Desktop targeting pack; running it on a non-Windows SDK
> without that pack will fail at the `dotnet publish` step.

### 5.3 Manual equivalent

If you need to publish without the helper scripts, the single relevant
command is:

```bash
dotnet publish src/RomsCollect/RomsCollect.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output <output-folder> \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

then manually copy `RomsCollect.exe`, `i18n/`, and `assets/` from
`<output-folder>` into your distribution folder, and create the empty
`data/` subfolders listed above.

## 6. SPDX header check

Every `.cs` and `.xaml` source file must start with:

```
SPDX-License-Identifier: GPL-3.0-or-later
```

This is verified (and can be auto-fixed) with:

```bash
scripts/check_spdx.sh          # check mode
scripts/check_spdx.sh --fix    # add missing headers
```

The script has already been run once across the full source tree
(105 `.cs`/`.xaml` files): all `.cs` files already had the header, and it
was added to the 13 `.xaml` files that were missing it, preserving the
UTF-8 BOM at the top of `App.xaml`.

## 7. Directory scaffolding (development setup)

The portable data directory tree (see `ROADMAP.md` for the full layout:
`data/Roms/`, `data/Media/`, `data/Database/`, `data/Bios/`,
`data/Saves/`, `data/Screenshots/`, `data/Themes/`, `data/Logs/`,
`data/Backup/`, `assets/png/`, `assets/icon/`, `i18n/`) was created once
in the source tree with:

```bash
scripts/scaffold_dirs.sh
```

This script only needs to be re-run if the directory tree is deleted or
if the project is re-scaffolded from scratch; it is not part of the
normal build/publish pipeline described above.

## 8. Pre-release verification checklist (still open)

Per the roadmap, the following build-related checks are not yet
completed and remain required before a v1.0 tag:

- [ ] Run `scripts/build_portable.ps1` / `.sh` end-to-end on a real
      Windows machine and confirm `dist/RomsCollect-win-x64/` is produced
      correctly (this has not yet been exercised outside of script
      review).
- [ ] Verify the executable starts with no missing dependency on a clean
      Windows 10 **and** Windows 11 machine that has no .NET SDK
      installed.
- [ ] Confirm the main window's icon (title bar / taskbar, via
      `Window.Icon`) renders correctly from the portable build output —
      this has only been wired for the executable icon
      (`<ApplicationIcon>`) and the About window so far.
- [ ] Zip the verified `dist/RomsCollect-win-x64/` folder into the final
      release archive and test it on a clean machine (Phase 10).
