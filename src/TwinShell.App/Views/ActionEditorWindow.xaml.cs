using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.App.ViewModels;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Views;

/// <summary>
/// Window for creating and editing actions
/// </summary>
public partial class ActionEditorWindow : Window
{
    private readonly ActionEditorViewModel _viewModel;

    public ActionEditorWindow(ActionEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Subscribe to dialog result changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

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

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ActionEditorViewModel.DialogResult))
        {
            if (_viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult.Value;
                Close();
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        base.OnClosed(e);
    }

    // Custom Title Bar Button Handlers
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
