; BlueMax Compatible Installer 6.1 (x64)
; Same application binaries, compatibility-oriented setup profile

#define AppName "Calibration Certificates"
#define AppVersion "6.1"
#define AppPublisher "BlueMax"
#define AppExeName "BlueMax.Presentation.Wpf.exe"
#define AppId "{{E5BCBA70-9A17-41FB-90B1-6BF8A807B3AD}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\BlueMax\Calibration Certificates
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=E:\BlueMax pro 6.1
OutputBaseFilename=BlueMax-Setup-6.1-x64-Compatible
SetupIconFile=E:\BlueMax pro\BlueMax.Presentation.Wpf\Calibration2.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
MinVersion=10.0
SetupLogging=yes

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "E:\BlueMax pro 6.1\App\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon
