# FileNest — automatic folder organizer

A **C# / .NET 8** utility that tidies up a folder by moving every file into a
subfolder based on its **extension** (and, optionally, its **date**). It comes in
**two versions** that share the same engine:

| Version | File | When to use it |
|---|---|---|
| 🪟 **Window** | **`Organizador de carpetas.exe`** | The normal case: double-click, choose a folder with the native Windows dialog and see the result |
| ⌨️ **Console** | `organizador.exe` (+ `Organizador.bat`) | Scripts, automation, or if the window version gives you trouble |

> 🌍 **Bilingual:** the interface —and the folders it creates— is in **Spanish if
> your Windows is in Spanish**, and in **English** otherwise. You can force it with
> the environment variable `FILENEST_LANG=es` (or `FILENEST_LANG=en`).

> 📦 **No dependencies:** both `.exe` files are **self-contained** (they bundle the
> .NET runtime). You do **not** need to install .NET: copy and run.

> 🎨 **Its own icon** embedded in both `.exe` files (`recursos/organizador.ico`).
> To change it: replace the `.ico` and rebuild with `build.bat`.

> **It is safe:** by default it runs in **simulation mode**. It only shows what it
> *would* do; nothing is moved until you use `--apply`. It also writes a **log**
> that lets you undo the whole operation.

## Download

On the repository's **Releases** tab there are two ways to get it:

- **Installer** — `FileNest-x.y.z-instalador.exe`: installs it with shortcuts in the
  Start menu and (optionally) the Desktop, and **uninstalls** cleanly.
- **Standalone executables** — `Organizador de carpetas.exe` and `organizador.exe`.
  Just copy them and run; nothing to install.

## Automatic updates

When it starts, FileNest checks the **latest Release** on GitHub and:

- If a newer version exists → it **warns** you and, if you accept, **downloads the
  installer and updates itself** (it closes, updates and you can open it again).
- If you are up to date → it stays quiet (the check is **silent**).

You can also check manually from the menu **Help → Check for updates…**
(and see the version in **Help → About FileNest**).

### Publishing a new version

1. Bump the version in **one single place**: `<Version>` in `Directory.Build.props`.
   (The two `.exe`, the About box and the installer take it from there.)
2. Push the changes to `main`.
3. On GitHub: **Releases → Draft a new release**, type the new tag (for example
   `v2.1.1`) and click **Publish release**. Creating the tag makes the workflow
   compile, build the installer and **attach** everything to the Release.

Users on the previous version will see the update notice when they open FileNest.

## The window version (recommended)

Double-click **`Organizador de carpetas.exe`**. You will see:

- **Folder**: the box with the path and the **Choose folder…** button (it opens the
  native Windows dialog). You can also **drag a folder** and drop it on the window.
- **Options**: *Group by date (Year\Month)* and *Include subfolders*.
- **The list**: every file with its icon, size, category and target.
- **Buttons**:
  - **Simulate**: it only calculates and fills in the list (it touches nothing).
  - **Apply**: it moves the files; each row turns **green** (moved) or **red**
    (failed) and the progress bar advances.
  - **Undo**: it opens a **history** with the last runs on that folder and reverts
    the one you choose.
  - **Open folder**: opens the folder in Explorer.
  - **Theme**: switches between **light and dark** with one click (it remembers your choice).
  - **Rules…**: your own rules. A rule is **pattern → folder**, and both sides accept more
    than you would expect (see below). They are saved and applied straight away.
  - **Phone**: detects an **Android phone** connected over USB, lets you **choose its
    folder** with a mini browser, shows what it would organize and, when you click
    **Apply**, creates the subfolders and **moves the files** (with an **Undo** option).

The same explanations are available **inside the program**: menu **Help → Instructions…**
(or **F1**).

On start-up it automatically works out the simulation for your **Downloads** folder.

> **Only one window at a time:** if you open FileNest again, the window you already
> have comes to the front instead of opening a second one. If you open it with a
> folder (or drag a folder onto the shortcut), the open window switches to it.

> **Explorer context menu:** the installer adds **“Organize with FileNest”** when you
> right-click a folder (and “Organize this folder with FileNest” when you right-click
> the empty space inside one). On **Windows 11** those entries live in the classic
> menu: *Show more options* (Shift+F10).

### Watch a folder (automatic mode)

Tick **Watch this folder** and FileNest stays in the **system tray**, sorting what
arrives on its own:

- Only moves files that arrive **while watching** and have been **2 minutes without
  changes**, so a download in progress is never moved.
- It does **not** touch the pile that was already there (press *Apply* for that).
- Each batch writes its own log, so *Undo* works as usual, and a **tray notification**
  tells you how many files were moved.
