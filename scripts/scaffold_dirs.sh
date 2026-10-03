#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Génère l'arborescence complète du projet RomsCollect.
# Répertoires "Roms" et "Media" selon la nomenclature de référence du
# projet (roms/<systeme>/ et Media/<systeme>/<TypeMedia>/).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# --- Solution / code source -------------------------------------------------
mkdir -p \
  src/RomsCollect/Assets/png \
  src/RomsCollect/Assets/icon \
  src/RomsCollect/i18n \
  src/RomsCollect/Models \
  src/RomsCollect/Services/Database \
  src/RomsCollect/Services/Scanning \
  src/RomsCollect/Services/Media \
  src/RomsCollect/Services/Emulation \
  src/RomsCollect/Services/Import \
  src/RomsCollect/ViewModels \
  src/RomsCollect/Views/Controls \
  src/RomsCollect/Views/Windows \
  src/RomsCollect/Resources/Themes \
  src/RomsCollect/Resources/Styles \
  src/RomsCollect/Data \
  src/RomsCollect/Converters \
  src/RomsCollect/Helpers

mkdir -p tests/RomsCollect.Tests/Services tests/RomsCollect.Tests/Fixtures

mkdir -p docs

# --- Racine des données portables (à côté de l'exécutable) -----------------
DATA="data"
mkdir -p "$DATA/Database" "$DATA/Bios" "$DATA/Saves" "$DATA/Screenshots" "$DATA/Themes" "$DATA/Logs" "$DATA/Backup"

# Liste représentative de systèmes (nomenclature de référence du projet).
# Extensible via Settings > Systèmes dans l'application, en respectant
# la même convention.
SYSTEMS=(
  amiga500 amiga1200 amigacd32 amstradcpc atari2600 atari5200 atari7800
  lynx atarist c64 colecovision dos dreamcast gameandwatch gamegear
  gb gba gbc mame mastersystem megadrive msx msx2 n64 nds nes ngp ngpc
  pcengine pcenginecd psp psx ps2 saturn scummvm sega32x megacd sg1000
  snes virtualboy windows wswan wswanc x68000 zxspectrum
)

# Sous-types de médias, nomenclature de référence du projet.
MEDIA_TYPES=(
  "Box - Front"
  "Box - Back"
  "Box - 3D"
  "Screenshot - Gameplay"
  "Screenshot - Game Title"
  "Fanart - Background"
  "Clear Logo"
  "Video"
  "Manual"
  "Cart - Front"
)

for sys in "${SYSTEMS[@]}"; do
  mkdir -p "$DATA/Roms/$sys"
  for media in "${MEDIA_TYPES[@]}"; do
    mkdir -p "$DATA/Media/$sys/$media"
  done
done

echo "Arborescence générée sous $ROOT/$DATA et $ROOT/src, $ROOT/tests, $ROOT/docs."
