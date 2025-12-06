# TwinShell v1.5.0 - Release Notes

## What's New in v1.5.0

### Actions Architecture Refactoring
- **513 commands** now stored as individual JSON files in `data/seed/actions/`
- Each action is a separate file for easier maintenance and collaboration
- Better Git history and merge conflict handling
- Cleaner structure for community contributions

### Examples Organization
- **2,732 examples** properly organized by platform
- Cross-platform actions now use `windowsExamples` and `linuxExamples` arrays
- Fixed 79 actions with mixed platform examples
- Fixed 36 actions with missing command template IDs

### Quality Improvements
- All 513 JSON files validated
- IDs normalized to lowercase (89 files fixed)
- Removed duplicate examples (928 duplicates cleaned)
- Dynamic version display in About window

---

## What's New in v1.4.0

### Commands Database Audit & Cleanup
- **479 commands** (optimized from 488 - removed duplicates and merged similar actions)
- **13 categories** (streamlined from 15 - removed redundant categories)
- **100% quality score** - all commands have examples, templates, and descriptions
- **Fixed platform consistency** - examples now correctly match their action's platform
- **Renamed categories** for clarity:
  - "Windows Optimization" → "Windows Debloat"
  - Removed "Linux Administration" (redistributed to thematic categories)
  - Removed "User Management" (merged into Security & Encryption)

### Category Improvements
- Commands are now organized by function, not by OS
- Linux commands for permissions/ACL moved to "Security & Encryption"
- Disk/partition commands moved to "Files & Storage"
- Process management moved to "Performance"

---

## What's New in v1.3.0

### Git Synchronization
- Full GitOps support for team collaboration
- HTTPS authentication with tokens
- Auto-sync on startup option

---

## What's New in v1.2.0

### Dynamic Command Generator Improvements
- **Editable complex scripts**: Commands with `foreach`, `ForEach-Object`, `while`, ranges (`1..100`), multi-statement scripts, etc. are now displayed in an editable multiline TextBox instead of being parsed into individual parameters
- **Improved script detection**: Better handling of PowerShell pipelines, script blocks, and complex command patterns
- **Dynamic parameter fields**: The command generator now adapts to the selected example, showing appropriate fields

### Technical Improvements
- Enhanced `IsComplexScript()` detection for more script patterns
- Observable properties in `ParameterViewModel` for proper WPF binding
- Added `check_examples.py` script for analyzing example parsing issues

---

## What's New in v1.1.0

### Cross-Platform Enhancements
- **15 unified cross-platform actions** with automatic Windows/Linux command selection
- **Docker and Ansible commands** now universally available on both platforms
- **User management unification**: create/delete local users on Windows & Linux
- **Network diagnostics**: unified port testing (Test-NetConnection / nc)
- **File hash calculation**: unified SHA256 hash (Get-FileHash / sha256sum)

### New Unified Actions
| Action | Windows Command | Linux Command |
|--------|-----------------|---------------|
| unified-file-copy | Copy-Item | cp |
| unified-file-move | Move-Item | mv |
| unified-file-delete | Remove-Item | rm |
| unified-file-create-directory | New-Item | mkdir |
| unified-file-permissions | icacls | chmod |
| unified-file-disk-usage | Get-PSDrive | df |
| unified-find-files | Get-ChildItem | find |
| unified-search-content | Select-String | grep |
| unified-tree | tree | tree |
| unified-service-status | Get-Service | systemctl |
| unified-service-restart | Restart-Service | systemctl |
| unified-network-test-port | Test-NetConnection | nc |
| unified-create-local-user | New-LocalUser | useradd |
| unified-delete-local-user | Remove-LocalUser | userdel |
| unified-file-hash | Get-FileHash | sha256sum |

### Improvements
- **497 total commands** (up from 507 - optimized with unified actions)
- **74 cross-platform commands** available on both Windows and Linux
- **Better shell escaping** for cross-platform command safety
- **Performance optimizations** in CommandGeneratorService

---

## Core Features

