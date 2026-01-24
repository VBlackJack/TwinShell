# Changelog

All notable changes to TwinShell will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
