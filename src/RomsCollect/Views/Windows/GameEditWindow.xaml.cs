// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using Microsoft.Win32;
using RomsCollect.Helpers;
using RomsCollect.ViewModels;
using Wpf.Ui.Controls;

namespace RomsCollect.Views.Windows;

/// <summary>
/// Tabbed game-edit dialog (Main / Personal / Cover / Description). The
/// cover file picker is a local-file-only dialog — RomsCollect never
/// searches for cover art online.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class GameEditWindow : FluentWindow
{
    private readonly GameEditViewModel _viewModel;

    public GameEditWindow(GameEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.Saved += (_, _) => { DialogResult = true; Close(); };
        _viewModel.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }

    private void OnImportCoverClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = App.Localization["GameEdit.SelectCoverImage.Title"],
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var cropWindow = new CropCoverWindow(dialog.FileName) { Owner = this };
        if (cropWindow.ShowDialog() == true && cropWindow.CroppedImagePath is { } croppedPath)
        {
            _viewModel.ImportCoverFromFile(croppedPath);
            File.Delete(croppedPath);
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e) => _viewModel.SaveCommand.Execute(null);

    private void OnCancelClick(object sender, RoutedEventArgs e) => _viewModel.CancelCommand.Execute(null);

    private void OnDescriptionBoldClick(object sender, RoutedEventArgs e) => WrapDescriptionSelection("**", "**");

    private void OnDescriptionItalicClick(object sender, RoutedEventArgs e) => WrapDescriptionSelection("*", "*");

    private void OnDescriptionBulletClick(object sender, RoutedEventArgs e)
    {
        var caretIndex = DescriptionTextBox.CaretIndex;
        DescriptionTextBox.Text = DescriptionTextBox.Text.Insert(caretIndex, "- ");
        DescriptionTextBox.CaretIndex = caretIndex + 2;
        DescriptionTextBox.Focus();
    }

    /// <summary>
    /// Wraps the current selection with lightweight markup (e.g. "**" for
    /// bold), or inserts an empty pair with the caret placed between them
    /// when nothing is selected.
    /// </summary>
    private void WrapDescriptionSelection(string prefix, string suffix)
    {
        var selectionStart = DescriptionTextBox.SelectionStart;
        var selectionLength = DescriptionTextBox.SelectionLength;
        var selectedText = DescriptionTextBox.Text.Substring(selectionStart, selectionLength);

        DescriptionTextBox.Text = DescriptionTextBox.Text
            .Remove(selectionStart, selectionLength)
            .Insert(selectionStart, $"{prefix}{selectedText}{suffix}");

        DescriptionTextBox.CaretIndex = selectionStart + prefix.Length + selectedText.Length + suffix.Length;
        DescriptionTextBox.Focus();
    }
}
