# User Guide

## Installation

RomsCollect is fully portable. Extract the release archive to any folder
— a local drive, an external drive, or a USB stick — and run
`RomsCollect.exe`. There is no installer, no administrator rights are
needed, and nothing is written outside the application's own folder.

## Directory layout

RomsCollect stores everything next to its executable, in a `data/`
folder:

```
RomsCollect.exe
i18n/en.json
data/
  Roms/<system>/                Your ROM files, one subfolder per system
  Media/<system>/<Media Type>/  Box art, screenshots, fanart, videos, manuals
  Database/romscollect.db       The collection database
  Bios/<system>/                BIOS files your emulators may need
  Saves/<system>/               Emulator save files
  Screenshots/                  Screenshots captured from within RomsCollect
  Backup/                       Automatic database backups (made before each migration)
assets/
  png/logo.png
  icon/RomsCollect.ico
```

### ROM folder names

Each system's ROMs go in `data/Roms/<system-key>/`, where `<system-key>`
matches RomsCollect's naming convention — for example `snes`, `nes`,
`megadrive`, `psx`, `mame`, `amiga1200`. If you already have a `roms/`
folder organized the same way, you can import it directly (see
"Importing an existing collection" below) instead of copying files by
hand.

The list of systems is not hard-coded: open `Menu > Settings... >
Systems` to add a system with its key, display name, and the file
extensions it uses (for example `.sfc,.smc` for SNES). RomsCollect only
picks up files with a registered extension when scanning.

### Media folder names

Media assets go in `data/Media/<system-key>/<Media Type>/`, where
`<Media Type>` matches RomsCollect's media-type naming convention —
`Box - Front`, `Box - Back`, `Box - 3D`, `Screenshot - Gameplay`,
`Screenshot - Game Title`, `Fanart - Background`, `Clear Logo`, `Video`,
`Manual`, `Cart - Front`. A media file is linked to a game automatically
when its file name (without extension) matches the game's ROM file name.

## Importing an existing collection

- **From an existing ROM folder:** `Menu > Import Collection...` and
  point it at your `roms/` folder. RomsCollect detects which of its
  subfolders match a configured system and lets you copy the files in,
  or create a symbolic link instead of copying (useful if you want to
  keep using the same ROM files without duplicating them).
- **From an existing media library:** the same dialog reads platform
  metadata XML files (`Data/Platforms/*.xml`) and imports the text
  metadata (title, description, developer, publisher, release date,
  genres) for games whose ROM file name matches — entirely from local
  files, with no network access.

## Scanning for new games

`Menu > Scan for New Games...` (or `Ctrl+R`) opens the scan window. Choose
**Scan all systems** to look through `data/Roms/<system>/` for every
configured system, or pick one system from the dropdown and use **Scan
selected system only** to scan just that folder. Either way, RomsCollect
matches files by extension and computes a hash of each file's content to
detect a renamed duplicate of a ROM you already have, showing which system
is currently being scanned while it runs. Nothing is written to your
collection until you review the scan report (added / updated / unchanged /
errors) and press **Apply Changes**.

## Browsing your collection

The main window offers three interchangeable views, switchable from the
`Arrange By` menu or with `Ctrl+W` (toggles between grid and wheel):

- **Grid** — box-art tiles with the platform, title, developer, and your
  rating.
- **Wheel/carousel** — the selected game's cover is centered and
  enlarged, with neighboring games shown smaller on each side; navigate
  with the arrow keys or an Xbox-compatible gamepad's D-Pad/left stick
  (left/right to move, A to launch the selected game).
- **List (tabular catalog)** — a full collector's spreadsheet view.
  `Select Columns...` lets you choose which fields are shown, grouped by
  collapsible categories (Main, Hardware, Edition, Completeness,
  Personal); drag a column header to reorder it, which is remembered the
  next time you open RomsCollect. `Group By` organizes rows into folders
  by platform, completeness, genre, developer, or publisher (and
  platform+completeness combined); clicking a column header sorts by it.
  `Add Games` opens a manual entry form (title and platform only) —
  RomsCollect never looks anything up online. Check one or more rows to
  reveal a bulk actions bar: delete the selection, add a tag to all of
  them, set their Collection Status, or export the selection to a CSV
  file.

