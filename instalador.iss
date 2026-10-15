; =============================================================================
;  instalador.iss  --  Instalador de FileNet (Inno Setup)
;
;  Compilar con:  build-instalador.bat   (o ISCC.exe instalador.iss)
;  Genera:        salida\FileNet-<version>-instalador.exe
; =============================================================================

#define MiVersion "1.0.0"
#define MiExe     "Organizador de carpetas.exe"

[Setup]
AppName=FileNet
AppVersion={#MiVersion}
AppPublisher=FileNet
AppPublisherURL=https://github.com/AdriTg00/FileNet
DefaultDirName={autopf}\FileNet
DefaultGroupName=FileNet
DisableProgramGroupPage=yes
OutputDir=salida
OutputBaseFilename=FileNet-{#MiVersion}-instalador
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MiExe}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=recursos\organizador.ico
LicenseFile=README.md

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un icono en el Escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "{#MiExe}";    DestDir: "{app}"; Flags: ignoreversion
Source: "organizador.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Organizador.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md";       DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FileNet";              Filename: "{app}\{#MiExe}"
Name: "{group}\FileNet (consola)";    Filename: "{app}\Organizador.bat"
Name: "{autodesktop}\FileNet";        Filename: "{app}\{#MiExe}"; Tasks: escritorio

[Run]
Filename: "{app}\{#MiExe}"; Description: "Abrir FileNet ahora"; Flags: nowait postinstall skipifsilent
