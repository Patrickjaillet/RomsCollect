// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using RomsCollect.Helpers;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// The mandatory "About" window: shows the logo, copyright, license, contact
/// e-mail and website, per the project's licensing constraints. The e-mail
/// and website links are opened via the system's default handler
/// (<see cref="Process.Start"/> with <c>UseShellExecute = true</c>) — an
/// explicit user action outside RomsCollect's own offline runtime.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class AboutWindow : FluentWindow
{
    public AboutWindow()
    {
        InitializeComponent();

        var logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "png", "logo.png");
        if (File.Exists(logoPath))
        {
            LogoImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(logoPath));
            LogoImage.Visibility = Visibility.Visible;
        }
    }

    private void OnContactEmailClick(object sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo($"mailto:{App.Localization["About.ContactEmail"]}") { UseShellExecute = true });

    private void OnWebsiteClick(object sender, RoutedEventArgs e)
        => Process.Start(new ProcessStartInfo(App.Localization["About.Website"]) { UseShellExecute = true });

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
