// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Controls;

[SupportedOSPlatform("windows")]
public partial class GameDetailPanelControl : UserControl
{
    /// <summary>Raised when the user clicks the Edit button, carrying the currently shown game.</summary>
    public event EventHandler<GameCardViewModel>? EditRequested;

    /// <summary>Raised when the user clicks the "More details" button, carrying the currently shown game.</summary>
    public event EventHandler<GameCardViewModel>? MoreDetailsRequested;

    /// <summary>Raised when the user toggles the Favorite quick action.</summary>
    public event EventHandler<GameCardViewModel>? ToggleFavoriteRequested;

    /// <summary>Raised when the user toggles the Completed quick action.</summary>
    public event EventHandler<GameCardViewModel>? ToggleCompletedRequested;

    /// <summary>Raised when the user clicks Delete, carrying the currently shown game. The confirmation dialog is the view's responsibility.</summary>
    public event EventHandler<GameCardViewModel>? DeleteRequested;

    /// <summary>Raised when the user clicks "Open ROM Folder", carrying the currently shown game.</summary>
    public event EventHandler<GameCardViewModel>? OpenRomFolderRequested;

    /// <summary>Raised when the user clicks Play, carrying the currently shown game.</summary>
    public event EventHandler<GameCardViewModel>? PlayRequested;

    private readonly DispatcherTimer _videoPositionTimer;
    private bool _isScrubbing;

    public GameDetailPanelControl()
    {
        InitializeComponent();

        _videoPositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _videoPositionTimer.Tick += (_, _) =>
        {
            if (!_isScrubbing)
            {
                VideoScrubber.Value = VideoPlayer.Position.TotalSeconds;
            }
        };

        DataContextChanged += (_, _) => StopVideoAndResetControls();
        Unloaded += (_, _) => _videoPositionTimer.Stop();
    }

    private void StopVideoAndResetControls()
    {
        _videoPositionTimer.Stop();
        VideoPlayer.Stop();
        VideoPlayer.Position = TimeSpan.Zero;
        VideoScrubber.Value = 0;
        VideoPlayPauseButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 };
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

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            EditRequested?.Invoke(this, game);
        }
    }

    private void OnMoreDetailsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            MoreDetailsRequested?.Invoke(this, game);
        }
    }

    private void OnToggleFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            ToggleFavoriteRequested?.Invoke(this, game);
        }
    }

    private void OnToggleCompletedClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            ToggleCompletedRequested?.Invoke(this, game);
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            DeleteRequested?.Invoke(this, game);
        }
    }

    private void OnOpenRomFolderClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            OpenRomFolderRequested?.Invoke(this, game);
        }
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameCardViewModel game)
        {
            PlayRequested?.Invoke(this, game);
        }
    }

    private void OnFilmstripThumbnailClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is GameCardViewModel game && sender is FrameworkElement { DataContext: string thumbnailPath })
        {
            game.SelectedPreviewImagePath = thumbnailPath;
        }
    }
}