- Closing the window keeps FileNest running in the tray (right-click the icon to open
  it, stop watching or quit). The setting is remembered.

You can also open it directly on a specific folder:

```bat
"Organizador de carpetas.exe" "C:\Users\adria\Documents\mess"
```

## Download the prebuilt `.exe`

Every time code is pushed, **GitHub compiles the project automatically**
(see `.github/workflows/build.yml`), runs the tests and leaves the files downloadable:

- In the **Actions** tab → latest run → *Artifacts* → **FileNest-windows**.
- And if you publish a **Release** (tag `v2.1.1`, for example), the `.exe` files are
  attached to the Release by themselves.

## Normal use (console version): `Organizador.bat`

Double-click **`Organizador.bat`** and a **menu** appears:

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  FOLDER ORGANIZER                                 v2.1.1 ║
  ╚══════════════════════════════════════════════════════════════╝

   [1]  Choose a folder and organize it   (simulates, then asks)
   [2]  Simulate on my Downloads folder
   [3]  Apply to my Downloads folder
   [4]  Undo the last run on Downloads
   [5]  Undo specifying the folder
   [0]  Exit

Choose an option [0-5]:
```

- **Option 1**: it asks you for a folder (you can **drag it into the window** or
  paste the path), shows you the simulation and asks whether to apply it.
- If you do not give a folder, it uses your **Downloads** folder by default
  (`%USERPROFILE%\Downloads`).

## Using it from the command line

```bat
organizador.exe --menu                 :: shows the menu (used by the .bat)
organizador.exe                        :: simulates on your Downloads folder
organizador.exe --dir "C:\path"        :: simulates on another folder
organizador.exe --apply                :: actually applies the changes
organizador.exe --apply --by-date      :: also groups into Year\Month
organizador.exe --apply --recursive    :: includes existing subfolders
organizador.exe --rule .psd=Images     :: adds or edits a rule
organizador.exe --undo "C:\...\.organizador-log-XXXX.csv"
organizador.exe --color                :: forces colors (or --no-color)
organizador.exe --help
```

## Default categories

| Category (ES / EN) | Extensions |
|---|---|
| Imágenes / Images | jpg, jpeg, png, gif, bmp, webp, tif, tiff, svg, ico, heic, raw, cr2, nef, psd, avif |
| Documentos / Documents | pdf, doc, docx, txt, rtf, odt, xls, xlsx, ods, ppt, pptx, odp, csv, md, epub |
| Vídeos / Videos | mp4, mkv, avi, mov, wmv, flv, webm, m4v, mpg, mpeg, 3gp, mts, m2ts |
| Audio | mp3, wav, flac, aac, ogg, wma, m4a, opus, mid, midi |
| Comprimidos / Archives | zip, rar, 7z, tar, gz, bz2, xz, iso, cab, zst |
| Programas / Programs | exe, msi, msix, appx, bat, cmd, com, apk, deb, rpm |
| Código / Code | c, cpp, cc, h, hpp, cs, java, py, js, ts, html, htm, css, php, rs, go, rb, sh, json, xml, yml, yaml, sql, ini, toml |
| Fuentes / Fonts | ttf, otf, woff, woff2 |
| Otros / Other | everything else |

> `.ts` goes to **Code** (TypeScript). Video transport streams use `.mts` / `.m2ts`.

## Your own rules

Rules are checked **before** the default categories, in order: **the first match wins**
(in the Rules window, use Up/Down to change the order).

| Pattern | Matches |
|---|---|
| `.psd` | a single extension |
| `factura*.pdf` | any file starting with `factura` and ending in `.pdf` |
| `informe?.txt` | one character where the `?` is |
| `presupuesto 2026.pdf` | that exact file name |

The target folder can use variables, so one rule can sort by date or by name:

| Variable | Value |
|---|---|
| `{año}` · `{year}` | year of the file (last modified) |
| `{mes}` · `{month}` | month, two digits |
| `{día}` · `{day}` | day, two digits |
| `{ext}` | extension, without the dot |
| `{nombre}` · `{name}` | file name without extension |

```
factura*.pdf  →  Facturas\{año}\{mes}
*.psd         →  Diseño
*.jpg         →  Fotos\{año}
```

If a rule already contains `{año}`/`{mes}`, the *Group by date* option does not add them
again for those files.

The same rules can be used from the console:

```bat
organizador.exe --dir "C:\x" --apply --rule "factura*.pdf=Facturas\{año}"
```

## Look and feel (console)

The console interface is polished: colours per category, a header box, icons by
file type, a bar chart with the breakdown by category and a progress bar while
moving files.

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  FOLDER ORGANIZER                                 v2.1.1 ║
  ╚══════════════════════════════════════════════════════════════╝

   📁 Folder        C:\Users\adria\Downloads
   ⚙ Mode           SIMULATION  ·  nothing will be moved
   📅 Date           no
   🔁 Subfolders     no

   11 analyzed   ·   11 to sort

   CATEGORIES
   📄 Documents     4  ████████████████████
   📷 Images        2  ██████████░░░░░░░░░░
   🎵 Audio         1  █████░░░░░░░░░░░░░░░
   ...

   FILES
   📄 final report.pdf         →  Documents\final report.pdf
   📷 holiday photo.jpg        →  Images\holiday photo.jpg
   ...

  ⚠ SIMULATION: nothing was moved.
   To apply it: --apply  ·  or by opening "Organizador.bat"
```

