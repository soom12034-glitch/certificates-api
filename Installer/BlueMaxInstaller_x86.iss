#define MyAppName "Calibration Certificates"
#define MyAppVersion "6.1.0"
#define MyAppPublisher "BlueMax"
#define MyAppURL "https://"
#define MyAppExeName "BlueMax.Presentation.Wpf.exe"
#define MyAppId "{{E2C7A9BB-F50E-4D75-8B9E-2E3A2D320D03}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=no
OutputDir=d:\BlueMax pro 7.0\Installer\Output
OutputBaseFilename=CalibrationCertificates-Setup-x86
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x86
SetupIconFile=d:\\BlueMax pro 7.0\\BlueMax.Presentation.Wpf\\Calibration2.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\\Arabic.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Only x86 payload
Source: "d:\\BlueMax pro 7.0\\BlueMax.Presentation.Wpf\\dist\\win-x86\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; App icon (used by shortcuts)
Source: "d:\\BlueMax pro 7.0\\BlueMax.Presentation.Wpf\\Calibration2.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; IconFilename: "{app}\\Calibration2.ico"
Name: "{commondesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; IconFilename: "{app}\\Calibration2.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent shellexec; Verb: runas