TwinShell is a comprehensive PowerShell and Bash command library for system administrators, featuring 513 ready-to-use commands.

### Key Features

- **513 commands** organized in 13 categorized collections
- **Search and filtering** capabilities for quick command discovery
- **Command history** with export functionality
- **Favorites system** to bookmark frequently used commands
- **Parameter templates** for easy command customization
- **Category management** for organizing custom commands

### Command Categories

#### Security & Compliance
- 🔒 **BitLocker & Encryption** (21 commands)
  - Local management: status, enable/disable, recovery keys, TPM configuration
  - Domain administration: AD key retrieval, domain-wide audits, compliance reports

- 🛡️ **Windows Defender** (17 commands)
  - Local management: scans, exclusions, real-time protection, threat management
  - Domain administration: protection status audits via WinRM

- 🔑 **LAPS** (8 commands)
  - Password retrieval and management
  - Expiration control
  - Compliance auditing

#### System Administration
- 📁 Active Directory management
- 🖥️ Computer and user management
- 📊 System diagnostics and monitoring
- 🌐 Network configuration
- 🔧 Service and process management
- 💾 Storage and disk management
- 📝 Event log analysis
- 🗂️ Registry operations
- And much more!

## Installation

### Portable Version (Recommended for Quick Start)
1. Download `TwinShell-v1.2.0-Portable-win-x64.zip`
2. Extract to your desired location
3. Run `TwinShell.App.exe`
4. No installation required!

### Installer Version
1. Download `TwinShell-v1.2.0-Setup-win-x64.exe` (if available)
2. Run the installer
3. Follow the installation wizard
4. Launch from Start Menu or Desktop shortcut

## System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Framework**: .NET 8.0 Runtime (included in self-contained build)
- **Memory**: 256 MB RAM minimum
- **Disk Space**: ~150 MB

## Data Storage

TwinShell stores user data (favorites, history, custom commands) in:
```
%LOCALAPPDATA%\TwinShell\
```

This directory includes:
- `twinshell.db` - SQLite database containing your data
- Configuration files
- Application logs

## Uninstallation

### Portable Version
Simply delete the extracted folder. To remove user data, delete:
```
%LOCALAPPDATA%\TwinShell\
```

### Installer Version
Use Windows "Add or Remove Programs" or the uninstaller shortcut in the Start Menu.

## Known Issues

None at this time.

## Feedback and Support

- **Issues**: Report bugs at https://github.com/VBlackJack/TwinShell/issues
- **Discussions**: Join the conversation at https://github.com/VBlackJack/TwinShell/discussions
- **Documentation**: Visit https://github.com/VBlackJack/TwinShell/wiki

## License

See LICENSE file for details.

## Credits

Developed with:
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Entity Framework Core
- CommunityToolkit.Mvvm

---

## Version History

### v1.5.0 (2025-12-06)
- Actions architecture refactored (513 individual JSON files)
- 2,732 examples properly organized by platform
- Fixed 79 cross-platform actions with mixed examples
- Fixed 36 actions with missing template IDs
- 928 duplicate examples removed
- Dynamic version in About window

### v1.4.0 (2025-12-05)
- Commands database audit and cleanup (479 commands, 13 categories)
- 100% quality score - all commands validated
- Merged duplicates and fixed platform inconsistencies
- Reorganized categories by function

### v1.3.0 (2025-12-01)
- Git synchronization (GitOps)
- Team collaboration features
- Security audit fixes

### v1.2.0 (2025-11-26)
- Editable multiline TextBox for complex scripts
- Dynamic command generator adapts to selected examples
- Improved script detection (ForEach-Object, ranges, pipelines)

### v1.1.0 (2025-11-26)
- 15 unified cross-platform actions
- Docker/Ansible universalization
- User management and file hash actions
- Shell escaping improvements

### v1.0.0 (2025-01-18)
- Initial release with 507 commands
- Category-based organization
- Favorites and history system
- Parameter templates

---

Thank you for using TwinShell!
