# FileNest — manual interno

Documento de trabajo (en español) para mantener FileNest: cómo está montado, cómo se
compila, qué trampas tiene y qué no se debe romper. El README.md es la cara pública
(en inglés) y el manual que ve el usuario está **dentro del programa**
(Ayuda → Manual de instrucciones…, o F1); esto es lo de dentro.

Versión actual: **2.4.0**.

---

## 1. Qué es

Utilidad de Windows que ordena una carpeta moviendo cada archivo a una subcarpeta
según su extensión (y opcionalmente por año/mes). Tres piezas:

| Pieza | Proyecto | Ejecutable | Interfaz |
|---|---|---|---|
| Motor | `src/FileNest.Core` | (librería) | ninguna |
| Consola | `src/FileNest.Cli` | `organizador.exe` | menú/ANSI |
| Ventana | `src/FileNest.App` | `Organizador de carpetas.exe` | WPF |

Las dos interfaces usan el mismo motor, así que se comportan igual. Es bilingüe
español/inglés según el idioma de Windows (o `FILENEST_LANG=es|en`).

---

## 2. Requisitos para desarrollar

- **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`). Si está instalado en el
  perfil (`%LOCALAPPDATA%\Microsoft\dotnet`), `build.bat` lo encuentra solo.
- **Inno Setup 6** solo para el instalador (`winget install JRSoftware.InnoSetup`).
  Se busca en `%LOCALAPPDATA%\Programs\Inno Setup *` y en `Program Files`.
- No hay más dependencias: ni NuGet en producción ni librerías nativas.

---

## 3. Estructura

```
Directory.Build.props        version del producto (UNICO sitio) y ajustes comunes
FileNest.sln
src/FileNest.Core/           motor, sin UI
  Categories.cs              tabla de categorias es/en (nombre, icono, color ANSI, extensiones)
  Planner.cs                 simula: decide origen -> destino (no toca el disco)
  RuleEngine.cs              reglas con patron (* y ?) y plantilla ({año} {mes} {ext} {nombre})
  Applier.cs                 aplica el plan y escribe el registro CSV
  Undoer.cs                  deshace desde un registro y limpia carpetas vacias
  CsvLog.cs                  formato de los registros (compatible con la version C++)
  Rules.cs                   reglas propias patron=carpeta (%LOCALAPPDATA%\FileNest\reglas.txt)
  Settings.cs                preferencias en HKCU\Software\FileNest
  Android.cs                 adb: detectar movil, listar, organizar dentro y deshacer
  Updater.cs                 GitHub Releases: comprobar, comparar y descargar el instalador
  Watcher.cs                 modo vigilancia: solo lo nuevo y con 2 min sin cambios
  Duplicates.cs              duplicados por contenido (tamano -> SHA-256) y mover a Duplicados
  Lang.cs / Paths.cs / Model.cs / AppInfo.cs
src/FileNest.Cli/            organizador.exe (Program.cs + ConsoleUi.cs + app.manifest)
src/FileNest.App/            Organizador de carpetas.exe
  MainWindow.xaml(.cs)       ventana principal
  Themes/                    Light.xaml, Dark.xaml (paletas) y Common.xaml (estilos)
  Dialogs/                   historial, reglas y carpeta del movil
  Services/                  IconCache, ThemeManager, TitleBar, ScrollBarStyler, SingleInstance,
                             WatchService (FileSystemWatcher) y TrayIcon (NotifyIcon)
tests/FileNest.Tests/        xUnit del motor (no hay tests de UI)
recursos/                    organizador.ico (lo usan los .exe y el instalador) + certificado de firma
instalador.iss               instalador Inno (incluye el menu contextual del Explorador)
build.bat / build-instalador.bat
firmar.ps1                   firma con el .pfx autofirmado (opcional)
Organizador.bat              lanzador de la consola (menu)
publish/ y salida/           productos de compilacion (ignorados por git)
```

---

## 4. Compilar, probar y empaquetar

```bat
build.bat              :: tests + los dos .exe en publish\ (+ firma si hay .pfx)
build-instalador.bat   :: instalador en salida\FileNest-<version>-instalador.exe
```

A mano:

```bat
dotnet test  tests\FileNest.Tests\FileNest.Tests.csproj -c Release
dotnet build FileNest.sln -c Release

