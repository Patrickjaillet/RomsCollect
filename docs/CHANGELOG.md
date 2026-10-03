# Changelog

All notable changes to RomsCollect are documented in this file. The
format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.0] - 2026-10-03

### Added

- Scan Collection window: review added/updated/unchanged/error entries
  and explicitly apply them, scan every configured system or just one,
  with live per-system progress on a background thread.
- Advanced search filter panel (genre, release year, minimum rating,
  completeness, favorites/completed), combinable with the existing
  search text and sidebar selection.
- Full-screen toggle and a "last scan" notification in the top status
  area.
- User-defined collections in the sidebar (create/delete), with a
  right-click context menu on a game card to add or remove it from any
  collection.
- `Settings > Display` toggle to hide the per-system game count in the
  sidebar.
- Gamepad navigation (Xbox-compatible, via XInput) in the wheel/carousel
  view: D-Pad/left stick to move, A to launch the selected game.
- Clickable filmstrip thumbnails in the quick detail panel, switching
  the main preview image.
- A second, detailed Information block in the quick detail panel
  (rating, genre, play mode, region, status, imported ROM, portable).
- `Games.PlayMode` and `Games.IsPortable` catalog fields, editable from
  the game-edit dialog's Main tab.
- Integrated video player (play/pause, scrub bar) in the quick detail
  panel and the full details page, used automatically when a game has a
  "Video" media asset.
- Direct keyboard shortcuts: `F5` (Play), `Ctrl+D` (toggle Favorite),
  `Ctrl+T` (toggle Completed), plus a `Menu > Keyboard Shortcuts...`
  summary window.
- Full details page gained a media gallery, an editable 0–10 rating, and
  a working Play button (previously missing entirely).
- Bulk actions in the tabular catalog view for checked rows: delete, add
  a tag, set Collection Status, export to CSV.
- Collapsible categories in the "Select Columns" dialog; draggable,
  persisted column order in the catalog grid; grouping by genre,
  developer, or publisher.
- `Format` is now a dropdown of standard values (still free-text
  capable, so nothing already saved is lost).
- Simple cover-image cropping (drag and zoom to a fixed ratio) when
  importing a cover.
- Lightweight description markup (`**bold**`, `*italic*`, `- bullet`),
  editable via toolbar buttons and rendered on the full details page.
- "Open Settings > Emulators" button on a launch failure that a user can
  actually fix there.
- A standard genre list (Action, Adventure, Role-Playing, Strategy,
  Simulation, Sports, Racing, Fighting, Shooter, Platformer, Puzzle,
  Party, Music/Rhythm, Card/Board Game, Educational, Horror, Visual
  Novel, Sandbox, Stealth, Survival), offered in the game-edit dialog
  and the advanced search filter (still free-text capable).

### Fixed

- `i18n/en.json` landed in a doubled `i18n/i18n/` path on a plain
  `dotnet build`/`dotnet run` (only `scripts/build_portable.ps1` avoided
  the bug, by copying `i18n/` straight from source), so every UI label
  silently fell back to its raw translation key outside a portable
  build.
- Every pre-filled emulator preset (RetroArch, Dolphin, PCSX2,
  DuckStation) and the default custom command-line template
  double-quoted the ROM path, which most command-line parsers do not
  treat as the intended single argument — this would have broken
  launching for effectively any default-configured emulator profile.
- Scan error messages were written to the wrong field of a scan report
  entry and were always blank wherever shown.
- The main window never set its own icon at runtime, falling back to
  the default WPF icon in the title bar and taskbar despite
  `RomsCollect.ico` being bundled.
- Five system keys did not match the project's reference naming
  convention (`atarilynx`→`lynx`, `segacd`→`megacd`,
  `wonderswan`→`wswan`, `wonderswancolor`→`wswanc`, `tg16` removed as a
  duplicate of `pcengine`/`pcenginecd`).

## [0.9.1] and earlier

Pre-dates this changelog. See `git log` and `ROADMAP.md` (local,
untracked) for the development history of Phases 0–10.
