using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.ViewModels;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Views;

/// <summary>
/// Window for managing custom categories.
/// </summary>
public partial class CategoryManagementWindow : Window
{
    public CategoryManagementWindow(CategoryManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
            ApplyAcrylicBackdrop();
        };
    }

    private void ApplyAcrylicBackdrop()
    {
        var backdropService = App.ServiceProvider?.GetService<IBackdropEffectService>();
        if (backdropService?.IsBackdropEffectSupported == true)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var isDark = Application.Current.Resources["BackgroundBrush"] is SolidColorBrush brush
                         && brush.Color.R < 128;
            if (backdropService.ApplyAcrylic(hwnd, isDark))
            {
                Background = Brushes.Transparent;
            }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // Custom Title Bar Button Handlers
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
