// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>Small modal collecting a Collection Status value to apply to every checked row in the catalog view.</summary>
[SupportedOSPlatform("windows")]
public partial class BulkSetCollectionStatusWindow : FluentWindow
{
    public string? SelectedStatus { get; private set; }

    public BulkSetCollectionStatusWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
        StatusComboBox.SelectedIndex = 0;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (StatusComboBox.SelectedItem is not ComboBoxItem { Content: string status })
        {
            return;
        }

        SelectedStatus = status;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
