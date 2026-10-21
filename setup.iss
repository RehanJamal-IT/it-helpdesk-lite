; =====================================================================
;  IT Help Desk Lite - installer script (Inno Setup 6)
;  Copyright (c) 2026 Rehan Jamal - https://github.com/Rehanjamal-IT
;  Built automatically by BUILD.bat
; =====================================================================
#define AppName "IT Help Desk Lite"
#define AppVer "1.1.0"
#define AppExe "IT Help Desk Lite.exe"

[Setup]
AppId={{C878BD00-DADA-4F12-A07E-D8A6387254C6}
AppName={#AppName}
AppVersion={#AppVer}
AppVerName={#AppName} {#AppVer}
AppPublisher=Rehan Jamal
AppPublisherURL=https://github.com/Rehanjamal-IT
AppSupportURL=https://github.com/Rehanjamal-IT/it-helpdesk-lite
AppCopyright=Copyright (c) 2026 Rehan Jamal
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
LicenseFile=LICENSE
OutputDir=dist
OutputBaseFilename=ITHelpDeskLite-Setup-{#AppVer}
SetupIconFile=src\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "dist\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "ithelp.ini"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Open {#AppName} now"; Flags: nowait postinstall skipifsilent

[Code]
var
  CfgPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  CfgPage := CreateInputQueryPage(wpSelectDir,
    'Ticket e-mail settings', 'Where should IT tickets be sent?',
    'Fill this if you are the IT person. You can leave it empty and set it later in the app (Settings tab).');
  CfgPage.Add('Company name:', False);
  CfgPage.Add('Level 1 support e-mail (first-line):', False);
  CfgPage.Add('Level 2 e-mail (IT Manager):', False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  S: String;
begin
  if CurStep = ssPostInstall then
  begin
    if (Trim(CfgPage.Values[1]) <> '') or (Trim(CfgPage.Values[2]) <> '') then
    begin
      S := '; IT Help Desk Lite by Rehan Jamal - settings written by the installer' + #13#10;
      if Trim(CfgPage.Values[0]) <> '' then S := S + 'company=' + Trim(CfgPage.Values[0]) + #13#10;
      if Trim(CfgPage.Values[1]) <> '' then S := S + 'level1_email=' + Trim(CfgPage.Values[1]) + #13#10;
      if Trim(CfgPage.Values[2]) <> '' then S := S + 'level2_email=' + Trim(CfgPage.Values[2]) + #13#10;
      SaveStringToFile(ExpandConstant('{app}\ithelp.ini'), S, False);
    end;
  end;
end;
