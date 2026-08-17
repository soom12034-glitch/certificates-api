; ============================================================================
;  Calibration Certificates - Professional Installer (Inno Setup 6)
;  Single source package: application + Cairo fonts + optional .NET 8 runtime
;  Installable from an admin account, removable from Control Panel
;  (Programs & Features). User data under %LOCALAPPDATA%\BlueMax and
;  %APPDATA%\BlueMax is NEVER touched on uninstall.
; ============================================================================

#define MyAppName "Calibration Certificates"
#define MyAppVersion "11.3.3"
#define MyAppPublisher "BlueMax"
#define MyAppExeName "BlueMax.Presentation.Wpf.exe"
#define MyAppId "B1C0DE11-0D0E-4C0A-8B1E-5C0FFEE00011"
#define MySourceDir "D:\Calibration Certificates_v11\BlueMax.Presentation.Wpf\bin\Release\net8.0-windows"
#define MyFontsDir "D:\Calibration Certificates_v11\BlueMax.Presentation.Wpf\Assets\Fonts"
#define MyIcon "D:\Calibration Certificates_v11\BlueMax.Presentation.Wpf\Calibration2.ico"
; Set to the actual downloaded runtime installer path, or leave empty to use the
; registry-based check with a download prompt instead of bundling.
#define MyDotNetRuntimeInstaller "D:\Calibration Certificates_v11\Installer\Redist\windowsdesktop-runtime-8.0.29-win-x64.exe"
#define MyDotNetRuntimeFile "windowsdesktop-runtime-8.0.29-win-x64.exe"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://example.com
AppSupportURL=https://example.com
DefaultDirName={autopf}\BlueMax\Calibration Certificates
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
SetupIconFile={#MyIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UsePreviousAppDir=yes
DisableDirPage=auto
DisableReadyPage=no
OutputDir=D:\Calibration Certificates_v11\Installer\Output
OutputBaseFilename=CalibrationCertificates_Setup_{#MyAppVersion}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Installer
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ShowLanguageDialog=no
CloseApplications=no
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; --- Application payload (everything from the Release publish folder) ---
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.xml,Logs\*,Certificates_Output\*,*.publish"

; --- Cairo fonts (Arabic UI + receipts). Registered system-wide. ---
Source: "{#MyFontsDir}\Cairo-Regular.ttf"; DestDir: "{autofonts}"; FontInstall: "Cairo (TrueType)"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#MyFontsDir}\Cairo-Bold.ttf";    DestDir: "{autofonts}"; FontInstall: "Cairo Bold (TrueType)"; Flags: onlyifdoesntexist uninsneveruninstall

; --- Bundled .NET 8 Desktop Runtime (optional, only installed when missing) ---
#if FileExists(MyDotNetRuntimeInstaller)
Source: "{#MyDotNetRuntimeInstaller}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet8DesktopInstalled
#endif

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Install the .NET 8 Desktop Runtime silently when it is missing
#if FileExists(MyDotNetRuntimeInstaller)
Filename: "{tmp}\{#MyDotNetRuntimeFile}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 8 Desktop Runtime..."; Flags: skipifdoesntexist; Check: not IsDotNet8DesktopInstalled
#endif
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
// ---------------------------------------------------------------------------
//  Detect .NET 8 Desktop Runtime (Microsoft.WindowsDesktop.App 8.0.x)
// ---------------------------------------------------------------------------

function IsDotNet8DesktopInstalled(): Boolean;
var
  Keys: array[0..1] of String;
  Versions: array[0..29] of String;
  i, k: Integer;
begin
  Keys[0] := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  Keys[1] := 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App';
  Versions[0] := '8.0.0';  Versions[1] := '8.0.1';  Versions[2] := '8.0.2';
  Versions[3] := '8.0.3';  Versions[4] := '8.0.4';  Versions[5] := '8.0.5';
  Versions[6] := '8.0.6';  Versions[7] := '8.0.7';  Versions[8] := '8.0.8';
  Versions[9] := '8.0.10'; Versions[10] := '8.0.11'; Versions[11] := '8.0.12';
  Versions[12] := '8.0.13'; Versions[13] := '8.0.14'; Versions[14] := '8.0.15';
  Versions[15] := '8.0.16'; Versions[16] := '8.0.17'; Versions[17] := '8.0.18';
  Versions[18] := '8.0.19'; Versions[19] := '8.0.20'; Versions[20] := '8.0.21';
  Versions[21] := '8.0.22'; Versions[22] := '8.0.23'; Versions[23] := '8.0.24';
  Versions[24] := '8.0.25'; Versions[25] := '8.0.26'; Versions[26] := '8.0.27';
  Versions[27] := '8.0.28'; Versions[28] := '8.0.29'; Versions[29] := '8.0.30';

  Result := False;
  for k := 0 to 1 do
  begin
    for i := 0 to 29 do
    begin
      if RegValueExists(HKLM64, Keys[k], Versions[i]) or
         RegValueExists(HKLM32, Keys[k], Versions[i]) or
         RegValueExists(HKCU,  Keys[k], Versions[i]) then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

// ---------------------------------------------------------------------------
//  Kill a running instance before install/upgrade so files can be replaced.
// ---------------------------------------------------------------------------

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  Exec(ExpandConstant('{sys}\taskkill.exe'),
       '/F /IM {#MyAppExeName} /T',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;
