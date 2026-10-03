-- SPDX-License-Identifier: GPL-3.0-or-later
-- RomsCollect initial schema.
-- This script must stay idempotent: every statement uses IF NOT EXISTS so it
-- can be re-run safely against a database that already has this version applied.

CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version    INTEGER NOT NULL PRIMARY KEY,
    AppliedAt  TEXT    NOT NULL
);

-- ---------------------------------------------------------------------------
-- Systems (platforms) — directory names follow a standard per-system
-- folder naming convention (snes, nes, megadrive, psx, mame, amiga1200,
-- ...) so an existing roms/ tree in that same convention can be imported
-- without renaming.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Systems (
    Id                  INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Key                 TEXT    NOT NULL UNIQUE,   -- e.g. "snes", "megadrive"
    Name                TEXT    NOT NULL,           -- e.g. "Super Nintendo Entertainment System"
    RomExtensions       TEXT    NOT NULL DEFAULT '', -- comma-separated, e.g. ".sfc,.smc"
    SortOrder           INTEGER NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Systems_Key ON Systems (Key);

-- ---------------------------------------------------------------------------
-- Games — the "playable ROM" facet and the collector catalog facet share
-- this single row. Per-copy ownership data lives in GameOwnership (a game
-- can have several owned copies/editions).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Games (
    Id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SystemId        INTEGER NOT NULL REFERENCES Systems (Id) ON DELETE CASCADE,
    Title           TEXT    NOT NULL,
    SortTitle       TEXT    NOT NULL,
    RomPath         TEXT    NULL,       -- relative to data/Roms/<system>/, NULL for catalog-only entries
    RomHash         TEXT    NULL,       -- CRC32 or MD5 of the ROM file, for duplicate detection
    ReleaseDate     TEXT    NULL,       -- ISO-8601 date, partial dates allowed as free text
    Series          TEXT    NULL,
    AudienceRating  TEXT    NULL,
    Barcode         TEXT    NULL,
    Format          TEXT    NULL,       -- Cartridge / CD / DVD / Blu-ray / Digital / ...
    Edition         TEXT    NULL,
    Region          TEXT    NULL,
    Description     TEXT    NULL,
    IsFavorite      INTEGER NOT NULL DEFAULT 0,
    IsCompleted     INTEGER NOT NULL DEFAULT 0,
    Rating          INTEGER NULL,       -- 0-10, personal rating
    DateAdded       TEXT    NOT NULL,
    DateModified    TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Games_SystemId ON Games (SystemId);
CREATE INDEX IF NOT EXISTS IX_Games_Title ON Games (Title);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Games_SystemId_RomPath
    ON Games (SystemId, RomPath) WHERE RomPath IS NOT NULL;

-- ---------------------------------------------------------------------------
-- Reusable lookup dictionaries for the list-valued catalog fields
-- (Developers, Publishers, Genres) plus free-form personal Tags.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Developers (
    Id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name  TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Publishers (
    Id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name  TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Genres (
    Id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name  TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Tags (
    Id    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name  TEXT    NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS GameDevelopers (
    GameId       INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    DeveloperId  INTEGER NOT NULL REFERENCES Developers (Id) ON DELETE CASCADE,
    PRIMARY KEY (GameId, DeveloperId)
);

CREATE TABLE IF NOT EXISTS GamePublishers (
    GameId       INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    PublisherId  INTEGER NOT NULL REFERENCES Publishers (Id) ON DELETE CASCADE,
    PRIMARY KEY (GameId, PublisherId)
);

CREATE TABLE IF NOT EXISTS GameGenres (
    GameId   INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    GenreId  INTEGER NOT NULL REFERENCES Genres (Id) ON DELETE CASCADE,
    PRIMARY KEY (GameId, GenreId)
);

CREATE TABLE IF NOT EXISTS GameTags (
    GameId  INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    TagId   INTEGER NOT NULL REFERENCES Tags (Id) ON DELETE CASCADE,
    PRIMARY KEY (GameId, TagId)
);

-- ---------------------------------------------------------------------------
-- Media assets, following RomsCollect's own media-folder convention
-- (Box - Front, Screenshot - Gameplay, Clear Logo, Video, Manual, ...).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS GameMedia (
    Id            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameId        INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    MediaType     TEXT    NOT NULL,  -- e.g. "Box - Front", "Screenshot - Gameplay", "Video"
    RelativePath  TEXT    NOT NULL,  -- relative to data/Media/<system>/<MediaType>/
    SortOrder     INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_GameMedia_GameId ON GameMedia (GameId);

-- ---------------------------------------------------------------------------
-- Manually entered download links. RomsCollect never validates or fetches
-- these URLs itself; the link is opened in the system's default browser
-- only on explicit user action.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS GameDownloadLinks (
    Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameId     INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    Label      TEXT    NOT NULL,
    Url        TEXT    NOT NULL,
    SortOrder  INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_GameDownloadLinks_GameId ON GameDownloadLinks (GameId);

-- ---------------------------------------------------------------------------
-- Emulator launch profiles, one set of settings per system.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS EmulatorProfiles (
    Id                     INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    SystemId               INTEGER NOT NULL REFERENCES Systems (Id) ON DELETE CASCADE,
    Name                   TEXT    NOT NULL,
    ExecutablePath         TEXT    NOT NULL,
    CommandLineTemplate    TEXT    NOT NULL DEFAULT '"{RomPath}"',
    WorkingDirectory       TEXT    NULL,
    IsDefault              INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_EmulatorProfiles_SystemId ON EmulatorProfiles (SystemId);

-- ---------------------------------------------------------------------------
-- Play history, one row per play session.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS PlayHistory (
    Id               INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameId           INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    StartedAt        TEXT    NOT NULL,
    EndedAt          TEXT    NULL,
    DurationSeconds  INTEGER NULL
);

CREATE INDEX IF NOT EXISTS IX_PlayHistory_GameId ON PlayHistory (GameId);

-- ---------------------------------------------------------------------------
-- Application settings, a simple key/value store (language, theme, paths...).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Settings (
    Key    TEXT NOT NULL PRIMARY KEY,
    Value  TEXT NOT NULL
);

-- ---------------------------------------------------------------------------
-- User-defined collections / playlists.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Collections (
    Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name       TEXT    NOT NULL,
    SortOrder  INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS CollectionGames (
    CollectionId  INTEGER NOT NULL REFERENCES Collections (Id) ON DELETE CASCADE,
    GameId        INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    SortOrder     INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (CollectionId, GameId)
);

-- ---------------------------------------------------------------------------
-- Ownership — backs the "Personal" tab. One row per physical or digital
-- copy, so duplicates/re-editions of the same game are tracked
-- separately.
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS GameOwnership (
    Id                INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    GameId            INTEGER NOT NULL REFERENCES Games (Id) ON DELETE CASCADE,
    Condition         TEXT    NULL,
    Owner             TEXT    NULL,
    PurchaseDate      TEXT    NULL,
    PurchaseStore     TEXT    NULL,
    StorageDevice     TEXT    NULL,
    StorageSlot       TEXT    NULL,
    PurchasePrice     REAL    NULL,
    CurrentValue      REAL    NULL,   -- manual entry only, never fetched online
    Completed         INTEGER NOT NULL DEFAULT 0,
    CompletedDate     TEXT    NULL,
    Notes             TEXT    NULL,
    Rating            INTEGER NULL,   -- 0-10
    CollectionStatus  TEXT    NULL,   -- In Collection / Wanted / Sold / On Loan / ...
    SortIndex         INTEGER NOT NULL DEFAULT 0,
    Quantity          INTEGER NOT NULL DEFAULT 1,
    Location          TEXT    NULL,
    Completeness      TEXT    NULL,   -- Loose / CIB / New
    HasBox            INTEGER NOT NULL DEFAULT 0,
    HasManual         INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_GameOwnership_GameId ON GameOwnership (GameId);
