/*
 * Copyright 2025 Julien Bombled
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Services;

/// <summary>
/// Service for displaying temporary snackbar notifications.
/// Implements INotificationService for dependency injection.
/// </summary>
public class SnackBarService : INotificationService, IDisposable
{
    private Border? _snackBar;
    private Panel? _container;
    private DispatcherTimer? _timer;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the SnackBarService.
    /// Call Initialize() with a Panel container before using Show methods.
    /// </summary>
    public SnackBarService()
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

    /// <inheritdoc />
    public void ShowSuccess(string message, string? title = null, int durationSeconds = 3)
    {
        Show(message, SnackBarColors.Success, durationSeconds * 1000);
    }

    /// <inheritdoc />
    public void ShowError(string message, string? title = null, int durationSeconds = 5)
    {
        Show(message, SnackBarColors.Error, durationSeconds * 1000);
    }

    /// <inheritdoc />
    public void ShowInfo(string message, string? title = null, int durationSeconds = 3)
    {
        Show(message, SnackBarColors.Info, durationSeconds * 1000);
    }

    /// <inheritdoc />
    public void ShowWarning(string message, string? title = null, int durationSeconds = 4)
    {
        Show(message, SnackBarColors.Warning, durationSeconds * 1000);
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
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(durationMs);
            _timer.Start();
        }
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

    /// <summary>
    /// Disposes resources used by the SnackBarService.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _timer?.Stop();
            _timer = null;

            if (_container != null && _snackBar != null)
            {
                _container.Children.Remove(_snackBar);
            }
            _snackBar = null;
            _container = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// Color constants for snackbar notifications.
    /// </summary>
    private static class SnackBarColors
    {
        public static readonly Color Success = Color.FromRgb(76, 175, 80);
        public static readonly Color Error = Color.FromRgb(231, 72, 86);
        public static readonly Color Info = Color.FromRgb(33, 150, 243);
        public static readonly Color Warning = Color.FromRgb(255, 193, 7);
    }
}
