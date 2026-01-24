/*
 * Copyright 2026 Julien Bombled
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TwinShell.App.Behaviors;

/// <summary>
/// Attached behavior that creates a Windows 11 Fluent Design "Reveal Border" effect.
/// A linear gradient follows the mouse cursor position, making the border glow
/// strongest at the edge nearest to the cursor.
/// </summary>
public static class RevealBorderBehavior
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Border, RevealBorderState> _states = new();

    /// <summary>
    /// Debounce timer interval matching 60fps frame rate.
    /// </summary>
    private const int DebounceMs = 16;

    #region IsEnabled Attached Property

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(RevealBorderBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region Intensity Attached Property

    public static readonly DependencyProperty IntensityProperty =
        DependencyProperty.RegisterAttached(
            "Intensity",
            typeof(double),
            typeof(RevealBorderBehavior),
            new PropertyMetadata(0.40));

    public static double GetIntensity(DependencyObject obj) => (double)obj.GetValue(IntensityProperty);
    public static void SetIntensity(DependencyObject obj, double value) => obj.SetValue(IntensityProperty, value);

    #endregion

    #region RevealColor Attached Property

    public static readonly DependencyProperty RevealColorProperty =
        DependencyProperty.RegisterAttached(
            "RevealColor",
            typeof(Color),
            typeof(RevealBorderBehavior),
            new PropertyMetadata(Colors.White));

    public static Color GetRevealColor(DependencyObject obj) => (Color)obj.GetValue(RevealColorProperty);
    public static void SetRevealColor(DependencyObject obj, Color value) => obj.SetValue(RevealColorProperty, value);

    #endregion

    #region FallbackBrush Attached Property

    public static readonly DependencyProperty FallbackBrushProperty =
        DependencyProperty.RegisterAttached(
            "FallbackBrush",
            typeof(Brush),
            typeof(RevealBorderBehavior),
            new PropertyMetadata(null));

    public static Brush? GetFallbackBrush(DependencyObject obj) => (Brush?)obj.GetValue(FallbackBrushProperty);
    public static void SetFallbackBrush(DependencyObject obj, Brush? value) => obj.SetValue(FallbackBrushProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
            return;

        bool isEnabled = (bool)e.NewValue;

        if (isEnabled)
        {
            AttachBehavior(border);
        }
        else
        {
            DetachBehavior(border);
        }
    }

    private static void AttachBehavior(Border border)
    {
        // Store original border brush for restoration
        var state = new RevealBorderState(border, border.BorderBrush);
        _states.AddOrUpdate(border, state);

        WeakEventManager<Border, MouseEventArgs>.AddHandler(
            border, nameof(Border.MouseEnter), OnMouseEnter);
        WeakEventManager<Border, MouseEventArgs>.AddHandler(
            border, nameof(Border.MouseLeave), OnMouseLeave);
        WeakEventManager<Border, MouseEventArgs>.AddHandler(
            border, nameof(Border.MouseMove), OnMouseMove);
    }

    private static void DetachBehavior(Border border)
    {
        WeakEventManager<Border, MouseEventArgs>.RemoveHandler(
            border, nameof(Border.MouseEnter), OnMouseEnter);
        WeakEventManager<Border, MouseEventArgs>.RemoveHandler(
            border, nameof(Border.MouseLeave), OnMouseLeave);
        WeakEventManager<Border, MouseEventArgs>.RemoveHandler(
            border, nameof(Border.MouseMove), OnMouseMove);

        if (_states.TryGetValue(border, out var state))
        {
            // Restore original brush
            border.BorderBrush = state.OriginalBrush;
            state.Dispose();
            _states.Remove(border);
        }
    }

    private static void OnMouseEnter(object? sender, MouseEventArgs e)
    {
        if (sender is not Border border)
            return;

        // Check if reduced motion is enabled
        if (IsReducedMotionEnabled())
            return;

        if (!_states.TryGetValue(border, out var state))
            return;

        state.IsActive = true;
        state.EnsureGradientCreated();
        UpdateRevealBorder(border, e.GetPosition(border));
    }

    private static void OnMouseLeave(object? sender, MouseEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (!_states.TryGetValue(border, out var state))
            return;

        state.IsActive = false;

        // Restore original or fallback brush
        var fallbackBrush = GetFallbackBrush(border);
        border.BorderBrush = fallbackBrush ?? state.OriginalBrush;
    }

    private static void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (!_states.TryGetValue(border, out var state))
            return;

        if (!state.IsActive)
            return;

        // Debounce mouse move events to maintain 60fps
        if (!state.ShouldUpdate())
            return;

        UpdateRevealBorder(border, e.GetPosition(border));
    }

    private static void UpdateRevealBorder(Border border, Point position)
    {
        if (!_states.TryGetValue(border, out var state))
            return;

        if (state.RevealBrush == null)
            return;

        double width = border.ActualWidth;
        double height = border.ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        // Calculate angle from center to mouse position
        double centerX = width / 2;
        double centerY = height / 2;
        double deltaX = position.X - centerX;
        double deltaY = position.Y - centerY;
        double angle = Math.Atan2(deltaY, deltaX);

        // Convert angle to gradient start/end points
        // Gradient should be brightest at the edge closest to the cursor
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);

        // Calculate gradient endpoints (from center towards cursor, then away)
        var startPoint = new Point(0.5 + cos * 0.5, 0.5 + sin * 0.5);
        var endPoint = new Point(0.5 - cos * 0.5, 0.5 - sin * 0.5);

        state.RevealBrush.StartPoint = startPoint;
        state.RevealBrush.EndPoint = endPoint;

        border.BorderBrush = state.RevealBrush;
    }

    private static bool IsReducedMotionEnabled()
    {
        try
        {
            return SystemParameters.ClientAreaAnimation == false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Internal state class for managing reveal border effect per element.
    /// </summary>
    private sealed class RevealBorderState : IDisposable
    {
        private readonly Border _border;
        private DateTime _lastUpdate = DateTime.MinValue;

        public Brush? OriginalBrush { get; }
        public LinearGradientBrush? RevealBrush { get; private set; }
        public bool IsActive { get; set; }

        public RevealBorderState(Border border, Brush? originalBrush)
        {
            _border = border;
            OriginalBrush = originalBrush;
        }

        public bool ShouldUpdate()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdate).TotalMilliseconds < DebounceMs)
                return false;

            _lastUpdate = now;
            return true;
        }

        public void EnsureGradientCreated()
        {
            if (RevealBrush != null)
                return;

            var color = GetRevealColor(_border);
            var intensity = GetIntensity(_border);

            // Create gradient stops
            var stops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb((byte)(255 * intensity), color.R, color.G, color.B), 0),
                new GradientStop(Color.FromArgb((byte)(255 * intensity * 0.3), color.R, color.G, color.B), 0.5),
                new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            };
            stops.Freeze();

            RevealBrush = new LinearGradientBrush(stops, new Point(0.5, 0), new Point(0.5, 1));
        }

        public void Dispose()
        {
            RevealBrush = null;
        }
    }
}
