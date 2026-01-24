# Changelog

All notable changes to TwinShell will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.4.0] - 2026-01-24

### Changed
- **MainWindow Glass UI Application**: Applied Glass UI styles to main interface
  - Search/Filters card now uses GlassCardStyle with semi-transparent background
  - Categories panel uses GlassPanelStyle for glass-morphism sidebar effect
  - Actions list panel uses GlassPanelStyle for visual consistency
  - Action Details panel uses GlassPanelStyle for unified glass appearance
  - Search TextBox uses GlassTextBoxStyle with reveal border on focus
  - Buttons (Copy, Clear) use GlassButtonStyle with scale-down press effect
  - TabControl now uses PageTransitionBehavior for smooth tab transitions (300ms fade + slide)

## [2.3.0] - 2026-01-24

### Added
- **Glass UI System**: Windows 11 Fluent Design glass-morphism effects
  - Glass design tokens: Glass.Opacity.*, Glass.Blur.*, Reveal.Radius, Reveal.Intensity.*
  - Glass color palette for Dark and Light themes with semi-transparent surfaces
  - GlassCardStyle with hover lift animation and shadow effects
  - GlassCardStyle.Subtle and GlassCardStyle.Strong intensity variants
  - GlassPanelStyle for sidebars and navigation areas
  - GlassButtonStyle with scale-down press effect (0.98x)
  - GlassTextBoxStyle with reveal border on focus
- **Reveal Effects**: Interactive light-following effects
  - RevealHighlightBehavior: RadialGradientBrush follows mouse cursor within element bounds
  - RevealBorderBehavior: Border gradient glows nearest to cursor position
  - Configurable radius (default: 120px) and intensity via attached properties
  - 16ms debounce for 60fps performance
- **Page Transitions**: Smooth view transitions
  - PageTransitionBehavior for TabControl with configurable enter/exit animations
  - Default: 300ms enter (fade + slide up), 200ms exit
- **Connected Animations**: Element transitions between views
  - IConnectedAnimationService for capturing and animating elements
  - PrepareToAnimate/TryStartAnimation pattern
  - 400ms cubic easing with fallback to fade
- **GlassCard UserControl**: Reusable glass container component
  - Intensity property (Subtle/Light/Medium/Strong)
  - EnableReveal, EnableRevealBorder, EnableHoverLift properties
  - Configurable RevealIntensity and BorderRevealIntensity
- **Glass Fallback Chain**: Graceful degradation for older systems
  - GlassFallbackLevel enum: Full, Partial, Minimal, None
  - Windows 11 22H2+: Full effects
  - Windows 11 21H2: Partial (Mica only)
  - Windows 10: Solid colors with gradients
  - High Contrast: Pure solid colors

### Changed
- IBackdropEffectService extended with FallbackLevel property and GetFallbackLevel() method
- DesignTokens.xaml now includes complete Glass UI and Reveal effect tokens
- Glass Shadow effects (Glass.Shadow, Glass.Shadow.Elevated, Glass.Shadow.Floating)

### Accessibility
- All Reveal effects respect ReducedMotion system setting
- All animations disabled when SystemParameters.ClientAreaAnimation is false
- Focus indicators maintained on all Glass components (WCAG 2.4.7)

## [2.2.0] - 2026-01-24

### Added
- Design tokens FontSize.2XS (11px) and FontSize.2XL (18px) for complete typography scale
- Fluent Design animations: PageFadeTransitionStoryboard, LiftAnimationStoryboard, LowerAnimationStoryboard, DialogEntryStoryboard
- AnimatedFluentCardStyle with hover lift effect (-4px Y translation) and reveal border highlight
- Acrylic backdrop effect for dialog windows (SettingsWindow, AboutWindow, ActionEditorWindow, CategoryManagementWindow)
- Theme-aware Acrylic effect with automatic dark/light detection

### Changed
- Replaced 200+ hardcoded values with design tokens across XAML files:
  - MainWindow.xaml: CornerRadius, Padding, Margin, FontSize values now use tokens
  - SettingsWindow.xaml: Tokenized styles and spacing
  - AboutWindow.xaml: Tokenized typography and spacing
- ComboBox MinHeight increased from 36px to 44px for WCAG 2.5.5 touch target compliance
- PasswordBox MinHeight increased to 44px for consistent touch targets
- FluentCardStyle now includes AnimatedFluentCardStyle variant for interactive cards

### Accessibility (WCAG 2.5.5)
- All interactive elements now meet 44px minimum touch target requirement
- ComboBox, PasswordBox, and Button styles updated to use TouchTarget.Minimum token

## [2.1.0] - 2026-01-24

### Added
- Windows 11 Mica backdrop effect for main window (glass-like transparency)
- IBackdropEffectService for managing Mica/Acrylic effects
- DwmApi interop for Windows Desktop Window Manager
- Fallback to solid colors on Windows 10 or when transparency is disabled
- Theme-aware favorite button colors (FavoritedStarBrush, UnfavoritedStarBrush)
- LoadingOverlayBrush and MicaBackgroundBrush theme resources
- Complete ComboBox styling for dark/light themes (dropdown popup, items)
- Complete CheckBox styling for dark/light themes
- Complete PasswordBox styling for dark/light themes
- Automatic app restart prompt when theme changes (ensures all UI updates correctly)

### Changed
- Improved Dark Theme contrast ratios for WCAG AA compliance
  - TextTertiaryBrush: #8E8E8E -> #999999 (4.5:1 ratio)
  - TextDisabledBrush: #707070 -> #757575 (3.5:1 ratio)
  - ScrollBar Thumb: #555555 -> #686868 (3.0:1 ratio)
