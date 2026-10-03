// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>Small modal collecting a single tag name to apply to every checked row in the catalog view.</summary>
[SupportedOSPlatform("windows")]
public partial class BulkAddTagWindow : FluentWindow
{
    public string? TagName { get; private set; }

    public BulkAddTagWindow(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TagTextBox.Text))
        {
            return;
        }

        TagName = TagTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
