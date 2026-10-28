# AGENTS.md — FileNest

Utilidad para Windows (C# / .NET 8 + WPF) que ordena carpetas por extensión o por reglas.
Documentación: `README.md` (público, inglés) y `MANUAL.md` (interno, español, con el detalle).
Esto es solo lo que un agente no deduciría leyendo el código.

## Comandos

- `build.bat` → tests + los dos `.exe` en `publish\` (+ firma si existe `recursos\firma-autofirmada.pfx`).
- `build-instalador.bat` → `salida\FileNest-<version>-instalador.exe` (Inno Setup 6).
- `dotnet test tests\FileNest.Tests\FileNest.Tests.csproj -c Release`
- Un solo grupo: `--filter "FullyQualifiedName~Watcher"` (`~Duplicates`, `~RuleEngine`, `~Planner`…).
- El SDK puede no estar en el PATH: `%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe`.

## Reglas del proyecto

- **La versión vive en un solo sitio**: `<Version>` de `Directory.Build.props`. Los `.exe` la leen
  por reflexión y el instalador la recibe con `/DMiVersion`.
- **El tag del Release debe ser `v` + esa versión** (`v2.4.1`). Si no coincide, el
  auto-actualizador ofrece la actualización en cada arranque, para siempre.
- El Release se publica desde la web con el tag `vX.Y.Z` y **el workflow adjunta los binarios**.
  Si el CI falla, el Release queda **vacío**; comprobar que tiene assets antes de dar nada por hecho.
- **El push desde este PC falla (403)**: git tiene credenciales de otra cuenta. El usuario
  empuja y publica; no insistir con `git push`.
- **No proponer firma digital**: está descartada a propósito.
- No cambiar estos formatos (los usuarios pierden el "deshacer" antiguo): `.organizador-log-*.csv`,
  `movil-*.csv`, `%LOCALAPPDATA%\FileNest\reglas.txt` (`patron=carpeta`) y las claves
  `HKCU\Software\FileNest`.
- **Nunca mover un archivo sin apuntarlo antes**: `Applier` escribe la fila del CSV y solo
  después hace `File.Move`. Si el registro no se puede crear, no se toca nada. No volver
  a "mover todo y escribir el log al final": un fallo ahí pierde el deshacer.

## Estructura

- `src/FileNest.Core` — motor sin UI: `Planner` (simula), `Applier` (mueve + registro),
  `Undoer` (deshace y borra carpetas vacías), `RuleEngine` (patrones y variables),
  `Watcher` (vigilancia), `Duplicates` (hash), `Android` (adb), `Updater` (GitHub).
- `src/FileNest.Cli` — `organizador.exe`. `src/FileNest.App` — WPF `Organizador de carpetas.exe`.
- `tests/FileNest.Tests` — xUnit del motor. **No hay tests de UI.**

## Trampas que ya han costado tiempo

- **Nunca escribir nombres de categoría a mano en los tests**: dependen del idioma
  (`Documentos`/`Documents`). Usar `Categories.Default` o el helper `CategoryFor` de `PlannerTests`.
  Se reproduce con `FILENEST_LANG=en`; ya rompió el CI (runner en inglés).
- **Vigilancia**: solo mueve lo que llega *después* de activarla y lleva 2 min sin cambios.
  La "llegada" es `max(LastWriteTime, CreationTime)` porque un archivo **copiado** conserva
  la fecha del original.
- **Duplicados**: `Duplicados`/`Duplicates` se excluye siempre del escaneo recursivo **y** del
  `Planner`, o las copias apartadas reaparecen (o vuelven a las categorías). Los nombres de
  carpeta dependen del idioma: usar `Duplicates.FolderNames`, nunca el literal.
- XAML: los comentarios no admiten `--`. No poner `Foreground` en el estilo implícito de
  `TextBlock` (pisa el color verde/rojo de las filas). Usar `IProgress<T>`, no `Progress<T>`.
  Los proyectos WPF no traen el `using System.IO` implícito.
- Las barras de scroll: el `ScrollViewer` les pone estilo propio y `MinWidth=17` local, así que
  se estilan en `ScrollBarStyler` después del primer layout (un estilo implícito no basta).
- `UseWindowsForms` está solo por el `NotifyIcon`; `<Using Remove>` evita sus `using` implícitos,
  que chocan con WPF (`Application`, `Color`, `Brush`, `DragEventArgs`…).
- Publicar WPF en un solo `.exe` exige `-p:IncludeNativeLibrariesForSelfExtract=true`, si no deja
  `wpfgfx_cor3.dll` y compañía fuera.
- En `.bat`, `echo algo -> x` **redirige** (hay que escapar `-^>`); usar CRLF y terminar en
  `exit /b 0`. `echo 0 | Organizador.bat` se cuelga: probarlo con SendKeys sobre una consola real.
- `publish\` y `salida\` son artefactos ignorados por git.

## Verificación

- Mínima y suficiente: `dotnet test` y, si se toca el motor o el empaquetado, `build.bat`.
  Si se toca la ventana, una pasada real (simular → aplicar → deshacer) o UI Automation.
- Comprobar con **datos** (tamaños, rutas, claves del registro, contadores), no con capturas.
- UI Automation: `TogglePattern` en un `CheckBox` **no** dispara `Click`; enfocar y enviar Espacio.
  El botón de un diálogo modal (MessageBox) suele resolverse mejor enfocando y con `{ENTER}`/`{ESC}`.
