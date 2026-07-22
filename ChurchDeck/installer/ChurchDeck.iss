; Build the application first: dotnet publish -c Release -r win-x64 --self-contained true -o publish
#define MyAppName "ChurchDeck"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ChurchDeck"
#define MyAppExeName "ChurchDeck.exe"

[Setup]
AppId={{D3FEA6D1-6CB4-4ACE-AEAE-C7A33DE0A248}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\ChurchDeck
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=ChurchDeck-Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "data\churchdeck.db"
; Preserve the church's data on repair/upgrade. A new database is installed only if none exists.
Source: "..\publish\data\churchdeck.db"; DestDir: "{app}\data"; Flags: onlyifdoesntexist

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
