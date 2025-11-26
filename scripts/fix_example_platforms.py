#!/usr/bin/env python3
"""
Script to fix Platform assignments in examples for cross-platform actions.
Analyzes command content to determine if it's Windows (PowerShell) or Linux (Bash).
"""

import json
import re

# PowerShell patterns (Windows)
WINDOWS_PATTERNS = [
    r'\b(Get|Set|New|Remove|Start|Stop|Restart|Clear|Enable|Disable|Test|Resolve|Import|Export|Add|Update|Invoke|Install|Uninstall)-\w+',  # PowerShell cmdlets
    r'\$\w+',  # PowerShell variables
    r'-\w+\s+\$',  # PowerShell parameters
    r'\bpowershell\b',
    r'\bWrite-Host\b',
    r'\bWhere-Object\b',
    r'\bSelect-Object\b',
    r'\bForEach-Object\b',
    r'\bOut-File\b',
    r'\bConvertTo-Json\b',
    r'\bConvertFrom-Json\b',
    r'\|\s*%\s*{',  # PowerShell shorthand for ForEach
    r'\|\s*\?\s*{',  # PowerShell shorthand for Where
    r'\bwmic\b',
    r'\bnetsh\b',
    r'\bipconfig\b',
    r'\.exe\b',
    r'\bsfc\b',
    r'\bdism\b',
    r'\bchkdsk\b',
    r'\bcmd\b',
    r'\brobocopy\b',
    r'\bxcopy\b',
    r'\bwinget\b',
    r'\bchoco\b',
    r'\bscoop\b',
    r'\bdotnet\b',
    r'\b\[System\.',  # .NET types
    r'\bC:\\',  # Windows paths
    r'%[A-Za-z]+%',  # Windows environment variables
    # Windows-specific network commands
    r'\btracert\b',  # Windows traceroute
    r'\bpathping\b',  # Windows path ping
    r'\bnbtstat\b',  # NetBIOS stats
    r'\bgetmac\b',  # Get MAC address
    r'\barp\b',  # ARP (more common on Windows in examples)
    r'\broute\s+print\b',  # Windows route print
    r'\bnetstat\s+-an\b',  # Windows-style netstat
    r'\bquser\b',  # Query user sessions
    r'\bqwinsta\b',  # Query sessions
    r'\bquery\s+user\b',  # Query user
    r'\btasklist\b',  # List processes
    r'\btaskkill\b',  # Kill processes
    r'\bsc\s+(query|start|stop)\b',  # Service control
    r'\breg\s+(query|add|delete)\b',  # Registry commands
    r'\bwevtutil\b',  # Event log utility
    r'\bfsutil\b',  # File system utility
    r'\bdiskpart\b',  # Disk partitioning
    r'\bbcdedit\b',  # Boot configuration
    r'\bshutdown\s+/[rsthfoa]\b',  # Windows shutdown syntax
    r'\bschtasks\b',  # Scheduled tasks
    r'\battrib\b',  # File attributes
    r'\bicacls\b',  # File permissions
    r'\btakeown\b',  # Take ownership
    r'\bsetx\b',  # Set environment variable
    r'\bwhere\.exe\b',  # Find files
    r'\bfindstr\b',  # Find strings in files
    r'\btype\b',  # Display file contents (Windows cat)
    r'\bcopy\b',  # Copy files
    r'\bdel\b',  # Delete files
    r'\bren\b',  # Rename files
    r'\bmd\b',  # Make directory
    r'\brd\b',  # Remove directory
    r'\bdir\b',  # List directory
    r'\bclip\b',  # Copy to clipboard
    r'\bmore\b',  # Page through output (also Linux but context matters)
    r'\bnet\s+(user|localgroup|share|use|view|start|stop)\b',  # Net commands
    r'\bwhoami\s+/\w+\b',  # Windows whoami with options
    r'\bTest-Connection\b',  # PowerShell ping equivalent
    r'gradlew\.bat\b',  # Windows Gradle wrapper
    r'mvnw\.cmd\b',  # Windows Maven wrapper
]