Colours turn on by themselves if the output is a console (your normal window) and
turn off if you redirect the output to a file. You can force them with `--color` or
disable them with `--no-color` (the `NO_COLOR` environment variable is honoured too).

> To see the emojis and accents properly, **Windows Terminal** (bundled with
> Windows 11) or the *Consolas*/*Cascadia Mono* fonts are recommended.

## Organizing your phone (Android)

The **Phone** button uses **adb** (Android Debug Bridge) to work with the phone.

**Requirements** (one-off):
1. On the phone: *Settings → Developer options → **USB debugging*** (enable it).
2. Plug it in over USB and accept the prompt *"Allow USB debugging?"*.
3. `adb` installed on the PC (`winget install Google.PlatformTools`).

**How it works:** when you click *Phone*, the program lists the folders of the
phone, lets you pick one (`/sdcard/Download` by default) and works out the category
and target of every loose file. When you click *Apply* (and confirm), it **uploads a
small `sh` script to the phone and runs it**, which creates the subfolders and moves
every file. Each row is marked **green** (moved) or **red** (failed).

> This technique (script + `sh`) avoids the quoting problems of `adb shell` and works
> with names containing **accents and spaces**. The script is uploaded to
> `/data/local/tmp/` and deleted when it finishes.

## Installer

```bat
build.bat
build-instalador.bat
```

It generates `salida\FileNest-<version>-instalador.exe`, which:

- Installs the program in `…\Programs\FileNest` (or wherever you choose).
- Creates the Start menu entries (window + console) and, optionally, a Desktop icon.
- Adds **“Organize with FileNest”** to the Explorer context menu (per user, no admin).
- Can be uninstalled from *Settings → Apps*; the context menu entries go with it.

## Building from source

Requirements: **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`).

```bat
build.bat              :: tests + the two self-contained .exe into publish\
build-instalador.bat   :: the installer into salida\   (needs Inno Setup 6)
```

Or by hand:

```bat
dotnet test  tests\FileNest.Tests\FileNest.Tests.csproj
dotnet publish src\FileNest.App\FileNest.App.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

> Internals, conventions and known pitfalls are documented in **`MANUAL.md`**
> (Spanish) for whoever maintains the project next.

## Project structure

```
Directory.Build.props     version del producto (unico sitio) + ajustes comunes
FileNest.sln
MANUAL.md                 notas internas (espanol): arquitectura, trampas y checklist
src/
  FileNest.Core/          motor: categorias, planificar, aplicar, deshacer, CSV,
                          reglas, ajustes (registro), Android (adb) y actualizaciones
  FileNest.Cli/           version de consola (organizador.exe)
  FileNest.App/           version con ventana, WPF (Organizador de carpetas.exe)
    Themes/               paletas clara y oscura + estilos
    Dialogs/              historial, reglas y carpeta del movil
    Services/             iconos, tema, barra de titulo, barras de scroll e instancia unica
tests/
  FileNest.Tests/         pruebas del motor (xUnit)
recursos/                 icono y certificado de firma
```

The **Core** project has no UI: the console and the window are two thin layers on
top of the same engine, so both behave exactly the same.

## Long paths

Both executables declare themselves **`longPathAware`**. That is what Windows asks for
to allow paths over 260 characters, and .NET 8 handles them for FileNest's own file
operations (verified with a 311-character folder: files are listed, moved and the log
is written). The Windows policy `LongPathsEnabled = 1`
(`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem`, needs administrator) is **off by
default** and only matters for third-party Win32 APIs.

## A note on the rewrite

FileNest 2.x is a full rewrite from C++/Win32 to **C# / WPF (.NET 8)**. The file
formats are unchanged, so it still reads and undoes the logs created by the old
version (`.organizador-log-*.csv` and `movil-*.csv`), and it keeps the same registry
keys (`HKCU\Software\FileNest`) and rules file (`%LOCALAPPDATA%\FileNest\reglas.txt`).
The old C++ code is still available in the repository history (commit `964ecec`).
