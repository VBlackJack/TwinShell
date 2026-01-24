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
using System.Windows.Media;
using System.Windows.Media.Animation;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Services;

/// <summary>
/// WPF implementation of connected animations between views.
/// Creates smooth transitions where elements appear to move from source to destination.
/// </summary>
public class ConnectedAnimationService : IConnectedAnimationService
{
    private readonly Dictionary<string, AnimationCapture> _captures = new();
    private TimeSpan _duration = TimeSpan.FromMilliseconds(400);
    private bool _isEnabled = true;

    /// <inheritdoc/>
    public TimeSpan Duration
    {
        get => _duration;
        set => _duration = value;
    }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get => _isEnabled && !IsReducedMotionEnabled();
        set => _isEnabled = value;
    }

    /// <inheritdoc/>
    public void PrepareToAnimate(string key, object sourceElement)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        if (sourceElement is not FrameworkElement element)
            return;

        // Remove any existing capture with this key
        _captures.Remove(key);

        // Capture the element's position and bounds
        var capture = CaptureElement(element);
        if (capture != null)
        {
            _captures[key] = capture;
        }
    }

    /// <inheritdoc/>
    public bool TryStartAnimation(string key, object destinationElement)
    {
        if (!IsEnabled)
            return false;

        if (string.IsNullOrEmpty(key))
            return false;

        if (destinationElement is not FrameworkElement destElement)
            return false;

        if (!_captures.TryGetValue(key, out var capture))
            return false;

        _captures.Remove(key);

        // Apply animation
        return AnimateToDestination(capture, destElement);
    }

    /// <inheritdoc/>
    public void Cancel(string key)
    {
        _captures.Remove(key);
    }

    private AnimationCapture? CaptureElement(FrameworkElement element)
    {
        try
        {
            // Get the element's position relative to the window
            var window = Window.GetWindow(element);
            if (window == null)
                return null;

            var position = element.TransformToAncestor(window).Transform(new Point(0, 0));

            return new AnimationCapture
            {
                Bounds = new Rect(position, new Size(element.ActualWidth, element.ActualHeight)),
                CapturedAt = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    private bool AnimateToDestination(AnimationCapture capture, FrameworkElement destination)
    {
        try
        {
            // Get destination position
            var window = Window.GetWindow(destination);
            if (window == null)
                return false;

            var destPosition = destination.TransformToAncestor(window).Transform(new Point(0, 0));
            var destBounds = new Rect(destPosition, new Size(destination.ActualWidth, destination.ActualHeight));

            // Ensure transform group exists
            if (destination.RenderTransform is not TransformGroup transformGroup)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(new TranslateTransform());
                transformGroup.Children.Add(new ScaleTransform());
                destination.RenderTransform = transformGroup;
            }

            var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
            var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();

            if (translateTransform == null || scaleTransform == null)
                return FallbackFadeAnimation(destination);

            // Calculate initial offset from source to destination
            double initialX = capture.Bounds.X - destBounds.X;
            double initialY = capture.Bounds.Y - destBounds.Y;
            double initialScaleX = capture.Bounds.Width / destBounds.Width;
            double initialScaleY = capture.Bounds.Height / destBounds.Height;

            // Set initial transform values
            translateTransform.X = initialX;
            translateTransform.Y = initialY;
            scaleTransform.ScaleX = initialScaleX;
            scaleTransform.ScaleY = initialScaleY;

            // Create storyboard
            var storyboard = new Storyboard();
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Animate X translation
            var animateX = new DoubleAnimation
            {
                From = initialX,
                To = 0,
                Duration = Duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(animateX, destination);
            Storyboard.SetTargetProperty(animateX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));
            storyboard.Children.Add(animateX);

            // Animate Y translation
            var animateY = new DoubleAnimation
            {
                From = initialY,
                To = 0,
                Duration = Duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(animateY, destination);
            Storyboard.SetTargetProperty(animateY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
            storyboard.Children.Add(animateY);

            // Animate scale X
            var animateScaleX = new DoubleAnimation
            {
                From = initialScaleX,
                To = 1,
                Duration = Duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(animateScaleX, destination);
            Storyboard.SetTargetProperty(animateScaleX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(animateScaleX);

            // Animate scale Y
            var animateScaleY = new DoubleAnimation
            {
                From = initialScaleY,
                To = 1,
                Duration = Duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(animateScaleY, destination);
            Storyboard.SetTargetProperty(animateScaleY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(animateScaleY);

            // Fade in
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(Duration.TotalMilliseconds * 0.5)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(fadeIn, destination);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeIn);

            storyboard.Begin();
            return true;
        }
        catch
        {
            return FallbackFadeAnimation(destination);
        }
    }

    private bool FallbackFadeAnimation(FrameworkElement destination)
    {
        try
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            destination.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            return true;
        }
        catch
        {
            return false;
        }
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

    private sealed class AnimationCapture
    {
        public Rect Bounds { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
