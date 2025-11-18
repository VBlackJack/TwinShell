; TwinShell Installer Script for Inno Setup
; Requires Inno Setup 6.0 or later: https://jrsoftware.org/isinfo.php

#define MyAppName "TwinShell"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "TwinShell"
#define MyAppURL "https://github.com/yourusername/TwinShell"
#define MyAppExeName "TwinShell.App.exe"
#define MyAppDescription "A comprehensive PowerShell command library for Windows system administrators"

[Setup]
; App identity
AppId={{A7B8C9D0-1234-5678-9ABC-DEF012345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
AppComments={#MyAppDescription}

; Installation paths
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Output
OutputDir=publish\release
OutputBaseFilename=TwinShell-v{#MyAppVersion}-Setup-win-x64
SetupIconFile=src\TwinShell.App\Assets\twinshell-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
LZMANumBlockThreads=4

; System requirements
MinVersion=10.0
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; UI
WizardStyle=modern
DisableWelcomePage=no
LicenseFile=LICENSE.txt
InfoBeforeFile=
InfoAfterFile=

; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application files
Source: "publish\installer\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up user data (optional - commented out by default to preserve user data)
; Type: filesandordirs; Name: "{localappdata}\TwinShell"

[Code]
procedure InitializeWizard;
var
  WelcomeLabel: TNewStaticText;
begin
  WelcomeLabel := TNewStaticText.Create(WizardForm);
  WelcomeLabel.Parent := WizardForm.WelcomePage;
  WelcomeLabel.Caption :=
    'This will install TwinShell v{#MyAppVersion} on your computer.' + #13#10 + #13#10 +
    'TwinShell is a comprehensive PowerShell command library with 500+ commands ' +
    'for Windows system administration, including:' + #13#10 +
    '  • Active Directory management' + #13#10 +
    '  • BitLocker & encryption' + #13#10 +
    '  • Windows Defender' + #13#10 +
    '  • LAPS (Local Admin Password Solution)' + #13#10 +
    '  • System monitoring & diagnostics' + #13#10 +
    '  • And much more!' + #13#10 + #13#10 +
    'Click Next to continue.';
  WelcomeLabel.AutoSize := True;
  WelcomeLabel.WordWrap := True;
  WelcomeLabel.Top := WizardForm.WelcomeLabel2.Top + WizardForm.WelcomeLabel2.Height + 20;
  WelcomeLabel.Width := WizardForm.WelcomeLabel2.Width;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;
