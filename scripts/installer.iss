; Inno Setup Script for Mentor - Game Analysis
; Requires Inno Setup 6.x: https://jrsoftware.org/isdl.php

; Version is passed from build-installer.ps1 via command line
; Fallback to 1.0.0 if not specified (for manual compilation)
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "Mentor"
#define MyAppFullName "Mentor - Game Analysis"
#define MyAppPublisher "Neil Clayton"
#define MyAppExeName "Mentor.exe"
#define MyAppId "{{A8F8D9E1-2B3C-4D5E-6F7A-8B9C0D1E2F3A}"

[Setup]
; Basic App Information
AppId={#MyAppId}
AppName={#MyAppFullName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/neilclayton/mentor
AppSupportURL=https://github.com/neilclayton/mentor/issues
AppUpdatesURL=https://github.com/neilclayton/mentor/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
OutputDir=..\dist
OutputBaseFilename=MentorSetup-{#MyAppVersion}
SetupIconFile=
Compression=lzma2
SolidCompression=yes
LZMAUseSeparateProcess=yes
WizardStyle=modern
DisableWelcomePage=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Version Information
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppFullName} Setup
VersionInfoProductName={#MyAppFullName}
VersionInfoProductVersion={#MyAppVersion}

; Uninstall Information
UninstallDisplayName={#MyAppFullName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "launchapp"; Description: "Launch {#MyAppName} after installation"; GroupDescription: "After installation:"; Flags: unchecked

[Files]
Source: "..\dist\Mentor-Windows-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Tasks: launchapp

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Check if .NET 9 runtime is installed (optional check)
  // This is a basic check - you might want to implement a more robust version check
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') and
     not RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost') then
  begin
    if MsgBox('This application requires .NET 9 Runtime to run. It appears .NET may not be installed.' + #13#10 + #13#10 +
              'The application is self-contained and should work, but if you experience issues, ' +
              'you may need to install .NET 9 Runtime.' + #13#10 + #13#10 +
              'Do you want to continue with the installation?',
              mbConfirmation, MB_YESNO) = IDYES then
      Result := True
    else
      Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Any post-installation tasks can go here
  end;
end;

function InitializeUninstall(): Boolean;
var
  Response: Integer;
begin
  Result := True;
  
  Response := MsgBox('Do you want to keep your Mentor data files (saved analyses)?', 
                     mbConfirmation, MB_YESNO or MB_DEFBUTTON2);
  
  if Response = IDYES then
  begin
    // User wants to keep data - we won't delete Documents\Mentor folder
    // The data is stored in user documents, not in the install directory,
    // so it won't be deleted automatically anyway
  end;
end;

