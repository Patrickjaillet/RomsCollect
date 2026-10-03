// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.ViewModels;

/// <summary>How catalog rows are grouped into folders in the list view.</summary>
public enum CatalogGroupingMode
{
    NoFolders,
    Platform,
    Completeness,
    PlatformAndCompleteness,
    Genre,
    Developer,
    Publisher,
}