dotnet publish src\FileNest.Cli\FileNest.Cli.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish

dotnet publish src\FileNest.App\FileNest.App.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

- **`IncludeNativeLibrariesForSelfExtract=true` es obligatorio en WPF**: si falta,
  deja sueltas `D3DCompiler_47_cor3.dll`, `wpfgfx_cor3.dll`, etc.
- La consola se publica con `PublishTrimmed` (11 MB); la ventana no se puede recortar.
- Tamaños de referencia: ventana 63 MB, consola 11 MB, instalador 60 MB.

---

## 5. Versión y publicación

**La versión vive en un solo sitio**: `<Version>`, `<AssemblyVersion>` y `<FileVersion>`
en `Directory.Build.props`. Los `.exe` la leen por reflexión (`AppInfo.Version`),
`build-instalador.bat` se la pasa a Inno con `/DMiVersion=...` y el workflow también.

Para publicar:

1. Subir la versión en `Directory.Build.props` (y `app.manifest` de la ventana, que
   lleva su `assemblyIdentity`).
2. `build.bat` + `build-instalador.bat` y probar.
3. Commit + push a `main`.
4. **Release con tag `vX.Y.Z`** (GitHub Actions compila, firma si hay secretos y
   adjunta `FileNest-X.Y.Z-instalador.exe` y los dos `.exe`).

> **Regla de oro**: subir la versión ANTES de crear el tag. Si el código dice 1.0.1 y
> el Release es v1.0.2, el auto-actualizador avisa en cada arranque para siempre
> (pasó de verdad; fue el bug que motivó la versión única).

---

## 6. Cómo funciona por dentro

Flujo de una organización:

1. `MainWindow.Simulate()` (o la consola) construye `Options` (carpeta, `ByDate`,
   `Recursive`, reglas) y llama a `Planner.Build()`.
2. `Planner` recorre la carpeta y, por cada archivo, busca primero una regla propia
   (`RuleEngine`, con patrón y variables); si ninguna coincide, usa la tabla de
   categorías por extensión. Calcula el destino (`Categoría`, `Categoría\Año\Mes`),
   evita colisiones con `nombre (1).ext` y devuelve
   un `Plan` con la lista de movimientos y estadísticas. **No toca nada.**
3. Al pulsar *Aplicar*, `Applier.Apply(plan, carpeta, callback)` crea carpetas, hace
   `File.Move` y escribe `.organizador-log-<stamp>.csv` en la carpeta organizada.
4. *Deshacer* lee ese CSV con `Undoer.Undo()`, devuelve cada archivo a su sitio y
   borra las carpetas de categoría/fecha que queden vacías.

Detalles a respetar:

- **Nunca** se entra en carpetas de categoría al recorrer en recursivo: se comparan
  los nombres en **los dos idiomas** (`Categories.AllNames()`).
- Se omiten archivos ocultos/sistema/reparse (cuentan como "omitidos"), los que
  empiezan por `.`, `desktop.ini` y `thumbs.db`.
- Los nombres duplicados se resuelven en memoria con un `HashSet` case-insensitive,
  no consultando el disco en bucle.
- La ventana aplica en segundo plano (`Task.Run`) y actualiza la lista con
  `IProgress<T>`; la consola usa el mismo callback para la barra de progreso.
- **Una sola instancia** de la ventana: mutex `Local\FileNest.SingleInstance`; la
  segunda instancia avisa por `RegisterWindowMessage` + `HWND_BROADCAST` y la que ya
  está abierta se trae al frente (y cambia de carpeta si se le pasa una).

### Formatos que NO se pueden cambiar (compatibilidad con la 1.x en C++)

- Registro de organización: `.organizador-log-AAAAMMDD-HHMMSS.csv` en la carpeta,
  cabecera `accion,ruta_original,ruta_destino,fecha` y filas `MOVE,...` (CSV con
  comillas dobles escapadas con `""`).
