#define MyAppName "BlueMax Pro"
#define MyAppVersion "4.7.0"
#define MyAppPublisher "High Precision Surveying Establishment"
#define SourceDir "C:\Users\soom12034\Desktop\program\BlueMax\BlueMax pro\BlueMax.Presentation.Wpf\bin\Release\net8.0-windows\win-x64\publish2\"
#define OutputDir "C:\Users\soom12034\Desktop\BlueMax_Final_Installer"
#define OutputBase "BlueMax_Pro_Setup"
#define IconFile "C:\Users\soom12034\Desktop\program\BlueMax\BlueMax pro\BlueMax.Presentation.Wpf\Calibration2.ico"

[Setup]
AppId={{A1B2E34D-9B56-4F77-9B10-3A2BE3C64E91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
Compression=lzma
SolidCompression=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBase}
WizardStyle=modern
DisableProgramGroupPage=yes
DisableReadyMemo=no
LanguageDetectionMethod=none
ShowLanguageDialog=no
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\BlueMax.Presentation.Wpf.exe

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
Name: "{app}"; Flags: uninsalwaysuninstall

[Files]
Source: "{#SourceDir}*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "Template_*.docx;ReportTemplates\*;Certificates_Output\*;*print*template*.*"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\BlueMax.Presentation.Wpf.exe"; IconFilename: "{#IconFile}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\BlueMax.Presentation.Wpf.exe"; Tasks: desktopicon; IconFilename: "{#IconFile}"
