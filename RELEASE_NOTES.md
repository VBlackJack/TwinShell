# TwinShell v1.2.0 - Release Notes

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

TwinShell is a comprehensive PowerShell and Bash command library for system administrators, featuring over 490 ready-to-use commands.

### Key Features

- **497 commands** organized in categorized collections
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

### v1.2.0 (2025-11-26)
- Editable multiline TextBox for complex scripts
- Dynamic command generator adapts to selected examples
- Improved script detection (ForEach-Object, ranges, pipelines)
- check_examples.py analysis tool

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