- Registro del móvil: `%LOCALAPPDATA%\FileNest\movil-<stamp>.csv` con 3 columnas.
- Reglas: `%LOCALAPPDATA%\FileNest\reglas.txt`, líneas `patron=carpeta`, `#`/`;` como
  comentario. El FORMATO es el mismo desde la 1.x; desde la 2.2 el patrón admite
  comodines (`factura*.pdf`, `.psd`, nombre exacto) y la carpeta variables (`{año}`,
  `{mes}`, `{día}`, `{ext}`, `{nombre}`). Se aplica la **primera** regla que coincide,
  en el orden del fichero.
- Registro de Windows: `HKCU\Software\FileNest` con `Tema` (DWORD), `UltimaCarpeta`,
  `UltimaCarpetaMovil`, `Ventana`, `VentanaMaximizada`.
- Si se rompe cualquiera de estos, los usuarios pierden el "deshacer" antiguo.

---

## 7. Convenciones

- Comentarios y mensajes internos **en español, sin tildes ni eñes** en el código
  (`arreglado`, `version`, `anadir`); el texto de interfaz sí lleva acentos porque va
  en literales.
- Todo texto visible pasa por `Lang.T("español", "english")` o `Strings.cs`. No dejar
  literales sueltos en la UI.
- Nada de dependencias externas: solo BCL + WPF. Si hace falta algo, se implementa.
- `Core` no puede referenciar WPF ni `System.Windows`.
- Los `async void` solo para manejadores de eventos; el resto `async Task`.
- Errores por archivo no abortan la operación: se cuentan y se pintan en rojo.
- No tocar el estilo implícito de `TextBlock` (ver trampas).

---

## 8. Trampas conocidas (todas nos han costado tiempo)

**WPF**
- Un `Style TargetType="TextBlock"` con `Foreground` **pisa** el color heredado del
  `ListViewItem`: las filas "movidas" (verdes) se veían negras. No poner `Foreground`
  en el estilo implícito de `TextBlock`.
- `Progress<T>` implementa `IProgress<T>.Report` de forma **explícita**: hay que
  declarar la variable como `IProgress<T>`, no como `Progress<T>`.
- En proyectos WPF **faltan los implicit usings de `System.IO`** (choque con
  `Shapes.Path`): poner `using System.IO;` a mano.
- Los comentarios XAML no pueden contener `--` (rompe el XML, MC3000).
- `MenuItem` necesita plantilla propia para el tema oscuro (`PART_Popup` +
  `StackPanel IsItemsHost=True`), si no sale una caja blanca del sistema.
- **ScrollBar**: el `ScrollViewer` crea sus barras con un estilo explícito y además
  el tema fija `MinWidth/MinHeight=17` como valor local. Por eso `ScrollBarStyler`
  las estiliza a mano tras el primer layout (y pone `MinWidth=0`). Un estilo
  implícito **no** basta.
- `PrintWindow` funciona con WPF para capturar la ventana; los `Popup` son ventanas
  aparte.

**cmd / batch**
- `echo algo -> fichero` **redirige** en cmd: hay que escapar `-^>`.
- Los `.bat` con CRLF y terminados en `exit /b 0` (si no, heredan el errorlevel de un
  `del` que falló).
- Pasar rutas con espacios a `powershell -File script.ps1 "a b"` se rompe: mejor que
  el script recorra su carpeta por defecto.
- `echo 0 | Organizador.bat` **se cuelga** (`set /p`): para probarlo de verdad, abrir
  consola y usar `SendKeys`.

**adb (móvil)**
- **Nunca** usar tuberías para leer la salida de adb: el daemon hereda el handle y la
  lectura no termina nunca (la ventana se congelaba). Se redirige a un fichero
  temporal con `cmd /c ... > temp 2>&1`, con timeout y `Kill` si expira.
- El script que se sube al teléfono debe llevar **LF**, no CRLF.
- Se sube a `/data/local/tmp`, se ejecuta con `sh` y se borra.

**Empaquetado / instalación**
- Sin `IncludeNativeLibrariesForSelfExtract` el `.exe` de WPF deja DLLs nativas fuera.
- Inno escribe `[Registry]` **después** de copiar los ficheros: al verificar una
  instalación silenciosa hay que esperar (o comprobar al final) o parece que no se
  escribieron las claves.
