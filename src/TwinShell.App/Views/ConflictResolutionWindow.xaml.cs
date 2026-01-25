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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using TwinShell.Core.Interfaces;

namespace TwinShell.App.Views;

/// <summary>
/// Dialog for resolving sync conflicts
/// </summary>
public partial class ConflictResolutionWindow : Window
{
    private readonly List<ConflictResolutionItem> _dataConflicts;
    private readonly List<string> _mergeConflicts;

    /// <summary>
    /// Gets the resolved conflicts after user applies resolutions
    /// </summary>
    public List<SyncConflict> ResolvedConflicts { get; private set; } = new();

    /// <summary>
    /// Gets whether the user applied resolutions (true) or cancelled (false)
    /// </summary>
    public bool WasApplied { get; private set; }

    /// <summary>
    /// Creates a new conflict resolution dialog
    /// </summary>
    /// <param name="dataConflicts">List of data-level conflicts (entity conflicts)</param>
    /// <param name="mergeConflicts">List of Git merge conflict file paths</param>
    public ConflictResolutionWindow(IEnumerable<SyncConflict>? dataConflicts = null, IEnumerable<string>? mergeConflicts = null)
    {
        InitializeComponent();

        _dataConflicts = (dataConflicts ?? Enumerable.Empty<SyncConflict>())
            .Select(c => new ConflictResolutionItem(c))
            .ToList();

        _mergeConflicts = (mergeConflicts ?? Enumerable.Empty<string>()).ToList();

        SetupUI();
        Loaded += (s, e) => ApplyAcrylicBackdrop();
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

    private void SetupUI()
    {
        // Setup data conflicts section
        if (_dataConflicts.Count > 0)
        {
            DataConflictsHeader.Visibility = Visibility.Visible;
            DataConflictsList.ItemsSource = _dataConflicts;
        }

        // Setup merge conflicts section
        if (_mergeConflicts.Count > 0)
        {
            MergeConflictsHeader.Visibility = Visibility.Visible;
            MergeConflictsPanel.Visibility = Visibility.Visible;
            MergeConflictsList.ItemsSource = _mergeConflicts;

            // If only merge conflicts exist, disable Apply button (user must resolve manually)
            if (_dataConflicts.Count == 0)
            {
                ApplyButton.Content = "Close";
            }
        }

        // If no conflicts at all, show a message
        if (_dataConflicts.Count == 0 && _mergeConflicts.Count == 0)
        {
            DataConflictsHeader.Visibility = Visibility.Visible;
            DataConflictsHeader.Text = "No conflicts to resolve";
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Collect resolved conflicts
        ResolvedConflicts = _dataConflicts
            .Select(item => new SyncConflict
            {
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                EntityName = item.EntityName,
                LocalModifiedAt = item.LocalModifiedAt,
                RemoteModifiedAt = item.RemoteModifiedAt,
                Resolution = item.SelectedResolution
            })
            .ToList();

        WasApplied = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        WasApplied = false;
        DialogResult = false;
        Close();
    }

    // Custom Title Bar Button Handlers
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

/// <summary>
/// View model item for conflict resolution in the UI
/// </summary>
public class ConflictResolutionItem : INotifyPropertyChanged
{
    private bool _isKeepLocalSelected = true;
    private bool _isUseRemoteSelected;
    private bool _isSkipSelected;

    public string EntityType { get; }
    public Guid EntityId { get; }
    public string EntityName { get; }
    public DateTime LocalModifiedAt { get; }
    public DateTime RemoteModifiedAt { get; }

    public bool IsKeepLocalSelected
    {
        get => _isKeepLocalSelected;
        set
        {
            if (_isKeepLocalSelected != value)
            {
                _isKeepLocalSelected = value;
                OnPropertyChanged(nameof(IsKeepLocalSelected));
            }
        }
    }

    public bool IsUseRemoteSelected
    {
        get => _isUseRemoteSelected;
        set
        {
            if (_isUseRemoteSelected != value)
            {
                _isUseRemoteSelected = value;
                OnPropertyChanged(nameof(IsUseRemoteSelected));
            }
        }
    }

    public bool IsSkipSelected
    {
        get => _isSkipSelected;
        set
        {
            if (_isSkipSelected != value)
            {
                _isSkipSelected = value;
                OnPropertyChanged(nameof(IsSkipSelected));
            }
        }
    }

    public ConflictResolution SelectedResolution
    {
        get
        {
            if (IsUseRemoteSelected) return ConflictResolution.UseRemote;
            if (IsSkipSelected) return ConflictResolution.Skip;
            return ConflictResolution.KeepLocal;
        }
    }

    public ConflictResolutionItem(SyncConflict conflict)
    {
        EntityType = conflict.EntityType;
        EntityId = conflict.EntityId;
        EntityName = conflict.EntityName;
        LocalModifiedAt = conflict.LocalModifiedAt;
        RemoteModifiedAt = conflict.RemoteModifiedAt;

        // Set initial selection based on existing resolution
        switch (conflict.Resolution)
        {
            case ConflictResolution.UseRemote:
                _isUseRemoteSelected = true;
                _isKeepLocalSelected = false;
                break;
            case ConflictResolution.Skip:
                _isSkipSelected = true;
                _isKeepLocalSelected = false;
                break;
            default:
                _isKeepLocalSelected = true;
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
