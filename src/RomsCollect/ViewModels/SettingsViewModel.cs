// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RomsCollect.Models;
using RomsCollect.Services;
using RomsCollect.Services.Database;

namespace RomsCollect.ViewModels;

/// <summary>Behavior applied when the user launches a game from RomsCollect.</summary>
public enum GameLaunchBehavior
{
    Minimize,
    Close,
    DoNothing,
}

/// <summary>
/// Backing view model for the Settings window: language, theme, accent
/// color, portable directory paths, and the launch behavior. Persisted to
/// the <c>Settings</c> key/value table via <see cref="SettingsRepository"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;
    private readonly SystemRepository _systems;
    private readonly EmulatorProfileRepository _emulatorProfiles;
    private readonly PortableDataPaths _dataPaths;

    private const string LanguageKey = "language";
    private const string ThemeKey = "theme";
    private const string AccentColorKey = "accentColor";
    private const string LaunchBehaviorKey = "launchBehavior";
    private const string ShowSidebarGameCountKey = "showSidebarGameCount";

    [ObservableProperty]
    private string _language = "en";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private string _accentColorHex = "#22C55E";

    [ObservableProperty]
    private GameLaunchBehavior _launchBehavior = GameLaunchBehavior.Minimize;

    /// <summary>
    /// Whether the left sidebar shows a per-system game count (catalog
    /// mode) or hides it (pure frontend mode). Persisted so the choice
    /// survives a restart.
    /// </summary>
    [ObservableProperty]
    private bool _showSidebarGameCount = true;

    public string RomsDirectory => _dataPaths.RomsRoot;
    public string MediaDirectory => _dataPaths.MediaRoot;
    public string BiosDirectory => _dataPaths.BiosRoot;
    public string SavesDirectory => _dataPaths.SavesRoot;

    public ObservableCollection<GameSystem> Systems { get; } = [];

    [ObservableProperty]
    private string _newSystemKey = string.Empty;

    [ObservableProperty]
    private string _newSystemName = string.Empty;

    [ObservableProperty]
    private string _newSystemExtensions = string.Empty;

    [ObservableProperty]
    private GameSystem? _selectedEmulatorSystem;

    public ObservableCollection<EmulatorProfile> EmulatorProfiles { get; } = [];
    public IReadOnlyList<EmulatorPreset> EmulatorPresets => ViewModels.EmulatorPreset.All;

    [ObservableProperty]
    private EmulatorPreset? _selectedEmulatorPreset;

    [ObservableProperty]
    private string _newEmulatorProfileName = string.Empty;

    [ObservableProperty]
    private string _newEmulatorExecutablePath = string.Empty;

    [ObservableProperty]
    private string _newEmulatorCommandLineTemplate = "\"{RomPath}\"";

    public SettingsViewModel(
        SettingsRepository settings,
        SystemRepository systems,
        EmulatorProfileRepository emulatorProfiles,
        PortableDataPaths dataPaths)
    {
        _settings = settings;
        _systems = systems;
        _emulatorProfiles = emulatorProfiles;
        _dataPaths = dataPaths;
        LoadFromStorage();
        ReloadSystems();
    }

    partial void OnSelectedEmulatorSystemChanged(GameSystem? value) => ReloadEmulatorProfiles();

    partial void OnSelectedEmulatorPresetChanged(EmulatorPreset? value)
    {
        if (value is not null)
        {
            NewEmulatorCommandLineTemplate = value.CommandLineTemplate;
            if (string.IsNullOrWhiteSpace(NewEmulatorProfileName))
            {
                NewEmulatorProfileName = value.Name;
            }
        }
    }

    private void ReloadEmulatorProfiles()
    {
        EmulatorProfiles.Clear();
        if (SelectedEmulatorSystem is null)
        {
            return;
        }

        foreach (var profile in _emulatorProfiles.GetBySystem(SelectedEmulatorSystem.Id))
        {
            EmulatorProfiles.Add(profile);
        }
    }

    [RelayCommand]
    private void AddEmulatorProfile()
    {
        if (SelectedEmulatorSystem is null || string.IsNullOrWhiteSpace(NewEmulatorProfileName) || string.IsNullOrWhiteSpace(NewEmulatorExecutablePath))
        {
            return;
        }

        var isFirstProfileForSystem = EmulatorProfiles.Count == 0;

        _emulatorProfiles.Add(new EmulatorProfile
        {
            SystemId = SelectedEmulatorSystem.Id,
            Name = NewEmulatorProfileName.Trim(),
            ExecutablePath = NewEmulatorExecutablePath.Trim(),
            CommandLineTemplate = string.IsNullOrWhiteSpace(NewEmulatorCommandLineTemplate) ? "\"{RomPath}\"" : NewEmulatorCommandLineTemplate.Trim(),
            IsDefault = isFirstProfileForSystem,
        });

        NewEmulatorProfileName = string.Empty;
        NewEmulatorExecutablePath = string.Empty;
        NewEmulatorCommandLineTemplate = "\"{RomPath}\"";
        SelectedEmulatorPreset = null;
        ReloadEmulatorProfiles();
    }

    [RelayCommand]
    private void RemoveEmulatorProfile(EmulatorProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        _emulatorProfiles.Delete(profile.Id);
        ReloadEmulatorProfiles();
    }

    [RelayCommand]
    private void SetDefaultEmulatorProfile(EmulatorProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        foreach (var existing in EmulatorProfiles)
        {
            if (existing.IsDefault && existing.Id != profile.Id)
            {
                existing.IsDefault = false;
                _emulatorProfiles.Update(existing);
            }
        }

        profile.IsDefault = true;
        _emulatorProfiles.Update(profile);
        ReloadEmulatorProfiles();
    }

    private void ReloadSystems()
    {
        Systems.Clear();
        foreach (var system in _systems.GetAll())
        {
            system.IconFullPath = system.IconPath is null ? null : _dataPaths.ResolveMediaPath(system.IconPath);
            Systems.Add(system);
        }
    }

    /// <summary>
    /// Raised when the user asks to remove a system, before anything is
    /// deleted. The view owns the confirmation dialog (naming the system
    /// and the exact number of games that would be affected) and the
    /// choice between deleting those games or reassigning them to another
    /// system, per the MVVM boundary — the ViewModel never shows UI itself.
    /// </summary>
    public event EventHandler<SystemRemovalRequestedEventArgs>? SystemRemovalRequested;

    [RelayCommand]
    private void AddSystem()
    {
        if (string.IsNullOrWhiteSpace(NewSystemKey) || string.IsNullOrWhiteSpace(NewSystemName))
        {
            return;
        }

        _systems.Add(new GameSystem
        {
            Key = NewSystemKey.Trim(),
            Name = NewSystemName.Trim(),
            RomExtensions = NewSystemExtensions.Trim(),
        });

        NewSystemKey = string.Empty;
        NewSystemName = string.Empty;
        NewSystemExtensions = string.Empty;
        ReloadSystems();
    }

    [RelayCommand]
    private void RemoveSystem(GameSystem? system)
    {
        if (system is null)
        {
            return;
        }

        var affectedGameCount = _systems.CountGamesForSystem(system.Id);
        var otherSystems = Systems.Where(s => s.Id != system.Id).ToList();

        SystemRemovalRequested?.Invoke(this, new SystemRemovalRequestedEventArgs(system, affectedGameCount, otherSystems));
    }

    /// <summary>
    /// Deletes <paramref name="system"/> outright (cascading to its games,
    /// per the database schema) after the view has obtained an explicit
    /// confirmation from the user.
    /// </summary>
    public void ConfirmDeleteSystemWithGames(GameSystem system)
    {
        _systems.Delete(system.Id);
        ReloadSystems();
    }

    /// <summary>
    /// Moves every game of <paramref name="system"/> to
    /// <paramref name="targetSystem"/> and then removes the now-empty
    /// system, as the non-destructive alternative chosen by the user.
    /// </summary>
    public void ReassignGamesAndDeleteSystem(GameSystem system, GameSystem targetSystem)
    {
        _systems.ReassignGamesAndDelete(system.Id, targetSystem.Id);
        ReloadSystems();
    }

    /// <summary>
    /// Copies a locally chosen image file into <c>data/Media/&lt;Key&gt;/</c>
    /// and persists it as the system's icon/logo. Never touches the
    /// network — the source file must already be on the local disk, picked
    /// by the user through a standard file-open dialog in the view.
    /// </summary>
    public void SetSystemIcon(GameSystem system, string sourceFilePath)
    {
        var systemMediaDirectory = Path.Combine(_dataPaths.MediaRoot, system.Key);
        Directory.CreateDirectory(systemMediaDirectory);

        var destinationFileName = "icon" + Path.GetExtension(sourceFilePath);
        var destinationPath = Path.Combine(systemMediaDirectory, destinationFileName);
        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        system.IconPath = Path.Combine(system.Key, destinationFileName);
        _systems.Update(system);
        ReloadSystems();
    }

    private void LoadFromStorage()
    {
        Language = _settings.Get(LanguageKey) ?? Language;
        IsDarkTheme = _settings.Get(ThemeKey) != "light";
        AccentColorHex = _settings.Get(AccentColorKey) ?? AccentColorHex;
        LaunchBehavior = Enum.TryParse<GameLaunchBehavior>(_settings.Get(LaunchBehaviorKey), out var behavior)
            ? behavior
            : GameLaunchBehavior.Minimize;
        ShowSidebarGameCount = _settings.Get(ShowSidebarGameCountKey) != "false";
    }

    public void SaveToStorage()
    {
        _settings.Set(LanguageKey, Language);
        _settings.Set(ThemeKey, IsDarkTheme ? "dark" : "light");
        _settings.Set(AccentColorKey, AccentColorHex);
        _settings.Set(LaunchBehaviorKey, LaunchBehavior.ToString());
        _settings.Set(ShowSidebarGameCountKey, ShowSidebarGameCount ? "true" : "false");
    }
}

/// <summary>
/// Carries everything the view needs to build the removal confirmation
/// dialog for a system: the system itself, how many games currently
/// reference it (for an exact, non-vague warning message), and the other
/// systems the user could reassign those games to instead of deleting them.
/// </summary>
public sealed class SystemRemovalRequestedEventArgs(GameSystem system, int affectedGameCount, IReadOnlyList<GameSystem> otherSystems) : EventArgs
{
    public GameSystem System { get; } = system;
    public int AffectedGameCount { get; } = affectedGameCount;
    public IReadOnlyList<GameSystem> OtherSystems { get; } = otherSystems;
}
