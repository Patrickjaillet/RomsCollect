#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Vérifie que les clés de système (scripts de scaffolding, dossiers
# data/Roms/ existants) respectent la nomenclature de référence retenue
# par le projet, et que les sous-dossiers média (scripts de scaffolding,
# dossiers data/Media/<système>/ existants) respectent la nomenclature
# de référence des types de média, pour garantir l'import direct d'une
# collection de ROMs / bibliothèque de médias existante organisée selon
# ces mêmes conventions.
#
# Utilisation :
#   scripts/check_system_names.sh
#
# Exit code 0 si tout est conforme, 1 sinon (utilisable en CI et en hook
# pre-commit).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# Référence des clés système actuellement utilisées par
# scripts/scaffold_dirs.sh, vérifiées une à une contre la nomenclature
# de référence retenue par le projet. Mettre à jour cette liste en même
# temps que scaffold_dirs.sh à chaque ajout/retrait de système.
# Les clés historiquement incorrectes (atarilynx, segacd, wonderswan,
# wonderswancolor, tg16) ont été corrigées le 2026-10-03 (cf. ROADMAP.md,
# section 11.2) et ne doivent plus apparaître.
KNOWN_BAD_KEYS=(
  atarilynx
  segacd
  wonderswan
  wonderswancolor
  tg16
)

VALID_SYSTEMS=(
  amiga500 amiga1200 amigacd32 amstradcpc atari2600 atari5200 atari7800
  lynx atarist c64 colecovision dos dreamcast gameandwatch gamegear
  gb gba gbc mame mastersystem megadrive msx msx2 n64 nds nes ngp ngpc
  pcengine pcenginecd psp psx ps2 saturn scummvm sega32x megacd sg1000
  snes virtualboy windows wswan wswanc x68000 zxspectrum
)

# Référence des sous-dossiers média actuellement utilisés par
# scripts/scaffold_dirs.sh (tableau MEDIA_TYPES), vérifiés contre la
# nomenclature de référence des types de média retenue par le projet.
# Mettre à jour cette liste en même temps que scaffold_dirs.sh à chaque
# ajout/retrait de type de média.
VALID_MEDIA_TYPES=(
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

contains() {
  local needle="$1"
  shift
  for item in "$@"; do
    [[ "$needle" == "$item" ]] && return 0
  done
  return 1
}

errors=0

# --- Vérification de scripts/scaffold_dirs.sh (clés système) ---------------
SCAFFOLD_SCRIPT="scripts/scaffold_dirs.sh"
if [[ -f "$SCAFFOLD_SCRIPT" ]]; then
  for bad in "${KNOWN_BAD_KEYS[@]}"; do
    if grep -qw -- "$bad" "$SCAFFOLD_SCRIPT"; then
      echo "ERREUR : clé système non conforme à la nomenclature de référence trouvée dans $SCAFFOLD_SCRIPT : '$bad'"
      errors=$((errors + 1))
    fi
  done
else
  echo "AVERTISSEMENT : $SCAFFOLD_SCRIPT introuvable, vérification sautée."
fi

# --- Vérification des dossiers data/Roms/ existants (le cas échéant) -------
# data/ est gitignoré (données portables, pas de code source) : ce dossier
# n'existe en général que sur un poste de développement ou de production
# après un premier scaffold/scan, jamais dans le dépôt Git lui-même.
ROMS_DIR="data/Roms"
if [[ -d "$ROMS_DIR" ]]; then
  for dir in "$ROMS_DIR"/*/; do
    [[ -d "$dir" ]] || continue
    key="$(basename "$dir")"
    if ! contains "$key" "${VALID_SYSTEMS[@]}"; then
      echo "AVERTISSEMENT : dossier $ROMS_DIR/$key ne correspond à aucune clé système connue de ce script (mettre à jour VALID_SYSTEMS si c'est un ajout légitime)."
    fi
  done
fi

# --- Vérification des sous-dossiers média dans scripts/scaffold_dirs.sh ----
if [[ -f "$SCAFFOLD_SCRIPT" ]]; then
  for media in "${VALID_MEDIA_TYPES[@]}"; do
    if ! grep -qF -- "\"$media\"" "$SCAFFOLD_SCRIPT"; then
      echo "ERREUR : sous-dossier média attendu absent de $SCAFFOLD_SCRIPT : '$media'"
      errors=$((errors + 1))
    fi
  done
fi

# --- Vérification des dossiers data/Media/<système>/ existants -------------
MEDIA_DIR="data/Media"
if [[ -d "$MEDIA_DIR" ]]; then
  for system_dir in "$MEDIA_DIR"/*/; do
    [[ -d "$system_dir" ]] || continue
    for media_dir in "$system_dir"*/; do
      [[ -d "$media_dir" ]] || continue
      media_name="$(basename "$media_dir")"
      if ! contains "$media_name" "${VALID_MEDIA_TYPES[@]}"; then
        echo "AVERTISSEMENT : dossier $media_dir ne correspond à aucun type de média connu de ce script (mettre à jour VALID_MEDIA_TYPES si c'est un ajout légitime)."
      fi
    done
  done
fi

if [[ $errors -eq 0 ]]; then
  echo "OK : aucune clé système ni aucun type de média non conforme détecté."
  exit 0
fi

echo ""
echo "$errors erreur(s) trouvée(s). Corrigez les clés/types ci-dessus avant de committer."
exit 1