- Extracted all hardcoded colors from MainWindow.xaml to theme resources (Zero Hardcoding compliance)

### Fixed
- Favorite button colors now respect theme (was hardcoded #FFD700, #9E9E9E)
- Loading overlay now uses theme resource instead of hardcoded #80000000
- ComboBox dropdown in Settings now uses dark theme colors (was unreadable)
- CheckBox text in filter area now uses proper theme colors
- CriticalityToColorConverter now uses theme-aware colors (InfoBrush, SuccessBrush, DangerBrush)
- ErrorToColorConverter now uses theme-aware colors

## [2.0.0] - 2026-01-24

### Added
- High Contrast theme for accessibility (WCAG AAA+ compliant, 10:1+ contrast ratios)
- Reduced Motion accessibility setting (WCAG 2.3.3 compliant)
- SetSystemTheme and SetHighContrastTheme commands in MainViewModel
- ToggleReducedMotion command for accessibility preference
- New localization keys for accessibility messages (EN/FR)

### Changed
- Theme enum now includes HighContrast option
- ThemeService respects ReducedMotion setting (skips animations when enabled)
- UserSettings includes ReducedMotion property

## [1.8.0] - 2026-01-24

### Added
- UI-006: SearchViewModel extracted for better separation of concerns (SRP)
- UI-007: FluentCardStyle and FluentPanelStyle with layered elevation effects
- UI-008: Smooth 150ms fade transition when switching themes

### Changed
- Search/filter panel now uses FluentCardStyle with enhanced hover effects
- OutputPanel header uses FluentPanelStyle for visual consistency
- Theme transitions are now animated for better UX

## [1.7.0] - 2026-01-24

### Added
- UI-004: Theme-aware console output colors (ConsoleOutputTextBrush, ConsoleOutputErrorBrush)
- UI-005: Debouncing for search/filter operations to prevent race conditions

### Fixed
- UI-002: Added timeout to semaphore operations in MainViewModel and HistoryViewModel to prevent deadlocks
- UI-003: Added null checks for Application.Current.Dispatcher in ExecutionViewModel and BatchViewModel
- Extracted hardcoded colors from OutputPanel.xaml to theme resources

### Changed
- Improved filter performance with 150ms debounce on text input
- Enhanced thread safety in async operations

## [1.6.1] - 2026-01-24

### Fixed
- TD-001: Fixed SyncHistoryRepository memory leak - use ExecuteDeleteAsync instead of loading entities
- TD-003: Localized hardcoded MessageBox confirmations in HistoryViewModel
- TD-004: Localized keyboard shortcuts help dialog in MainWindow
- TD-014: Localized clipboard notification message in MainViewModel
- Fixed SearchHistoryRepository.ClearAllAsync to use ExecuteDeleteAsync pattern

### Added
- New localization keys for history and keyboard shortcuts messages
- French translations for new localization keys

## [1.6.0] - 2026-01-24

### Added
- Automatic backup service with configurable retention (RTO: 1h, RPO: 15min)
- Health check service for startup verification and monitoring
- Circuit breaker pattern for external service calls (Polly)
- Conflict resolution UI dialog for Git sync conflicts
- JSON Schema validation for sync imports
- Git merge conflict detection
- Comprehensive disaster recovery documentation
- Operational runbooks for deployment and maintenance
- Code coverage reporting in CI/CD
- Smoke tests in CI/CD pipeline
- GitHub Release automation on version tags
- Correlation ID service for distributed tracing (OpenTelemetry compatible)
- Performance SLA constants (SearchLatencyTargetMs, UiLoadLatencyTargetMs, etc.)
- appsettings.json for runtime configuration
- BMAD NFR assessment documentation (29/29 criteria pass)

### Changed
- Re-enabled tests in CI/CD pipeline
- Enhanced error handling with localized messages
- Improved Git sync with cancellation support
- Added batch operations for better performance
- Log levels now configurable via appsettings.json (no rebuild required)

### Security
- Reduced file size limit from 10MB to 100KB for imports
- Added path traversal protection in file operations
- Added repository path validation in Git sync

## [1.5.1] - 2025-01-20

### Added
- UX consistency improvements
- Localization enhancements
- Action import/export functionality

### Fixed
- Updated tests to load actions from individual JSON files

## [1.5.0] - 2025-01-15

### Added
- GitOps collaboration features
- Git sync with pull/push operations
- Sync history and audit logging
- Retry logic for network operations

### Changed
- Migrated from monolithic initial-actions.json to individual action files

## [1.4.0] - 2025-01-01

### Added
- Batch command execution
- Command templates with parameters
- Execution history with search

### Changed
- Improved UI responsiveness
- Better error messages

## [1.3.0] - 2024-12-15

### Added
- Dark/Light theme support
- French localization
- Favorites management

## [1.2.0] - 2024-12-01

### Added
- Custom categories
- Tag filtering
- Export/Import configuration

## [1.1.0] - 2024-11-15

### Added
- PowerShell Gallery integration
- Command validation
- Execution timeout settings

## [1.0.0] - 2024-11-01

### Added
- Initial release
- PowerShell and Bash command management
- Direct command execution
- Category organization
- Search functionality

[1.6.0]: https://github.com/jbombled/TwinShell/compare/v1.5.1...v1.6.0
[1.5.1]: https://github.com/jbombled/TwinShell/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/jbombled/TwinShell/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/jbombled/TwinShell/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/jbombled/TwinShell/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/jbombled/TwinShell/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/jbombled/TwinShell/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/jbombled/TwinShell/releases/tag/v1.0.0
