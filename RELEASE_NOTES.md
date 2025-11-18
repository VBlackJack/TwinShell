# TwinShell v1.0.0 - Release Notes

## What's New

TwinShell is a comprehensive PowerShell command library for Windows system administrators, featuring over 500 ready-to-use commands.

### Key Features

- **507 PowerShell commands** organized in categorized collections
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
1. Download `TwinShell-v1.0.0-Portable-win-x64.zip`
2. Extract to your desired location
3. Run `TwinShell.App.exe`
4. No installation required!

### Installer Version
1. Download `TwinShell-v1.0.0-Setup-win-x64.exe` (if available)
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

- **Issues**: Report bugs at https://github.com/yourusername/TwinShell/issues
- **Discussions**: Join the conversation at https://github.com/yourusername/TwinShell/discussions
- **Documentation**: Visit https://github.com/yourusername/TwinShell/wiki

## License

See LICENSE file for details.

## Credits

Developed with:
- .NET 8.0
- WPF (Windows Presentation Foundation)
- Entity Framework Core
- CommunityToolkit.Mvvm

---

**Note**: This is the initial release (v1.0.0). Future updates will bring additional commands, features, and improvements.

Thank you for using TwinShell!
