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
using Microsoft.Win32;

namespace TwinShell.App.Helpers;

/// <summary>
/// Helper class for detecting and respecting Windows reduced motion settings.
/// Follows WCAG 2.3.3 (AAA) - Animation from Interactions guidelines.
/// </summary>
public static class MotionHelper
{
    private const string SystemParametersKey = @"Control Panel\Desktop";
    private const string AnimationsValueName = "UserPreferencesMask";
    private const int AnimationsDisabledBit = 0x02;

    /// <summary>
    /// Gets a value indicating whether the user has enabled reduced motion in Windows settings.
    /// </summary>
    public static bool IsReducedMotionEnabled
    {
        get
        {
            try
            {
                // Check Windows accessibility setting for reduced motion
                if (SystemParameters.ClientAreaAnimation == false)
                {
                    return true;
                }

                // Also check the registry for more granular control
                using var key = Registry.CurrentUser.OpenSubKey(SystemParametersKey);
                if (key?.GetValue(AnimationsValueName) is byte[] preferences && preferences.Length > 0)
                {
                    // Bit 1 (0x02) controls animations - if NOT set, animations are disabled
                    return (preferences[0] & AnimationsDisabledBit) == 0;
                }

                return false;
            }
            catch
            {
                // If we can't determine the setting, assume animations are enabled
                return false;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether animations should be played.
    /// Returns false if reduced motion is enabled.
    /// </summary>
    public static bool ShouldAnimate => !IsReducedMotionEnabled;

    /// <summary>
    /// Gets the appropriate animation duration based on reduced motion settings.
    /// Returns TimeSpan.Zero if reduced motion is enabled, otherwise returns the provided duration.
    /// </summary>
    /// <param name="normalDuration">The normal animation duration to use when animations are enabled.</param>
    /// <returns>The duration to use for animations.</returns>
    public static TimeSpan GetAnimationDuration(TimeSpan normalDuration)
    {
        return IsReducedMotionEnabled ? TimeSpan.Zero : normalDuration;
    }

    /// <summary>
    /// Gets the appropriate animation duration in milliseconds based on reduced motion settings.
    /// Returns 0 if reduced motion is enabled, otherwise returns the provided duration.
    /// </summary>
    /// <param name="normalDurationMs">The normal animation duration in milliseconds.</param>
    /// <returns>The duration in milliseconds to use for animations.</returns>
    public static int GetAnimationDurationMs(int normalDurationMs)
    {
        return IsReducedMotionEnabled ? 0 : normalDurationMs;
    }

    /// <summary>
    /// Event that fires when the system animation settings change.
    /// </summary>
    public static event EventHandler? AnimationSettingsChanged;

    static MotionHelper()
    {
        // Subscribe to system events for animation settings changes
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.Accessibility)
        {
            AnimationSettingsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
