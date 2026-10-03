-- SPDX-License-Identifier: GPL-3.0-or-later
-- -----------------------------------------------------------------------
-- Adds an optional system icon/logo, imported from a local file and
-- displayed in the left-hand system list and in Settings > Systems.
-- IconPath is a path relative to data/Media/<systemKey>/, following the
-- same "never a network request" rule as every other media import in the
-- application (cover art, manuals, etc.).
-- -----------------------------------------------------------------------
ALTER TABLE Systems ADD COLUMN IconPath TEXT NULL;
