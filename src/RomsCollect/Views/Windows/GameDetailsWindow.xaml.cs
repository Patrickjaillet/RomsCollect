// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using RomsCollect.Helpers;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// Full game details page: download links, manual, and play history.
/// Opening a download link or a manual file is always confirmed explicitly
/// and performed via the system's default handler
/// (<see cref="Process.Start"/> with <c>UseShellExecute = true</c>) — an
/// explicit user action outside RomsCollect's own offline runtime.
/// RomsCollect itself never performs any network request or availability
/// check against a stored link.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class GameDetailsWindow : FluentWindow
{
    private readonly GameDetailsViewModel _viewModel;
    private readonly DispatcherTimer _videoPositionTimer;
    private bool _isScrubbing;

    /// <summary>Raised when the user clicks Play on this page, for the owner window to launch the game.</summary>
    public event EventHandler? PlayRequested;

    public GameDetailsWindow(GameDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.OpenExternalRequested += OnOpenExternalRequested;
        _viewModel.PlayRequested += (_, _) => PlayRequested?.Invoke(this, EventArgs.Empty);

        DescriptionTextBlock.Inlines.Clear();
        LightweightMarkupParser.AppendInlinesTo(DescriptionTextBlock.Inlines, viewModel.Game.Description ?? string.Empty);

        _videoPositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _videoPositionTimer.Tick += (_, _) =>
        {
            if (!_isScrubbing)
            {
                VideoScrubber.Value = VideoPlayer.Position.TotalSeconds;
            }
        };

        Unloaded += (_, _) => _videoPositionTimer.Stop();
    }

    private void OnVideoMediaOpened(object sender, RoutedEventArgs e)
    {
        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            VideoScrubber.Maximum = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }
    }

    private void OnVideoMediaEnded(object sender, RoutedEventArgs e)
    {
        _videoPositionTimer.Stop();
        VideoPlayer.Position = TimeSpan.Zero;
        VideoScrubber.Value = 0;
        VideoPlayPauseButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 };
    }

    private void OnVideoPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (_videoPositionTimer.IsEnabled)
        {
            VideoPlayer.Pause();
            _videoPositionTimer.Stop();
            VideoPlayPauseButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 };
        }
        else
        {
            VideoPlayer.Play();
            _videoPositionTimer.Start();
            VideoPlayPauseButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Pause24 };
        }
    }

    private void OnVideoScrubberPreviewMouseDown(object sender, MouseButtonEventArgs e) => _isScrubbing = true;

    private void OnVideoScrubberPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isScrubbing = false;
        VideoPlayer.Position = TimeSpan.FromSeconds(VideoScrubber.Value);
    }

    private void OnGalleryThumbnailClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string imagePath })
        {
            _viewModel.SelectGalleryImage(imagePath);
        }
    }

    private void OnOpenExternalRequested(object? sender, string target)
    {
        var message = string.Format(App.Localization["GameDetails.DownloadLinks.OpenConfirmMessage"], target);
        var result = System.Windows.MessageBox.Show(
            this,
            message,
            App.Localization["GameDetails.DownloadLinks.OpenConfirmTitle"],
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
    }

    private void OnImportManualClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = App.Localization["GameDetails.Manual.Import"],
            Filter = "Documents|*.pdf;*.png;*.jpg;*.jpeg;*.txt",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.ImportManualFromFile(dialog.FileName);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