# Bash/Linux patterns
LINUX_PATTERNS = [
    r'\bsudo\b',
    r'\bapt\b',
    r'\bapt-get\b',
    r'\byum\b',
    r'\bdnf\b',
    r'\bpacman\b',
    r'\bsystemctl\b',
    r'\bjournalctl\b',
    r'\bservice\b',
    r'\bchmod\b',
    r'\bchown\b',
    r'\bgrep\b',
    r'\bsed\b',
    r'\bawk\b',
    r'\bfind\b',
    r'\bcat\b',
    r'\bls\b',
    r'\brm\b',
    r'\bmv\b',
    r'\bcp\b',
    r'\bmkdir\b',
    r'\btouch\b',
    r'\btar\b',
    r'\bgzip\b',
    r'\bgunzip\b',
    r'\bwget\b',
    r'\bcurl\b',
    r'\bdig\b',
    r'\bnslookup\b',
    r'\bping\b',
    r'\bnetstat\b',
    r'\bss\b',
    r'\biptables\b',
    r'\bufw\b',
    r'\bfirewalld\b',
    r'\bnc\b',
    r'\btelnet\b',
    r'\bssh\b',
    r'\bscp\b',
    r'\brsync\b',
    r'\bdocker\b',
    r'\bkubectl\b',
    r'\bansible\b',
    r'\bdf\b',
    r'\bdu\b',
    r'\bfree\b',
    r'\btop\b',
    r'\bhtop\b',
    r'\bps\b',
    r'\bkill\b',
    r'\bkillall\b',
    r'\bhead\b',
    r'\btail\b',
    r'\bless\b',
    r'\becho\b',
    r'\bprintf\b',
    r'\bexport\b',
    r'\bsource\b',
    r'\bbash\b',
    r'\bsh\b',
    r'\bzsh\b',
    r'/etc/',  # Linux paths
    r'/var/',
    r'/usr/',
    r'/home/',
    r'/opt/',
    r'/bin/',
    r'/sbin/',
    r'\$[A-Z_]+',  # Linux environment variables like $HOME
    r'&&',  # Common in bash
    r'\|\|',  # Common in bash
    r'2>&1',  # Bash redirection
    r'>[>&]',  # Bash redirection
    # Linux-specific network commands
    r'\btraceroute\b',  # Linux traceroute (NOT tracert)
    r'\bmtr\b',  # My traceroute
    r'\bip\s+(addr|link|route|neigh)\b',  # ip command
    r'\bifconfig\b',  # Network interface config
    r'\biwconfig\b',  # Wireless config
    r'\bnetplan\b',  # Network configuration
    r'\bnmcli\b',  # NetworkManager CLI
    r'\bhostnamectl\b',  # Hostname control
    r'\btimedatectl\b',  # Time/date control
    r'\blocalectl\b',  # Locale control
    r'\buname\b',  # System info
    r'\blsb_release\b',  # Distribution info
    r'\bwhich\b',  # Find command path
    r'\bwhereis\b',  # Find binary/source/man
    r'\blocate\b',  # Find files by name
    r'\bupdatedb\b',  # Update locate database
    r'\bcrontab\b',  # Cron jobs
    r'\bat\b',  # Schedule commands
    r'\bxargs\b',  # Build command lines
    r'\btee\b',  # Read from stdin and write
    r'\bsort\b',  # Sort lines
    r'\buniq\b',  # Report/filter repeated lines
    r'\bwc\b',  # Word count
    r'\bcut\b',  # Cut sections from lines
    r'\btr\b',  # Translate characters
    r'\brev\b',  # Reverse lines
    r'\bdiff\b',  # Compare files
    r'\bpatch\b',  # Apply patches
    r'\bfile\b',  # Determine file type
    r'\bstat\b',  # File status
    r'\blsof\b',  # List open files
    r'\bfuser\b',  # Find processes using files
    r'\bstrace\b',  # System call tracer
    r'\bltrace\b',  # Library call tracer
    r'\bgdb\b',  # GNU debugger
    r'\bvalgrind\b',  # Memory debugger
    r'\bmake\b',  # Build tool
    r'\bgcc\b',  # C compiler
    r'\bg\+\+\b',  # C++ compiler
    r'\bclang\b',  # Clang compiler
    r'\bpip\b',  # Python package manager
    r'\bpip3\b',  # Python 3 package manager
    r'\bnpm\b',  # Node package manager
    r'\byarn\b',  # Yarn package manager
    r'\bgem\b',  # Ruby package manager
    r'\bcargo\b',  # Rust package manager
    r'\bman\b',  # Manual pages
    r'\binfo\b',  # Info pages
    r'\bhelp\b',  # Help command
    r'--help\b',  # Help flag (more common in Linux)
    r'\bshutdown\s+-[hrnPH]\b',  # Linux shutdown syntax
    r'\breboot\b',  # Reboot command
    r'\bpoweroff\b',  # Power off command
    r'\bhalt\b',  # Halt command
    r'\binit\s+[0-6]\b',  # Init runlevel
    r'\btelinit\b',  # Change runlevel
    r'\bsudo\s+-i\b',  # Root shell
    r'\bsu\s+-\b',  # Switch user
    r'\bpasswd\b',  # Change password
    r'\buseradd\b',  # Add user
    r'\buserdel\b',  # Delete user
    r'\busermod\b',  # Modify user
    r'\bgroupadd\b',  # Add group
    r'\bgroupdel\b',  # Delete group
    r'\bgroups\b',  # Show groups
    r'\bid\b',  # Show user/group IDs
    r'\bvisudo\b',  # Edit sudoers
    r'\bfdisk\b',  # Partition table manipulator
    r'\bparted\b',  # Partition editor
    r'\blsblk\b',  # List block devices
    r'\bblkid\b',  # Block device attributes
    r'\bmount\b',  # Mount filesystems
    r'\bumount\b',  # Unmount filesystems
    r'\bfstab\b',  # Filesystem table
    r'\bswapoff\b',  # Disable swap
    r'\bswapon\b',  # Enable swap
    r'\bmkfs\b',  # Create filesystem
    r'\be2fsck\b',  # Check ext filesystem
    r'\bfsck\b',  # Check filesystem
    r'\btune2fs\b',  # Tune ext filesystem
    r'\bdmesg\b',  # Kernel messages
    r'\bmodprobe\b',  # Load kernel module
    r'\blsmod\b',  # List kernel modules
    r'\brmmod\b',  # Remove kernel module
    r'\binsmod\b',  # Insert kernel module
    r'\bdpkg\b',  # Debian package manager
    r'\brpm\b',  # RPM package manager
    r'\bzypper\b',  # SUSE package manager
    r'\bemerge\b',  # Gentoo package manager
    r'\bsnap\b',  # Snap package manager
    r'\bflatpak\b',  # Flatpak package manager
    r'\bpython3\b',  # Python 3 (Linux convention)
    r'\./gradlew\b',  # Linux Gradle wrapper
    r'\./mvnw\b',  # Linux Maven wrapper
    r'\buptime\s+-p\b',  # Linux uptime with -p flag
]

