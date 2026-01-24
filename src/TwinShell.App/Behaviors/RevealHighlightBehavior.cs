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
using System.Windows.Threading;

namespace TwinShell.App.Behaviors;

/// <summary>
/// Attached behavior that creates a Windows 11 Fluent Design "Reveal Highlight" effect.
/// A radial gradient follows the mouse cursor position within the element bounds,
/// creating a spotlight effect on hover.
/// </summary>
public static class RevealHighlightBehavior
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkElement, RevealState> _states = new();

    /// <summary>
    /// Debounce timer interval matching 60fps frame rate.
    /// </summary>
    private const int DebounceMs = 16;

    #region IsEnabled Attached Property

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(RevealHighlightBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region Radius Attached Property

    public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.RegisterAttached(
            "Radius",
            typeof(double),
            typeof(RevealHighlightBehavior),
            new PropertyMetadata(120.0));

    public static double GetRadius(DependencyObject obj) => (double)obj.GetValue(RadiusProperty);
    public static void SetRadius(DependencyObject obj, double value) => obj.SetValue(RadiusProperty, value);

    #endregion

    #region Intensity Attached Property

    public static readonly DependencyProperty IntensityProperty =
        DependencyProperty.RegisterAttached(
            "Intensity",
            typeof(double),
            typeof(RevealHighlightBehavior),
            new PropertyMetadata(0.10));

    public static double GetIntensity(DependencyObject obj) => (double)obj.GetValue(IntensityProperty);
    public static void SetIntensity(DependencyObject obj, double value) => obj.SetValue(IntensityProperty, value);

    #endregion

    #region RevealColor Attached Property

    public static readonly DependencyProperty RevealColorProperty =
        DependencyProperty.RegisterAttached(
            "RevealColor",
            typeof(Color),
            typeof(RevealHighlightBehavior),
            new PropertyMetadata(Colors.White));

    public static Color GetRevealColor(DependencyObject obj) => (Color)obj.GetValue(RevealColorProperty);
    public static void SetRevealColor(DependencyObject obj, Color value) => obj.SetValue(RevealColorProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        bool isEnabled = (bool)e.NewValue;

        if (isEnabled)
        {
            AttachBehavior(element);
        }
        else
        {
            DetachBehavior(element);
        }
    }

    private static void AttachBehavior(FrameworkElement element)
    {
        var state = new RevealState(element);
        _states.AddOrUpdate(element, state);

        WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(
            element, nameof(FrameworkElement.MouseEnter), OnMouseEnter);
        WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(
            element, nameof(FrameworkElement.MouseLeave), OnMouseLeave);
        WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(
            element, nameof(FrameworkElement.MouseMove), OnMouseMove);
    }

    private static void DetachBehavior(FrameworkElement element)
    {
        WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(
            element, nameof(FrameworkElement.MouseEnter), OnMouseEnter);
        WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(
            element, nameof(FrameworkElement.MouseLeave), OnMouseLeave);
        WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(
            element, nameof(FrameworkElement.MouseMove), OnMouseMove);

        if (_states.TryGetValue(element, out var state))
        {
            state.Dispose();
            _states.Remove(element);
        }
    }

    private static void OnMouseEnter(object? sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        // Check if reduced motion is enabled
        if (IsReducedMotionEnabled())
            return;

        if (!_states.TryGetValue(element, out var state))
            return;

        state.IsActive = true;
        state.EnsureOverlayCreated();
        UpdateRevealPosition(element, e.GetPosition(element));
    }

    private static void OnMouseLeave(object? sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (!_states.TryGetValue(element, out var state))
            return;

        state.IsActive = false;
        state.HideOverlay();
    }

    private static void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        if (!_states.TryGetValue(element, out var state))
            return;

        if (!state.IsActive)
            return;

        // Debounce mouse move events to maintain 60fps
        if (!state.ShouldUpdate())
            return;

        UpdateRevealPosition(element, e.GetPosition(element));
    }

    private static void UpdateRevealPosition(FrameworkElement element, Point position)
    {
        if (!_states.TryGetValue(element, out var state))
            return;

        if (state.RevealBrush == null)
            return;

        double width = element.ActualWidth;
        double height = element.ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        // Convert position to relative coordinates (0-1)
        double relativeX = Math.Clamp(position.X / width, 0, 1);
        double relativeY = Math.Clamp(position.Y / height, 0, 1);

        // Update gradient center
        state.RevealBrush.GradientOrigin = new Point(relativeX, relativeY);
        state.RevealBrush.Center = new Point(relativeX, relativeY);

        // Calculate radius relative to element size
        double radius = GetRadius(element);
        double radiusX = radius / width;
        double radiusY = radius / height;

        state.RevealBrush.RadiusX = radiusX;
        state.RevealBrush.RadiusY = radiusY;
    }

    private static bool IsReducedMotionEnabled()
    {
        // Check system reduced motion setting
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
    /// Internal state class for managing reveal effect per element.
    /// </summary>
    private sealed class RevealState : IDisposable
    {
        private readonly FrameworkElement _element;
        private Border? _overlayBorder;
        private DateTime _lastUpdate = DateTime.MinValue;

        public RadialGradientBrush? RevealBrush { get; private set; }
        public bool IsActive { get; set; }

        public RevealState(FrameworkElement element)
        {
            _element = element;
        }

        public bool ShouldUpdate()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdate).TotalMilliseconds < DebounceMs)
                return false;

            _lastUpdate = now;
            return true;
        }

        public void EnsureOverlayCreated()
        {
            if (_overlayBorder != null)
            {
                _overlayBorder.Visibility = Visibility.Visible;
                return;
            }

            // Get reveal settings
            var color = GetRevealColor(_element);
            var intensity = GetIntensity(_element);

            // Create frozen gradient stops for performance
            var stops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb((byte)(255 * intensity), color.R, color.G, color.B), 0),
                new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1)
            };
            stops.Freeze();

            // Create reveal brush
            RevealBrush = new RadialGradientBrush(stops)
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5
            };

            // Create overlay border
            _overlayBorder = new Border
            {
                Background = RevealBrush,
                IsHitTestVisible = false,
                Opacity = 1
            };

            // Add overlay to element's visual tree
            if (_element is Panel panel)
            {
                panel.Children.Add(_overlayBorder);
            }
            else if (_element is Border border && border.Child is Panel childPanel)
            {
                // If the element is a Border with a Panel child, add to the panel
                childPanel.Children.Add(_overlayBorder);
            }
            else if (_element is ContentControl contentControl)
            {
                // Wrap existing content in a Grid with the overlay
                var existingContent = contentControl.Content;
                var grid = new Grid();

                if (existingContent is UIElement uiElement)
                {
                    contentControl.Content = null;
                    grid.Children.Add(uiElement);
                }

                grid.Children.Add(_overlayBorder);
                contentControl.Content = grid;
            }
            else if (_element is Decorator decorator)
            {
                // For Border, wrap in adorner layer approach using Grid
                var existingChild = decorator.Child;
                var grid = new Grid();

                if (existingChild != null)
                {
                    decorator.Child = null;
                    grid.Children.Add(existingChild);
                }

                grid.Children.Add(_overlayBorder);
                decorator.Child = grid;
            }
        }

        public void HideOverlay()
        {
            if (_overlayBorder != null)
            {
                _overlayBorder.Visibility = Visibility.Collapsed;
            }
        }

        public void Dispose()
        {
            // Clean up overlay
            if (_overlayBorder != null)
            {
                if (_overlayBorder.Parent is Panel parentPanel)
                {
                    parentPanel.Children.Remove(_overlayBorder);
                }
                _overlayBorder = null;
            }

            RevealBrush = null;
        }
    }
}
