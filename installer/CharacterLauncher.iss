#define AppVersion "1.3.0"
#ifndef PackageDirectory
  #define PackageDirectory "..\publish\Z-Orbit"
#endif

[Setup]
#ifdef VerificationBuild
AppId={{10D1BDB6-271A-449B-9F5B-9CF908757E53}
AppName=Z-Orbit Packaging Verification
#else
AppId={{A9DA4B59-8510-44E2-BDA6-4C7449C81DF3}
AppName=Z-Orbit
#endif
AppVersion={#AppVersion}
AppPublisher=ZShonz
DefaultDirName={localappdata}\Programs\Z-Orbit
DefaultGroupName=Z-Orbit
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=output
#ifdef VerificationBuild
OutputBaseFilename=Z-Orbit-Packaging-Verification
#else
OutputBaseFilename=Z-Orbit-Setup-{#AppVersion}
#endif
SetupIconFile=..\assets\app-icon.ico
UninstallDisplayIcon={app}\Z-Orbit.exe
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
#ifndef VerificationBuild
AppMutex=CharacterLauncher.SingleInstance
#endif
CloseApplications=no
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\notices\*"; DestDir: "{app}\notices"; Flags: ignoreversion
Source: "{#PackageDirectory}\Z-Orbit.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\apps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PackageDirectory}\assets\avatars\*"; DestDir: "{app}\assets\avatars"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Z-Orbit"; Filename: "{app}\Z-Orbit.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Z-Orbit"; Filename: "{app}\Z-Orbit.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Z-Orbit.exe"; Description: "Launch Z-Orbit"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Shell, Shortcut: Variant;
  ShortcutPath: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    ShortcutPath := ExpandConstant('{userstartup}\Z-Orbit.lnk');
    if FileExists(ShortcutPath) then
    begin
      try
        Shell := CreateOleObject('WScript.Shell');
        Shortcut := Shell.CreateShortcut(ShortcutPath);
        if CompareText(Shortcut.TargetPath, ExpandConstant('{app}\Z-Orbit.exe')) = 0 then
          DeleteFile(ShortcutPath);
      except
        Log('Startup shortcut could not be inspected. It was left unchanged.');
      end;
    end;
  end;
end;
