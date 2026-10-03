#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Build portable reproductible de RomsCollect.
#
# Publie le projet en self-contained / single-file pour win-x64, puis
# assemble un dossier dist/RomsCollect-win-x64/ prêt à zipper :
#   RomsCollect.exe
#   i18n/en.json
#   assets/png/logo.png
#   assets/icon/RomsCollect.ico
#   data/  (arborescence vide, aucune ROM/média/BIOS embarqué)
#
# Contrainte "100 % portable" (voir ROADMAP.md) : rien n'est écrit hors de
# ce dossier au premier lancement, aucune dépendance au SDK/runtime .NET
# installé sur la machine cible.
#
# Utilisation :
#   scripts/build_portable.sh              # build Release (par défaut)
#   scripts/build_portable.sh Debug        # build dans une autre config
#
# Prérequis : SDK .NET 10 installé (`dotnet --version`). La compilation
# WPF nécessite une machine Windows ou un SDK .NET avec les cibles
# Windows Desktop installées ; ce script ne contourne pas cette exigence.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CONFIGURATION="${1:-Release}"
RID="win-x64"
PROJECT="src/RomsCollect/RomsCollect.csproj"
DIST_DIR="dist/RomsCollect-${RID}"
PUBLISH_DIR="$(mktemp -d)"

echo "== RomsCollect : build portable (${CONFIGURATION}, ${RID}) =="

# --- Vérifications préalables ------------------------------------------
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Erreur : le SDK .NET (dotnet) est introuvable dans le PATH." >&2
  exit 1
fi

if [[ ! -f "$PROJECT" ]]; then
  echo "Erreur : projet introuvable : $PROJECT" >&2
  exit 1
fi

# --- Nettoyage de la sortie précédente ----------------------------------
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# --- Publication self-contained / single-file ---------------------------
echo "-- dotnet publish --"
dotnet publish "$PROJECT" \
  --configuration "$CONFIGURATION" \
  --runtime "$RID" \
  --self-contained true \
  --output "$PUBLISH_DIR" \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false

# --- Copie du résultat de publication vers dist/ -------------------------
# On ne garde que ce qui doit être livré : l'exécutable, ses assets
# portables (i18n/, assets/), et rien d'autre (pas de .pdb de debug, pas
# de fichiers intermédiaires).
echo "-- Assemblage de ${DIST_DIR} --"

cp "$PUBLISH_DIR/RomsCollect.exe" "$DIST_DIR/"

# On copie i18n/ et assets/ directement depuis les sources plutôt que
# depuis la sortie de "dotnet publish" : sur un build à froid (obj/bin
# absents), MSBuild ne recopie pas toujours de manière fiable les items
# None/CopyToOutputDirectory vers le dossier de publication. Copier
# depuis la source connue est déterministe et ne dépend d'aucun
# comportement d'incrémentalité MSBuild.
I18N_SRC="$ROOT/src/RomsCollect/i18n"
if [[ -d "$I18N_SRC" ]]; then
  cp -r "$I18N_SRC" "$DIST_DIR/"
fi

ASSETS_SRC="$ROOT/src/RomsCollect/Assets"
if [[ -d "$ASSETS_SRC" ]]; then
  mkdir -p "$DIST_DIR/assets/png" "$DIST_DIR/assets/icon"
  [[ -f "$ASSETS_SRC/png/logo.png" ]] && cp "$ASSETS_SRC/png/logo.png" "$DIST_DIR/assets/png/logo.png"
  [[ -f "$ASSETS_SRC/icon/RomsCollect.ico" ]] && cp "$ASSETS_SRC/icon/RomsCollect.ico" "$DIST_DIR/assets/icon/RomsCollect.ico"
fi

# --- Arborescence data/ portable, vide (pas de ROM/média/BIOS livré) -----
mkdir -p \
  "$DIST_DIR/data/Database" \
  "$DIST_DIR/data/Bios" \
  "$DIST_DIR/data/Saves" \
  "$DIST_DIR/data/Screenshots" \
  "$DIST_DIR/data/Themes" \
  "$DIST_DIR/data/Logs" \
  "$DIST_DIR/data/Backup"

# La liste des systèmes est extensible depuis l'UI (Réglages > Systèmes) ;
# on ne pré-crée donc pas data/Roms/<s> ni data/Media/<s> ici, ces
# dossiers étant créés par l'application elle-même au premier lancement
# ou lors de l'ajout d'un système.

rm -rf "$PUBLISH_DIR"

# --- Vérifications de sortie ---------------------------------------------
echo "-- Vérifications --"

if [[ ! -f "$DIST_DIR/RomsCollect.exe" ]]; then
  echo "Erreur : RomsCollect.exe absent de ${DIST_DIR} après publication." >&2
  exit 1
fi

if [[ ! -f "$DIST_DIR/i18n/en.json" ]]; then
  echo "Attention : ${DIST_DIR}/i18n/en.json est absent." >&2
fi

if [[ ! -f "$DIST_DIR/assets/png/logo.png" || ! -f "$DIST_DIR/assets/icon/RomsCollect.ico" ]]; then
  echo "Attention : logo.png ou RomsCollect.ico absent de ${DIST_DIR}/assets/." >&2
fi

# Rappel défensif de la contrainte hors-ligne / portable : aucun fichier
# ne doit référencer %APPDATA%, le registre, ou un chemin absolu utilisateur.
if grep -RIl "C:\\\\Users\\\\" "$DIST_DIR" >/dev/null 2>&1; then
  echo "Attention : un chemin absolu utilisateur a été détecté dans ${DIST_DIR}." >&2
fi

echo ""
echo "Build terminé : ${DIST_DIR}"
echo "Taille :"
du -sh "$DIST_DIR"
echo ""
echo "Pour livrer, zippez le contenu de ce dossier, par exemple :"
echo "  cd dist && zip -r RomsCollect-${RID}.zip RomsCollect-${RID}"
