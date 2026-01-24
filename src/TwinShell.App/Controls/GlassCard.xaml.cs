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

namespace TwinShell.App.Controls;

/// <summary>
/// Glass card control with Windows 11 Fluent Design glass-morphism effect.
/// Supports configurable intensity levels, reveal highlight, and hover animations.
/// </summary>
public partial class GlassCard : UserControl
{
    #region GlassIntensity Enum

    /// <summary>
    /// Glass transparency intensity levels.
    /// </summary>
    public enum GlassIntensity
    {
        /// <summary>
        /// 50% opacity - Most transparent.
        /// </summary>
        Subtle,

        /// <summary>
        /// 65% opacity - Light transparency.
        /// </summary>
        Light,

        /// <summary>
        /// 80% opacity - Default medium transparency.
        /// </summary>
        Medium,

        /// <summary>
        /// 90% opacity - Near-solid with slight transparency.
        /// </summary>
        Strong
    }

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty IntensityProperty =
        DependencyProperty.Register(
            nameof(Intensity),
            typeof(GlassIntensity),
            typeof(GlassCard),
            new PropertyMetadata(GlassIntensity.Medium, OnIntensityChanged));

    public static readonly DependencyProperty EnableRevealProperty =
        DependencyProperty.Register(
            nameof(EnableReveal),
            typeof(bool),
            typeof(GlassCard),
            new PropertyMetadata(true));

    public static readonly DependencyProperty EnableRevealBorderProperty =
        DependencyProperty.Register(
            nameof(EnableRevealBorder),
            typeof(bool),
            typeof(GlassCard),
            new PropertyMetadata(true));

    public static readonly DependencyProperty EnableHoverLiftProperty =
        DependencyProperty.Register(
            nameof(EnableHoverLift),
            typeof(bool),
            typeof(GlassCard),
            new PropertyMetadata(true));

    public static readonly DependencyProperty RevealIntensityProperty =
        DependencyProperty.Register(
            nameof(RevealIntensity),
            typeof(double),
            typeof(GlassCard),
            new PropertyMetadata(0.10));

    public static readonly DependencyProperty BorderRevealIntensityProperty =
        DependencyProperty.Register(
            nameof(BorderRevealIntensity),
            typeof(double),
            typeof(GlassCard),
            new PropertyMetadata(0.40));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the glass transparency intensity.
    /// </summary>
    public GlassIntensity Intensity
    {
        get => (GlassIntensity)GetValue(IntensityProperty);
        set => SetValue(IntensityProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the reveal highlight effect is enabled.
    /// </summary>
    public bool EnableReveal
    {
        get => (bool)GetValue(EnableRevealProperty);
        set => SetValue(EnableRevealProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the reveal border effect is enabled.
    /// </summary>
    public bool EnableRevealBorder
    {
        get => (bool)GetValue(EnableRevealBorderProperty);
        set => SetValue(EnableRevealBorderProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the hover lift animation is enabled.
    /// </summary>
    public bool EnableHoverLift
    {
        get => (bool)GetValue(EnableHoverLiftProperty);
        set => SetValue(EnableHoverLiftProperty, value);
    }

    /// <summary>
    /// Gets or sets the reveal highlight intensity (0.0 - 1.0).
    /// </summary>
    public double RevealIntensity
    {
        get => (double)GetValue(RevealIntensityProperty);
        set => SetValue(RevealIntensityProperty, value);
    }

    /// <summary>
    /// Gets or sets the reveal border intensity (0.0 - 1.0).
    /// </summary>
    public double BorderRevealIntensity
    {
        get => (double)GetValue(BorderRevealIntensityProperty);
        set => SetValue(BorderRevealIntensityProperty, value);
    }

    #endregion

    public GlassCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyIntensity(Intensity);
    }

    private static void OnIntensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassCard card)
        {
            card.ApplyIntensity((GlassIntensity)e.NewValue);
        }
    }

    private void ApplyIntensity(GlassIntensity intensity)
    {
        string brushKey = intensity switch
        {
            GlassIntensity.Subtle => "Glass.Surface.Subtle.Brush",
            GlassIntensity.Light => "Glass.Surface.Brush", // Light uses medium as baseline
            GlassIntensity.Medium => "Glass.Surface.Brush",
            GlassIntensity.Strong => "Glass.Surface.Strong.Brush",
            _ => "Glass.Surface.Brush"
        };

        if (TryFindResource(brushKey) is Brush background)
        {
            CardBorder.Background = background;
        }

        if (TryFindResource("Glass.Border.Brush") is Brush border)
        {
            CardBorder.BorderBrush = border;
        }

        // Apply shadow based on intensity
        string shadowKey = intensity switch
        {
            GlassIntensity.Subtle => "Glass.Shadow",
            GlassIntensity.Light => "Glass.Shadow",
            GlassIntensity.Medium => "Glass.Shadow",
            GlassIntensity.Strong => "Glass.Shadow.Elevated",
            _ => "Glass.Shadow"
        };

        if (TryFindResource(shadowKey) is System.Windows.Media.Effects.Effect shadow)
        {
            CardBorder.Effect = shadow;
        }
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!EnableHoverLift || IsReducedMotionEnabled())
            return;

        // Apply elevated shadow
        if (TryFindResource("Glass.Shadow.Elevated") is System.Windows.Media.Effects.Effect elevatedShadow)
        {
            CardBorder.Effect = elevatedShadow;
        }

        // Animate lift
        if (TryFindResource("LiftAnimationStoryboard") is Storyboard liftStoryboard)
        {
            liftStoryboard.Begin(CardBorder, true);
        }
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!EnableHoverLift || IsReducedMotionEnabled())
            return;

        // Restore normal shadow
        ApplyIntensity(Intensity);

        // Animate lower
        if (TryFindResource("LowerAnimationStoryboard") is Storyboard lowerStoryboard)
        {
            lowerStoryboard.Begin(CardBorder, true);
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
}
