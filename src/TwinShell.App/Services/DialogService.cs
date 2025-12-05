using System.Windows;
using Microsoft.Win32;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Services;

/// <summary>
/// WPF implementation of dialog service using MessageBox and file dialogs.
/// </summary>
public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowSuccess(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public bool ShowQuestion(string message, string title)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public string? ShowSaveFileDialog(string filter, string defaultExtension, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExtension,
            FileName = defaultFileName
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowOpenFileDialog(string filter, string defaultExtension)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExtension
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowFolderBrowserDialog(string description, string? initialPath = null)
    {
        // Use OpenFolderDialog (available in .NET 8+ WPF)
        var dialog = new OpenFolderDialog
        {
            Title = description,
            Multiselect = false
        };

        if (!string.IsNullOrEmpty(initialPath) && System.IO.Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