The left sidebar lists `All`, `Favorites`, `Recently Added`, `Recently
Played`, your own collections (create one with the field and `+` button
below the list; the `✕` next to a collection's name removes it), and
every configured system, each with a game count — hide the count from
`Settings... > Display` if you prefer a pure frontend look. Use the
search box above the collection view to filter instantly by title, or
click the filter icon next to it to combine genre, release year, minimum
rating, completeness, and favorites/completed filters; `Reset Filters`
clears them all at once. Right-click a game's tile to add it to or
remove it from any of your collections.

## Keyboard shortcuts

`Menu > Keyboard Shortcuts...` shows this list from within the app.

| Shortcut | Action |
| --- | --- |
| `Ctrl+W` | Switch between grid and wheel view |
| `Ctrl+F` | Focus the search box |
| `Ctrl+R` | Scan for new games |
| `F5` | Play the selected game |
| `Ctrl+D` | Toggle Favorite on the selected game |
| `Ctrl+T` | Toggle Completed on the selected game |

## The detail panel and full details page

Selecting a game shows a quick detail panel on the right: cover art (or
an integrated video player with play/pause and a scrub bar, when the
game has a "Video" media asset), fanart banner with the game's logo,
rating, a Play button, a media filmstrip — click any thumbnail to show it
in the main preview — a second information block (rating, genre, play
mode, region, status, whether the ROM was imported by a scan, and
whether it's portable), and quick actions (mark as favorite, mark as
completed, open the ROM's folder, delete the game).

Click the "..." button for the full details page: the same fanart/logo
banner, a Play button, a media gallery (every screenshot and box-art
image, plus the video player when available), an editable 0–10 rating
you can change without reopening the edit dialog, description, download
links, manual, and play history.

### Download links

A game's download links are plain text URLs you type in yourself —
RomsCollect never fetches, previews, or validates them. Clicking "Open"
always asks for confirmation before handing off to your system's default
web browser, which is the one point where the action leaves RomsCollect
entirely.

### Manual

Import a manual (PDF or image) from a local file via "Import Manual...".
Opening it launches whichever application is associated with that file
type on your system — RomsCollect does not include a PDF viewer.

## Editing a game's catalog entry

Click "Edit..." to open the tabbed edit dialog:

- **Main** — title, sort title, platform, release date, completeness
  (Loose/CIB/New), box/manual checkboxes, barcode, format (a dropdown of
  standard values — Cartridge, CD, DVD, Blu-ray, Digital, Floppy Disk,
  Cassette — that still accepts your own text), edition, region, series,
  audience rating, play mode, a "Portable" checkbox, and editable chip
  lists for developers, publishers, and genres.
- **Personal** — condition, owner, purchase date/store, storage location,
  purchase price, and current value (always a manual entry — RomsCollect
  never queries an online pricing service), completed status, notes, a
  0–10 rating, tags, collection status (In Collection / Wanted / Sold /
  On Loan), and quantity/location fields.
- **Cover** — import a cover image from a local file (no online cover
  lookup); a crop window lets you drag and zoom it to the standard
  box-art ratio before it's saved.
- **Description** — a free-text field with Bold/Italic/Bullet buttons
  that wrap your selection in lightweight markup (`**bold**`, `*italic*`,
  `- bullet`), rendered on the full details page.

Use `< Previous` / `Next >` to move to the next game in your current
view without closing the dialog; your changes are saved automatically
each time you navigate.

## Configuring emulators

Open `Menu > Settings... > Emulators`, choose a system, and either pick a
preset (RetroArch, Dolphin, PCSX2, DuckStation) — which pre-fills a
command-line template — or write your own. Set the path to an emulator
executable you already have installed (RomsCollect does not download or
bundle any emulator), then click "Add Emulator Profile". Use "Set
Default" to choose which profile is used when you press Play for a game
on that system.

The command-line template supports a `{RomPath}` placeholder, replaced
with the game's full ROM path (quoted, so paths with spaces or special
characters work correctly) when the game is launched.

If Play fails — no profile configured, the emulator executable can't be
found, or the ROM file is missing — RomsCollect shows a message
explaining exactly what's wrong rather than failing silently, with a
direct "Open Settings > Emulators" button when the problem is something
you can fix there.

## Settings

`Menu > Settings...` also covers language, dark/light theme, accent
color, the portable directory paths (shown for reference), and what
happens to the RomsCollect window when you launch a game (minimize,
close, or do nothing).

## Offline by design

RomsCollect never makes a network request at runtime: no telemetry, no
update checks, no online metadata, cover art, or price lookups. The only
things that ever leave the application are a download link or a manual
file, and only after you explicitly confirm opening it.
