#ifndef ProfileName
  #define ProfileName "folder"
#endif

[Setup]
AppId=7954d19d-b9c7-42b4-882b-0c03e632e75f
AppName=AppSwitcher
AppVersion={#AppVersion}
AppVerName=AppSwitcher {#AppVersion}
DefaultDirName={autopf}\AppSwitcher
DefaultGroupName=AppSwitcher
UninstallDisplayIcon={app}\AppSwitcher.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Inno_Files
#if ProfileName == "selfcontained"
  OutputBaseFilename=AppSwitcher Standalone Installer
#else
  OutputBaseFilename=AppSwitcher Installer
#endif
PrivilegesRequired=lowest
AppMutex=AppSwitcherMutex
CloseApplications=yes
RestartApplications=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\publish\{#ProfileName}\*"; DestDir: "{app}"; Excludes: ".portable"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{userappdata}\AppSwitcher"; Flags: uninsneveruninstall
Name: "{localappdata}\AppSwitcher"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\AppSwitcher"; Filename: "{app}\AppSwitcher.exe"
Name: "{commondesktop}\AppSwitcher"; Filename: "{app}\AppSwitcher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AppSwitcher.exe"; Description: "{cm:LaunchProgram,AppSwitcher}"; Flags: nowait postinstall skipifsilent

[Code]
function GetWindowsDesktopMajorVersion(const Line: string): Integer;
var
  Prefix: string;
  VersionPart: string;
  DotPos: Integer;
begin
  Result := -1;
  Prefix := 'Microsoft.WindowsDesktop.App ';

  if Pos(Prefix, Line) <> 1 then
    Exit;

  { Example line:
    Microsoft.WindowsDesktop.App 8.0.25 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App] }
  VersionPart := Copy(Line, Length(Prefix) + 1, MaxInt);
  DotPos := Pos('.', VersionPart);

  if DotPos = 0 then
    Exit;

  Result := StrToIntDef(Copy(VersionPart, 1, DotPos - 1), -1);
end;

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
  I: Integer;
  Major: Integer;
  HighestMajor: Integer;
  Output: TExecOutput;
  IsStandalone: Boolean;
begin
  Result := False;
  IsStandalone := '{#ProfileName}' = 'selfcontained';
  if IsStandalone then
  begin
    Result := True;
    Exit;
  end;

  if not ExecAndCaptureOutput('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode, Output) then
  begin
    MsgBox(
      'Unable to query installed .NET runtimes.'#13#10 +
      'Please install .NET Desktop Runtime 8+ and run setup again.',
      mbError,
      MB_OK);
    Exit;
  end;

  for I := 0 to Length(Output.StdOut) - 1 do
  begin
    Major := GetWindowsDesktopMajorVersion(Trim(Output.StdOut[I]));
    if Major >= 8 then
    begin
      Result := True;
      Exit;
    end;
    if Major > HighestMajor then
      HighestMajor := Major;
  end;

  MsgBox(
    'AppSwitcher requires .NET 8.0 or newer.'#13#10 +
    'Detected version: ' + IntToStr(HighestMajor) + '.x'#13#10 +
    'Please install .NET Desktop Runtime 8+ and run setup again.',
    mbError,
    MB_OK
  );
end;

procedure CurUninstallStepChanged(UninstallStep: TUninstallStep);
var
  RoamingDir: String;
  LocalDir: String;
begin
  if UninstallStep = usPostUninstall then
  begin
    RoamingDir := ExpandConstant('{userappdata}\AppSwitcher');
    LocalDir := ExpandConstant('{localappdata}\AppSwitcher');

    if DirExists(RoamingDir) or DirExists(LocalDir) then
    begin
      if MsgBox('Do you want to delete all user data (settings and logs)?' #13#10 #13#10 +
                'This will permanently remove your configurations.',
                mbConfirmation, MB_YESNO) = idYes then
      begin
        if DirExists(RoamingDir) then
        begin
          Log('Removing Roaming data at: ' + RoamingDir);
          DelTree(RoamingDir, True, True, True);
        end;

        if DirExists(LocalDir) then
        begin
          Log('Removing Local data at: ' + LocalDir);
          DelTree(LocalDir, True, True, True);
        end;
      end;
    end;
  end;
end;