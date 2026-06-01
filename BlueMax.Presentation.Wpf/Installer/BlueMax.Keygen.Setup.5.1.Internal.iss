; BlueMax Keygen Internal Installer 6.1 (x64 + x86, internal use only)
; Build with Inno Setup 6 (ISCC.exe)

#define AppName "BlueMax KeyGen (Internal)"
#define AppVersion "6.1"
#define AppPublisher "BlueMax"
#define AppExeName "BlueMax.KeyGen.UI.exe"
#define AppId "{{C4D64B92-05C3-4AE5-946D-9A46018A286A}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\BlueMax\Calibration Keygen
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=E:\BlueMax pro 6.1
OutputBaseFilename=BlueMax-Keygen-Setup-6.1-Internal
SetupIconFile=E:\BlueMax pro\BlueMax.Presentation.Wpf\Calibration2.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86compatible and x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=10.0

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "E:\BlueMax pro\BlueMax.KeyGen.UI\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon
