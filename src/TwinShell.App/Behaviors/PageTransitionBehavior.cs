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

namespace TwinShell.App.Behaviors;

/// <summary>
/// Attached behavior that applies smooth page transition animations to TabControl
/// or other container controls. Implements Windows 11 Fluent Design page enter/exit animations.
/// </summary>
public static class PageTransitionBehavior
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TabControl, TransitionState> _states = new();

    #region IsEnabled Attached Property

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PageTransitionBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    #endregion

    #region EnterDuration Attached Property

    public static readonly DependencyProperty EnterDurationProperty =
        DependencyProperty.RegisterAttached(
            "EnterDuration",
            typeof(Duration),
            typeof(PageTransitionBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(300))));

    public static Duration GetEnterDuration(DependencyObject obj) => (Duration)obj.GetValue(EnterDurationProperty);
    public static void SetEnterDuration(DependencyObject obj, Duration value) => obj.SetValue(EnterDurationProperty, value);

    #endregion

    #region ExitDuration Attached Property

    public static readonly DependencyProperty ExitDurationProperty =
        DependencyProperty.RegisterAttached(
            "ExitDuration",
            typeof(Duration),
            typeof(PageTransitionBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200))));

    public static Duration GetExitDuration(DependencyObject obj) => (Duration)obj.GetValue(ExitDurationProperty);
    public static void SetExitDuration(DependencyObject obj, Duration value) => obj.SetValue(ExitDurationProperty, value);

    #endregion

    #region SlideDistance Attached Property

    public static readonly DependencyProperty SlideDistanceProperty =
        DependencyProperty.RegisterAttached(
            "SlideDistance",
            typeof(double),
            typeof(PageTransitionBehavior),
            new PropertyMetadata(30.0));

    public static double GetSlideDistance(DependencyObject obj) => (double)obj.GetValue(SlideDistanceProperty);
    public static void SetSlideDistance(DependencyObject obj, double value) => obj.SetValue(SlideDistanceProperty, value);

    #endregion

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TabControl tabControl)
            return;

        bool isEnabled = (bool)e.NewValue;

        if (isEnabled)
        {
            AttachBehavior(tabControl);
        }
        else
        {
            DetachBehavior(tabControl);
        }
    }

    private static void AttachBehavior(TabControl tabControl)
    {
        var state = new TransitionState();
        _states.AddOrUpdate(tabControl, state);

        tabControl.SelectionChanged += OnSelectionChanged;
    }

    private static void DetachBehavior(TabControl tabControl)
    {
        tabControl.SelectionChanged -= OnSelectionChanged;

        if (_states.TryGetValue(tabControl, out _))
        {
            _states.Remove(tabControl);
        }
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        // Only handle TabControl's own selection changes
        if (e.OriginalSource != tabControl)
            return;

        // Check if reduced motion is enabled
        if (IsReducedMotionEnabled())
            return;

        // Get the content presenter
        var contentPresenter = FindContentPresenter(tabControl);
        if (contentPresenter == null)
            return;

        // Apply page enter animation
        ApplyEnterAnimation(tabControl, contentPresenter);
    }

    private static ContentPresenter? FindContentPresenter(TabControl tabControl)
    {
        // Find the content presenter in the TabControl's visual tree
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(tabControl); i++)
        {
            var child = VisualTreeHelper.GetChild(tabControl, i);
            var presenter = FindContentPresenterInTree(child);
            if (presenter != null)
                return presenter;
        }
        return null;
    }

    private static ContentPresenter? FindContentPresenterInTree(DependencyObject element)
    {
        if (element is ContentPresenter presenter && presenter.Name == "PART_SelectedContentHost")
            return presenter;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            var result = FindContentPresenterInTree(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void ApplyEnterAnimation(TabControl tabControl, ContentPresenter contentPresenter)
    {
        var enterDuration = GetEnterDuration(tabControl);
        var slideDistance = GetSlideDistance(tabControl);

        // Ensure transform exists
        if (contentPresenter.RenderTransform is not TranslateTransform)
        {
            contentPresenter.RenderTransform = new TranslateTransform();
        }

        var storyboard = new Storyboard();

        // Fade in animation
        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = enterDuration,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeAnimation, contentPresenter);
        Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeAnimation);

        // Slide up animation
        var slideAnimation = new DoubleAnimation
        {
            From = slideDistance,
            To = 0,
            Duration = enterDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideAnimation, contentPresenter);
        Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        storyboard.Children.Add(slideAnimation);

        storyboard.Begin();
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

    private sealed class TransitionState
    {
        // Placeholder for future state management if needed
    }
}
