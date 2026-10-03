#Requires -Version 5.1
<#
.SYNOPSIS
    Build portable reproductible de RomsCollect (Windows).

.DESCRIPTION
    Publie le projet en self-contained / single-file pour win-x64, puis
    assemble un dossier dist/RomsCollect-win-x64/ pret a zipper :
        RomsCollect.exe
        i18n/en.json
        assets/png/logo.png
        assets/icon/RomsCollect.ico
        data/  (arborescence vide, aucune ROM/media/BIOS embarque)

    Contrainte "100% portable" (voir ROADMAP.md) : rien n'est ecrit hors
    de ce dossier au premier lancement, aucune dependance au SDK/runtime
    .NET installe sur la machine cible.

.PARAMETER Configuration
    Configuration de build (Release par defaut).

.EXAMPLE
    scripts\build_portable.ps1
    scripts\build_portable.ps1 -Configuration Debug
#>

# SPDX-License-Identifier: GPL-3.0-or-later

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Rid = "win-x64"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$Project = Join-Path $Root "src\RomsCollect\RomsCollect.csproj"
$DistDir = Join-Path $Root "dist\RomsCollect-$Rid"
$PublishDir = Join-Path ([System.IO.Path]::GetTempPath()) ("romscollect_publish_" + [guid]::NewGuid().ToString("N"))

Write-Host "== RomsCollect : build portable ($Configuration, $Rid) =="

# --- Verifications prealables --------------------------------------------
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "Le SDK .NET (dotnet) est introuvable dans le PATH."
    exit 1
}

if (-not (Test-Path $Project)) {
    Write-Error "Projet introuvable : $Project"
    exit 1
}

# --- Nettoyage de la sortie precedente ------------------------------------
if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

# --- Publication self-contained / single-file -----------------------------
Write-Host "-- dotnet publish --"
dotnet publish $Project `
    --configuration $Configuration `
    --runtime $Rid `
    --self-contained true `
    --output $PublishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish a echoue (code $LASTEXITCODE)."
    exit $LASTEXITCODE
}

# --- Copie du resultat de publication vers dist/ --------------------------
# On ne garde que ce qui doit etre livre : l'executable, ses assets
# portables (i18n/, assets/), et rien d'autre (pas de .pdb de debug, pas
# de fichiers intermediaires).
Write-Host "-- Assemblage de $DistDir --"

Copy-Item (Join-Path $PublishDir "RomsCollect.exe") $DistDir

# On copie i18n/ et assets/ directement depuis les sources plutot que
# depuis la sortie de "dotnet publish" : sur un build a froid (obj/bin
# absents), MSBuild ne recopie pas toujours de maniere fiable les items
# None/CopyToOutputDirectory vers le dossier de publication. Copier
# depuis la source connue est deterministe et ne depend d'aucun
# comportement d'incrementalite MSBuild.
$I18nSrc = Join-Path $Root "src\RomsCollect\i18n"
if (Test-Path $I18nSrc) {
    Copy-Item $I18nSrc (Join-Path $DistDir "i18n") -Recurse
}

$AssetsSrc = Join-Path $Root "src\RomsCollect\Assets"
if (Test-Path $AssetsSrc) {
    New-Item -ItemType Directory -Path (Join-Path $DistDir "assets\png") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $DistDir "assets\icon") -Force | Out-Null
    $LogoSrc = Join-Path $AssetsSrc "png\logo.png"
    $IconSrc = Join-Path $AssetsSrc "icon\RomsCollect.ico"
    if (Test-Path $LogoSrc) {
        Copy-Item $LogoSrc (Join-Path $DistDir "assets\png\logo.png")
    }
    if (Test-Path $IconSrc) {
        Copy-Item $IconSrc (Join-Path $DistDir "assets\icon\RomsCollect.ico")
    }
}

# --- Arborescence data/ portable, vide (pas de ROM/media/BIOS livre) ------
$DataSubdirs = @("Database", "Bios", "Saves", "Screenshots", "Themes", "Logs", "Backup")
foreach ($sub in $DataSubdirs) {
    New-Item -ItemType Directory -Path (Join-Path $DistDir "data\$sub") -Force | Out-Null
}

# La liste des systemes est extensible depuis l'UI (Reglages > Systemes) ;
# on ne pre-cree donc pas data/Roms/<s> ni data/Media/<s> ici, ces
# dossiers etant crees par l'application elle-meme au premier lancement
# ou lors de l'ajout d'un systeme.

Remove-Item $PublishDir -Recurse -Force

# --- Verifications de sortie -----------------------------------------------
Write-Host "-- Verifications --"

if (-not (Test-Path (Join-Path $DistDir "RomsCollect.exe"))) {
    Write-Error "RomsCollect.exe absent de $DistDir apres publication."
    exit 1
}

if (-not (Test-Path (Join-Path $DistDir "i18n\en.json"))) {
    Write-Warning "$DistDir\i18n\en.json est absent."
}

$LogoPath = Join-Path $DistDir "assets\png\logo.png"
$IconPath = Join-Path $DistDir "assets\icon\RomsCollect.ico"
if (-not (Test-Path $LogoPath) -or -not (Test-Path $IconPath)) {
    Write-Warning "logo.png ou RomsCollect.ico absent de $DistDir\assets\."
}

# Rappel defensif de la contrainte hors-ligne / portable : aucun fichier
# ne doit referencer un chemin absolu utilisateur (%USERPROFILE%, etc.).
$UserPathHit = Get-ChildItem $DistDir -Recurse -File | Select-String -Pattern "C:\\Users\\" -List -ErrorAction SilentlyContinue
if ($UserPathHit) {
    Write-Warning "Un chemin absolu utilisateur a ete detecte dans $DistDir."
}

Write-Host ""
Write-Host "Build termine : $DistDir"
$Size = (Get-ChildItem $DistDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host ("Taille : {0:N1} Mo" -f ($Size / 1MB))
Write-Host ""
Write-Host "Pour livrer, compressez ce dossier, par exemple :"
Write-Host "  Compress-Archive -Path '$DistDir' -DestinationPath 'dist\RomsCollect-$Rid.zip'"
