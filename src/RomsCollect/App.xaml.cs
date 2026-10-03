// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using RomsCollect.Helpers;
using RomsCollect.Services;
using RomsCollect.Services.Database;
using RomsCollect.ViewModels;
using Wpf.Ui.Appearance;

namespace RomsCollect;

[SupportedOSPlatform("windows")]
public partial class App : Application
{
    public static LocalizationService Localization { get; } = new();
    public static SqliteConnectionFactory Database { get; } = new();
    public static PortableDataPaths DataPaths { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        new DatabaseMigrator(Database).MigrateToLatest();

        var systems = new SystemRepository(Database);
        var games = new GameRepository(Database);
        var media = new GameMediaRepository(Database);
        var playHistory = new PlayHistoryRepository(Database);
        var ownership = new GameOwnershipRepository(Database);
        var settings = new SettingsRepository(Database);
        var collections = new CollectionRepository(Database);

        var mainViewModel = new MainViewModel(systems, games, media, playHistory, ownership, settings, collections, DataPaths);
        MainWindow = new MainWindow(mainViewModel);
        MainWindow.Show();
    }
}
