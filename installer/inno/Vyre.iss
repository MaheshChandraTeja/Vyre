#define MyAppName "Vyre"
#define MyPublisher "Kairais Tech"

#ifndef Version
#define Version "1.0.0"
#endif

#ifndef SourceDir
#define SourceDir "..\..\src\dotnet\Vyre.App\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif

#ifndef OutputDir
#define OutputDir "..\..\release\windows"
#endif

#ifndef ExeName
#define ExeName "Vyre.App.exe"
#endif

[Setup]
AppId={{8F09243A-4F91-4BDF-84B2-2EF9B4307F3B}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyPublisher}
AppPublisherURL=https://www.kairais.com
AppSupportURL=https://www.kairais.com
AppUpdatesURL=https://www.kairais.com
DefaultDirName={autopf}\Kairais Tech\Vyre
DefaultGroupName=Vyre
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Vyre-{#Version}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayName=Vyre
UninstallDisplayIcon={app}\{#ExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Vyre"; Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\Vyre"; Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch Vyre"; Flags: nowait postinstall skipifsilent