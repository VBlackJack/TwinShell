using System.Windows;

namespace TwinShell.App.Views;

/// <summary>
/// About window displaying application information
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