def detect_platform(command):
    """Detect the platform based on command content."""
    windows_score = 0
    linux_score = 0

    for pattern in WINDOWS_PATTERNS:
        if re.search(pattern, command, re.IGNORECASE):
            windows_score += 1

    for pattern in LINUX_PATTERNS:
        if re.search(pattern, command, re.IGNORECASE):
            linux_score += 1

    # If one score is clearly higher, use it
    if windows_score > linux_score:
        return 0  # Windows
    elif linux_score > windows_score:
        return 1  # Linux
    else:
        return 2  # Both/Unknown - keep as is

def fix_example_platforms(input_file, output_file):
    with open(input_file, 'r', encoding='utf-8') as f:
        data = json.load(f)

    fixes_made = 0
    actions_with_fixes = 0

    for action in data.get('actions', []):
        # Only process cross-platform actions (platform: 2)
        if action.get('platform') != 2:
            continue

        action_fixed = False

        # Process examples
        if 'examples' in action and action['examples']:
            for example in action['examples']:
                # Only fix examples that are currently set to Both (2)
                if example.get('platform') == 2:
                    detected = detect_platform(example.get('command', ''))
                    if detected != 2:
                        example['platform'] = detected
                        fixes_made += 1
                        action_fixed = True

        if action_fixed:
            actions_with_fixes += 1

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Fixed {fixes_made} examples in {actions_with_fixes} actions")
    return fixes_made, actions_with_fixes

if __name__ == '__main__':
    input_file = r'G:\_dev\TwinShell\TwinShell\data\seed\initial-actions.json'
    output_file = input_file  # Overwrite

    fix_example_platforms(input_file, output_file)
    print("Done!")
