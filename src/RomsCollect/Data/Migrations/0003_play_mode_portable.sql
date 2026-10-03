-- SPDX-License-Identifier: GPL-3.0-or-later
-- -----------------------------------------------------------------------
-- Adds the two catalog fields shown in the quick detail panel's second
-- "Information" block: PlayMode (free text, e.g. "Single
-- Player", "Multiplayer", "Co-op") and IsPortable (whether the game runs
-- standalone without requiring separate media, e.g. a homebrew ROM).
-- -----------------------------------------------------------------------
ALTER TABLE Games ADD COLUMN PlayMode TEXT NULL;
ALTER TABLE Games ADD COLUMN IsPortable INTEGER NOT NULL DEFAULT 0;
