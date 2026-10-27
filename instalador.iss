; =============================================================================
;  instalador.iss  --  Instalador de FileNest (Inno Setup)
;
;  Compilar con:  build-instalador.bat   (o ISCC.exe /DMiVersion=x.y.z instalador.iss)
;  Genera:        salida\FileNest-<version>-instalador.exe
;
;  Los .exe que empaqueta estan en la carpeta "publish" (los crea build.bat).
; =============================================================================

; La version se pasa desde build-instalador.bat, que la lee de Directory.Build.props
#ifndef MiVersion
  #define MiVersion "2.0.0"
#endif

#define MiExe "Organizador de carpetas.exe"

[Setup]
AppName=FileNest
AppVersion={#MiVersion}
AppVerName=FileNest {#MiVersion}
AppPublisher=FileNest
AppPublisherURL=https://github.com/AdriTg00/FileNest
DefaultDirName={autopf}\FileNest
DefaultGroupName=FileNest
DisableProgramGroupPage=yes
OutputDir=salida
OutputBaseFilename=FileNest-{#MiVersion}-instalador
Compression=lzma2/ultra64
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
spanish.MenuFolder=Organizar con FileNest
english.MenuFolder=Organize with FileNest
spanish.MenuHere=Organizar esta carpeta con FileNest
english.MenuHere=Organize this folder with FileNest

[Tasks]
Name: "escritorio"; Description: "{cm:TareaEscritorio}"; GroupDescription: "{cm:GrupoAccesos}"

[Files]
Source: "publish\{#MiExe}";        DestDir: "{app}"; Flags: ignoreversion
Source: "publish\organizador.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\Organizador.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\README.md";       DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FileNest";              Filename: "{app}\{#MiExe}"
Name: "{group}\{cm:ShortConsola}";     Filename: "{app}\Organizador.bat"
Name: "{autodesktop}\FileNest";        Filename: "{app}\{#MiExe}"; Tasks: escritorio

; Menu contextual del Explorador: clic derecho en una carpeta (o dentro de ella).
; Se escribe en HKCU para no pedir permisos de administrador.
[Registry]
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileNest"; ValueType: string; ValueName: ""; ValueData: "{cm:MenuFolder}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileNest"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MiExe},0"
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileNest\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MiExe}"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\FileNest"; ValueType: string; ValueName: ""; ValueData: "{cm:MenuHere}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\FileNest"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MiExe},0"
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\FileNest\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MiExe}"" ""%V"""

[Run]
Filename: "{app}\{#MiExe}"; Description: "{cm:AbrirAhora}"; Flags: nowait postinstall skipifsilent
