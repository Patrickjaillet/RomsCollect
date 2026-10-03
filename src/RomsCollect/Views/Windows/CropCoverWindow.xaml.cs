// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// Simple fixed-ratio (3:4 portrait, the standard box-art ratio) crop tool:
/// drag the image under the frame and zoom with the slider, then
/// <see cref="CroppedImagePath"/> is written as a new local file — never a
/// network fetch, the source is always a file already on disk.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class CropCoverWindow : FluentWindow
{
    private readonly BitmapImage _sourceBitmap;
    private Point _dragStartMouse;
    private Point _dragStartOffset;
    private bool _isDragging;

    /// <summary>Set after a successful confirm, pointing at a freshly written cropped copy of the source file.</summary>
    public string? CroppedImagePath { get; private set; }

    public CropCoverWindow(string sourceFilePath)
    {
        InitializeComponent();

        _sourceBitmap = new BitmapImage();
        _sourceBitmap.BeginInit();
        _sourceBitmap.CacheOption = BitmapCacheOption.OnLoad;
        _sourceBitmap.UriSource = new Uri(sourceFilePath, UriKind.Absolute);
        _sourceBitmap.EndInit();

        SourceImage.Source = _sourceBitmap;

        // Start zoomed so the frame (300x400) is already fully covered,
        // whichever dimension of the source image is the limiting one.
        var initialScale = Math.Max(300.0 / _sourceBitmap.PixelWidth, 400.0 / _sourceBitmap.PixelHeight);
        ZoomSlider.Minimum = initialScale;
        ZoomSlider.Value = initialScale;
        ApplyTransform(initialScale, 0, 0);
    }

    private void ApplyTransform(double scale, double offsetX, double offsetY)
    {
        ImageScaleTransform.ScaleX = scale;
        ImageScaleTransform.ScaleY = scale;
        ImageTranslateTransform.X = offsetX;
        ImageTranslateTransform.Y = offsetY;
    }

    private void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => ApplyTransform(ZoomSlider.Value, ImageTranslateTransform.X, ImageTranslateTransform.Y);

    private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartMouse = e.GetPosition(CropCanvas);
        _dragStartOffset = new Point(ImageTranslateTransform.X, ImageTranslateTransform.Y);
        CropCanvas.CaptureMouse();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var current = e.GetPosition(CropCanvas);
        var deltaX = current.X - _dragStartMouse.X;
        var deltaY = current.Y - _dragStartMouse.Y;
        ApplyTransform(ZoomSlider.Value, _dragStartOffset.X + deltaX, _dragStartOffset.Y + deltaY);
    }

    private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        CropCanvas.ReleaseMouseCapture();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        const int frameWidth = 300;
        const int frameHeight = 400;

        var renderTarget = new RenderTargetBitmap(frameWidth, frameHeight, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(CropCanvas);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        var tempPath = Path.Combine(Path.GetTempPath(), $"romscollect_crop_{Guid.NewGuid():N}.png");
        using (var stream = File.Create(tempPath))
        {
            encoder.Save(stream);
        }

        CroppedImagePath = tempPath;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
