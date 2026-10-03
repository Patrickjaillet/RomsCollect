// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;

namespace RomsCollect.ViewModels;

/// <summary>One row of the left-hand system/platform sidebar.</summary>
public sealed partial class SystemListItemViewModel : ObservableObject
{
    /// <summary>Null for the synthetic "All"/"Favorites"/... entries.</summary>
    public int? SystemId { get; }

    public string Key { get; }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private int _gameCount;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _showGameCount = true;

    /// <summary>Absolute path to the system's icon/logo image, or null when none is set (synthetic entries like "All" always have none).</summary>
    public string? IconFullPath { get; }

    /// <summary>True for a user-defined collection entry, which can be deleted from the sidebar.</summary>
    public bool IsUserCollection { get; }

    public SystemListItemViewModel(int? systemId, string key, string displayName, int gameCount, string? iconFullPath = null, bool isUserCollection = false)
    {
        SystemId = systemId;
        Key = key;
        _displayName = displayName;
        _gameCount = gameCount;
        IconFullPath = iconFullPath;
        IsUserCollection = isUserCollection;
    }
}