- En Windows 11 el menú contextual de registro sale en *Mostrar más opciones*; el
  menú nuevo exige MSIX (`IExplorerCommand`), que no usamos.
- Rutas >260: hace falta `longPathAware` (ya está en los dos manifiestos) y, para
  APIs Win32 de terceros, `LongPathsEnabled=1` (HKLM, admin). .NET 8 ya resuelve los
  recorridos y movimientos.

**Tests / verificación**
- No hay tests de UI: si se toca la ventana, comprobar a mano lo mínimo (aplicar,
  deshacer, tema) o con UI Automation.
- **Nunca dar por hecho el idioma del sistema en un test**: la categoría se llama
  `Documentos` o `Documents` según el Windows. Usar `Categories.Default` /
  `CategoryFor(...)` como en `PlannerTests`. Falló en GitHub (Windows en inglés) con
  los tests de `WatcherTests`, que llevaban "Documentos" escrito a mano.
- Los tests escriben en `%TEMP%` (clase `TempDir`); el del registro del móvil escribe
  en `%LOCALAPPDATA%\FileNest` y borra el fichero al terminar.

---

## 9. Checklist antes de dar algo por terminado

1. `dotnet test` (deben pasar los 41).
2. `build.bat` sin errores y los dos `.exe` arrancan (`--lang` en consola; ventana).
3. Si se tocó la ventana: simular → aplicar → deshacer sobre una carpeta de prueba.
4. Si se tocó el instalador: `build-instalador.bat` y (opcional) instalar en silencio,
   comprobar claves y desinstalar.
5. Si se tocó la versión: comprobar `(Get-Item .\publish\*.exe).VersionInfo`.
6. Commit. El push y el Release los hace el usuario (este PC tiene credenciales de
   otra cuenta y el push da 403).

---

## 10. Tareas frecuentes

- **Cambiar el icono**: sustituir `recursos/organizador.ico` y recompilar. Lo usan los
  dos `.exe` y el instalador (`SetupIconFile`).
- **Cambiar reglas**: RuleEngine.cs (coincidencia y variables) y RulesWindow (UI).
- **Añadir extensiones o una categoría**: `src/FileNest.Core/Categories.cs`, en las
  **dos** tablas (es/en) con las mismas extensiones; el test
  `BothLanguagesShareTheSameExtensions` falla si se desincronizan, y
  `NoExtensionBelongsToTwoCategories` si una extensión se repite.
- **Duplicados**: Duplicates.cs agrupa por tamano y solo calcula SHA-256 de los que empatan; MoveToFolder reutiliza Applier para que quede registro y se pueda deshacer.
- **Menú contextual / bandeja**: el ContextMenu de la bandeja es WPF (respeta el tema); el icono es Forms.NotifyIcon y por eso el proyecto tiene `UseWindowsForms` (con `Using Remove` para que no meta sus using implícitos, que chocan con WPF).
- **Añadir un texto**: `Strings.cs` (ventana) o `Lang.T(...)` (cualquier sitio). En el
  instalador, `[CustomMessages]` con `spanish.` y `english.`.
- **Manual de usuario**: el que se ve en el programa (Ayuda → Manual, F1) está en
  `src/FileNest.App/Dialogs/ManualWindow.xaml.cs`, en el método `Build()`; cada texto va
  con `Lang.T("español", "english")`. Este `MANUAL.md` es solo para desarrollo.
- **Cambiar el repo de actualizaciones**: `AppInfo.Repo` (y el README).
- **Reglas por defecto de un usuario**: no hay; las pone él desde la ventana o con
  `--rule` (que no se guarda).

---

## 11. Limitaciones conocidas

- El **móvil** está portado pero sin probar con un teléfono real desde la 2.0.
- La ventana ocupa **63 MB** (runtime incluido). ReadyToRun bajaría el arranque
  (1,3 s en caliente) a costa de ~30 MB más; no se ha hecho.
- **Sin firma digital**: decisión del autor (el aviso de SmartScreen se acepta). No
  volver a proponerlo.
- El "deshacer" del móvil solo recuerda la última ejecución.
- No hay tests de UI ni integración continua que abra la ventana.
