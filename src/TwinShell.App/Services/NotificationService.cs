using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Services;

/// <summary>
/// WPF implementation of notification service using toast-style popups.
/// </summary>
public class NotificationService : INotificationService, IDisposable
{
    private readonly DispatcherTimer _timer;
    private Popup? _currentPopup;
    private bool _disposed;

    public NotificationService()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    public void ShowInfo(string message, string? title = null, int durationSeconds = 3)
    {
        ShowNotification(message, title ?? "Information", Colors.CornflowerBlue, durationSeconds);
    }

    public void ShowSuccess(string message, string? title = null, int durationSeconds = 3)
    {
        ShowNotification(message, title ?? "Success", Colors.MediumSeaGreen, durationSeconds);
    }

    public void ShowWarning(string message, string? title = null, int durationSeconds = 4)
    {
        ShowNotification(message, title ?? "Warning", Colors.Orange, durationSeconds);
    }

    public void ShowError(string message, string? title = null, int durationSeconds = 5)
    {
        ShowNotification(message, title ?? "Error", Colors.IndianRed, durationSeconds);
    }

    private void ShowNotification(string message, string title, Color accentColor, int durationSeconds)
    {
        // Close existing popup if any
        CloseCurrentPopup();

        // Create notification UI
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)), // Dark background
            BorderBrush = new SolidColorBrush(accentColor),
            BorderThickness = new Thickness(0, 0, 0, 3),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 12, 16, 12),
            MinWidth = 300,
            MaxWidth = 400,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.3
            }
        };

        var stackPanel = new StackPanel();

        // Title
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 0, 4)
        };

        // Message
        var messageBlock = new TextBlock
        {
            Text = message,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            TextWrapping = TextWrapping.Wrap
        };

        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(messageBlock);
        border.Child = stackPanel;

        // BUGFIX: Check if MainWindow is not null before creating popup
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow == null)
            return; // Cannot show popup without main window

        // Create popup
        _currentPopup = new Popup
        {
            Child = border,
            PlacementTarget = mainWindow,
            Placement = PlacementMode.Top,
            HorizontalOffset = (mainWindow.ActualWidth) - 420,
            VerticalOffset = 60,
            AllowsTransparency = true,
            StaysOpen = true
        };

        // Fade in animation
        border.Opacity = 0;
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _currentPopup.IsOpen = true;
        border.BeginAnimation(UIElement.OpacityProperty, fadeIn);

        // Set timer to close
        _timer.Interval = TimeSpan.FromSeconds(durationSeconds);
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timer.Stop();
        CloseCurrentPopup();
    }

    private void CloseCurrentPopup()
    {
        if (_currentPopup?.Child is Border border)
        {
            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += OnFadeOutCompleted;

            border.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else if (_currentPopup != null)
        {
            _currentPopup.IsOpen = false;
            _currentPopup = null;
        }
    }

    /// <summary>
    /// Fade out animation completed handler
    /// </summary>
    private void OnFadeOutCompleted(object? sender, EventArgs e)
    {
        // Detach event handler to prevent memory leak
        if (sender is DoubleAnimation animation)
        {
            animation.Completed -= OnFadeOutCompleted;
        }

        if (_currentPopup != null)
        {
            _currentPopup.IsOpen = false;
            _currentPopup = null;
        }
    }

    /// <summary>
    /// Dispose resources to prevent memory leaks
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Stop and detach timer event handler
        _timer.Stop();
        _timer.Tick -= Timer_Tick;

        // Close current popup if any
        if (_currentPopup != null)
        {
            _currentPopup.IsOpen = false;
            _currentPopup = null;
        }

        _disposed = true;
    }
}
