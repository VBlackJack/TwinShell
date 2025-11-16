using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TwinShell.App.Services;

/// <summary>
/// Service for displaying temporary snackbar notifications
/// </summary>
public class SnackBarService
{
    private static SnackBarService? _instance;
    private Border? _snackBar;
    private Panel? _container;
    private DispatcherTimer? _timer;

    public static SnackBarService Instance => _instance ??= new SnackBarService();

    private SnackBarService()
    {
    }

    /// <summary>
    /// Initialize the snackbar service with a container panel
    /// </summary>
    public void Initialize(Panel container)
    {
        _container = container;
        CreateSnackBar();
    }

    private void CreateSnackBar()
    {
        if (_container == null) return;

        _snackBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(16),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            MinWidth = 200,
            MaxWidth = 500,
            Opacity = 0,
            Visibility = Visibility.Collapsed,
            Child = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        Panel.SetZIndex(_snackBar, 9999);
        _container.Children.Add(_snackBar);

        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    /// <summary>
    /// Show a success message
    /// </summary>
    public void ShowSuccess(string message, int durationMs = 3000)
    {
        Show(message, Color.FromRgb(76, 175, 80), durationMs); // Success green
    }

    /// <summary>
    /// Show an error message
    /// </summary>
    public void ShowError(string message, int durationMs = 3000)
    {
        Show(message, Color.FromRgb(231, 72, 86), durationMs); // Danger red
    }

    /// <summary>
    /// Show an info message
    /// </summary>
    public void ShowInfo(string message, int durationMs = 3000)
    {
        Show(message, Color.FromRgb(33, 150, 243), durationMs); // Info blue
    }

    /// <summary>
    /// Show a warning message
    /// </summary>
    public void ShowWarning(string message, int durationMs = 3000)
    {
        Show(message, Color.FromRgb(255, 193, 7), durationMs); // Warning amber
    }

    private void Show(string message, Color backgroundColor, int durationMs)
    {
        if (_snackBar == null || _container == null) return;

        // Stop any existing timer
        _timer?.Stop();

        // Update message and color
        if (_snackBar.Child is TextBlock textBlock)
        {
            textBlock.Text = message;
        }

        _snackBar.Background = new SolidColorBrush(backgroundColor);

        // Set automation properties for screen readers
        AutomationProperties.SetLiveSetting(_snackBar, AutomationLiveSetting.Assertive);
        AutomationProperties.SetName(_snackBar, "Notification: " + message);

        // Show with fade-in animation
        _snackBar.Visibility = Visibility.Visible;

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _snackBar.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        // Set timer to hide
        _timer.Interval = TimeSpan.FromMilliseconds(durationMs);
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer?.Stop();
        Hide();
    }

    private void Hide()
    {
        if (_snackBar == null) return;

        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (s, e) =>
        {
            _snackBar.Visibility = Visibility.Collapsed;
        };

        _snackBar.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
