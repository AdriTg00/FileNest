; =============================================================================
;  instalador.iss  --  Instalador de FileNest (Inno Setup)
;
;  Compilar con:  build-instalador.bat   (o ISCC.exe instalador.iss)
;  Genera:        salida\FileNest-<version>-instalador.exe
; =============================================================================

#define MiVersion "1.0.3"
#define MiExe     "Organizador de carpetas.exe"

[Setup]
AppName=FileNest
AppVersion={#MiVersion}
AppPublisher=FileNest
AppPublisherURL=https://github.com/AdriTg00/FileNest
DefaultDirName={autopf}\FileNest
DefaultGroupName=FileNest
DisableProgramGroupPage=yes
OutputDir=salida
OutputBaseFilename=FileNest-{#MiVersion}-instalador
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

; Textos que cambian segun el idioma del instalador (lo elige Inno solo)
[CustomMessages]
spanish.TareaEscritorio=Crear un icono en el Escritorio
english.TareaEscritorio=Create a Desktop icon
spanish.GrupoAccesos=Accesos directos:
english.GrupoAccesos=Shortcuts:
spanish.ShortConsola=FileNest (consola)
english.ShortConsola=FileNest (console)
spanish.AbrirAhora=Abrir FileNest ahora
english.AbrirAhora=Open FileNest now

[Tasks]
Name: "escritorio"; Description: "{cm:TareaEscritorio}"; GroupDescription: "{cm:GrupoAccesos}"

[Files]
Source: "{#MiExe}";    DestDir: "{app}"; Flags: ignoreversion
Source: "organizador.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Organizador.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md";       DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FileNest";              Filename: "{app}\{#MiExe}"
Name: "{group}\{cm:ShortConsola}";     Filename: "{app}\Organizador.bat"
Name: "{autodesktop}\FileNest";        Filename: "{app}\{#MiExe}"; Tasks: escritorio

[Run]
Filename: "{app}\{#MiExe}"; Description: "{cm:AbrirAhora}"; Flags: nowait postinstall skipifsilent
