#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Vérifie que 100 % des fichiers source (.cs, .xaml) portent l'en-tête
# SPDX-License-Identifier: GPL-3.0-or-later dans leurs 3 premières lignes.
#
# Utilisation :
#   scripts/check_spdx.sh            # vérifie tout le dépôt
#   scripts/check_spdx.sh --fix      # ajoute l'en-tête manquant automatiquement
#
# Exit code 0 si tout est conforme, 1 sinon (utilisable en CI et en hook
# pre-commit).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SPDX_TAG="SPDX-License-Identifier: GPL-3.0-or-later"
FIX_MODE=false
if [[ "${1:-}" == "--fix" ]]; then
  FIX_MODE=true
fi

# Répertoires exclus : artefacts de build, dépendances, données utilisateur.
EXCLUDE_DIRS=(
  "*/obj/*"
  "*/bin/*"
  "*/.git/*"
  "*/dist/*"
  "./data/*"
)

# Construit l'expression -path pour exclure les répertoires ci-dessus.
prune_expr=()
for dir in "${EXCLUDE_DIRS[@]}"; do
  prune_expr+=(-path "$dir" -o)
done
unset 'prune_expr[${#prune_expr[@]}-1]' # retire le dernier -o

mapfile -t FILES < <(
  find . \( "${prune_expr[@]}" \) -prune -o \
    -type f \( -name "*.cs" -o -name "*.xaml" \) -print | sort
)

missing=()
for f in "${FILES[@]}"; do
  if ! head -3 "$f" | grep -qF "$SPDX_TAG"; then
    missing+=("$f")
  fi
done

if [[ ${#missing[@]} -eq 0 ]]; then
  echo "OK : en-tête SPDX présent dans les ${#FILES[@]} fichiers source (.cs/.xaml)."
  exit 0
fi

echo "En-tête SPDX manquant dans ${#missing[@]} fichier(s) sur ${#FILES[@]} :"
for f in "${missing[@]}"; do
  echo "  - $f"
done

if [[ "$FIX_MODE" == true ]]; then
  echo ""
  echo "Correction automatique (--fix) ..."
  for f in "${missing[@]}"; do
    case "$f" in
      *.xaml)
        # Préserve un éventuel BOM UTF-8 en tête de fichier : le commentaire
        # SPDX doit venir juste après le BOM, jamais avant.
        if head -c 3 "$f" | grep -qP '^\xef\xbb\xbf'; then
          {
            head -c 3 "$f"
            printf '<!-- %s -->\n' "$SPDX_TAG"
            tail -c +4 "$f"
          } > "$f.tmp"
        else
          {
            printf '<!-- %s -->\n' "$SPDX_TAG"
            cat "$f"
          } > "$f.tmp"
        fi
        mv "$f.tmp" "$f"
        ;;
      *.cs)
        {
          printf '// %s\n' "$SPDX_TAG"
          cat "$f"
        } > "$f.tmp"
        mv "$f.tmp" "$f"
        ;;
    esac
    echo "  + corrigé : $f"
  done
  echo "Terminé. Relancez le script sans --fix pour confirmer."
  exit 0
fi

echo ""
echo "Relancez avec --fix pour ajouter l'en-tête automatiquement, ou corrigez"
echo "ces fichiers à la main avant de committer."
exit 1
