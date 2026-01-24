using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Views;

/// <summary>
/// About window displaying application information
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        // Get version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyAcrylicBackdrop();
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
