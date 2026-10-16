# FileNest — automatic folder organizer

A **C++** utility with **no external dependencies** that tidies up a folder by
moving every file into a subfolder based on its **extension** (and, optionally,
its **date**). It comes in **two versions** that share the same engine:

| Version | File | When to use it |
|---|---|---|
| 🪟 **Window** | **`Organizador de carpetas.exe`** | The normal case: double-click, choose a folder with the native Windows dialog and see the result |
| ⌨️ **Console** | `organizador.exe` (+ `Organizador.bat`) | Scripts, automation, or if the window version gives you trouble |

> 🌍 **Bilingual:** the interface —and the folders it creates— is in **Spanish if
> your Windows is in Spanish**, and in **English** otherwise. You can force it with
> the environment variable `FILENEST_LANG=es` (or `FILENEST_LANG=en`).

> 🎨 **Its own icon** embedded in both `.exe` files (`recursos/organizador.ico` +
> `recursos/icono.rc`). To change it: replace the `.ico` and rebuild with `build.bat`.

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

1. Bump the version in the **three** places: `FILENEST_VERSION` in `version.hpp`,
   `{#MiVersion}` in `instalador.iss` and `FILEVERSION`/`FileVersion` in
   `recursos/icono.rc`.
2. Push the changes to `main`.
3. On GitHub: **Releases → Draft a new release**, type the new tag (for example
   `v1.0.2`), give it a title and click **Publish release**. Creating the tag makes
   the workflow compile and **attach** the installer and the executables to the Release.

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
  - **Rules…**: add or remove your own `extension → folder` rules (they are saved and
    applied straight away).
  - **Phone**: detects an **Android phone** connected over USB, lets you **choose its
    folder** with a mini browser, shows what it would organize and, when you click
    **Apply**, creates the subfolders and **moves the files** (with an **Undo** option).

On start-up it automatically works out the simulation for your **Downloads** folder.

You can also open it directly on a specific folder:

```bat
"Organizador de carpetas.exe" "C:\Users\adria\Documents\mess"
```

## Download the prebuilt `.exe`

Every time code is pushed, **GitHub compiles the project automatically**
(see `.github/workflows/build.yml`) and leaves both `.exe` files downloadable:

- In the **Actions** tab → latest run → *Artifacts* → **FileNest-windows**.
- And if you publish a **Release** (tag `v1.0`, for example), the `.exe` files are
  attached to the Release by themselves.

## Normal use (console version): `Organizador.bat`

Double-click **`Organizador.bat`** and a **menu** appears:

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  FOLDER ORGANIZER                                 v1.0.1 ║
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

## Look and feel (console)

The console interface is polished: colours per category, a header box, icons by
file type, a bar chart with the breakdown by category and a progress bar while
moving files.

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  FOLDER ORGANIZER                                 v1.0.1 ║
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

**How it works:** when you click *Phone*, the program lists the files in
`/sdcard/Download` and works out their category and target. When you click *Apply*
(and confirm), it **uploads a small `sh` script to the phone and runs it**, which
creates the subfolders and moves every file. Each row is marked **green** (moved) or
**red** (failed).

> This technique (script + `sh`) avoids the quoting problems of `adb shell` and works
> with names containing **accents and spaces**. The script is uploaded to
> `/data/local/tmp/` and deleted when it finishes.

## Installer

```bat
build-instalador.bat
```

It generates `salida\FileNest-<version>-instalador.exe`, which:

- Installs the program in `…\Programs\FileNest` (or wherever you choose).
- Creates shortcuts in the **Start menu** and, if you want, on the **Desktop**.
- Shows up in *Installed apps* so it can be **uninstalled** cleanly.

The installer is available in **Spanish and English** and picks the language from your
Windows automatically.

## Digital signature of the `.exe`

Windows warns about an *"unknown publisher"* when the `.exe` is not signed. To sign it:

- **Just for testing** (what there is now): `recursos/firma-autofirmada.pfx` is a
  self-signed certificate. `firmar.ps1` applies it automatically (`build.bat` calls it).
  On **this PC** the signature shows as *Valid*; on other computers Windows will still warn.
- **To distribute it for real**: replace that `.pfx` with an **OV/EV** certificate
  (or use **Azure Artifact Signing**) and change the password in `firmar.ps1`.
  In GitHub Actions, configure the secrets `SIGNING_PFX` (the `.pfx` in base64)
  and `SIGNING_PFX_PASS`, and the workflow will sign automatically.

```bat
powershell -ExecutionPolicy Bypass -File firmar.ps1
```

## Project layout

```
filenest/
├── nucleo.hpp          Shared engine (categories, plan, apply, undo)
├── gui.cpp             Window version     →  Organizador de carpetas.exe
├── organizador.cpp     Console version    →  organizador.exe
├── Organizador.bat     Menu for the console version
├── idioma.hpp          Language handling (Spanish / English)
├── version.hpp         Version number (single source of truth)
├── build.bat           Builds everything (icon + console + window)
├── README.md
├── .gitignore
└── recursos/
    ├── organizador.ico     Program icon (6 sizes)
    ├── icono.rc            Resource that embeds it into the .exe files
    ├── dialogos.rc         Dialogs, in Spanish and English
    └── icono-fuente.png    Original picture the .ico came from
```

> The `.exe` files and `.o` files are **not uploaded** to the repository: they are
> generated with `build.bat`. The icon does not need compiling either:
> `recursos/organizador.ico` is already there (made from `icono-fuente.png`).

## Building

Double-click `build.bat` (it builds both versions), or do it by hand:

```bat
:: console version
g++ -std=c++20 -O2 -Wall -Wextra -static -o organizador.exe organizador.cpp recursos/icono.res.o -lshell32

:: window version (no console: -mwindows)
g++ -std=c++20 -O2 -Wall -Wextra -municode -mwindows -static -o "Organizador de carpetas.exe" gui.cpp recursos/icono.res.o -lcomctl32 -lole32 -luuid -lshell32
```

(`recursos/icono.res.o` is optional: it is generated with
`windres -O coff -o icono.res.o icono.rc` from inside `recursos/`. If you leave it
out, the `.exe` files come out without their own icon.)

## Default categories

| Folder | Extensions |
|---|---|
| Images | jpg, jpeg, png, gif, bmp, webp, tif, svg, ico, heic, raw, psd, avif… |
| Documents | pdf, doc(x), txt, rtf, odt, xls(x), ppt(x), csv, md, epub… |
| Videos | mp4, mkv, avi, mov, wmv, webm, mpg, 3gp… |
| Audio | mp3, wav, flac, aac, ogg, wma, m4a, opus, mid… |
| Archives | zip, rar, 7z, tar, gz, xz, iso, cab… |
| Programs | exe, msi, msix, appx, bat, cmd, apk, deb… |
| Code | c, cpp, h, cs, java, py, js, ts, html, css, php, rs, go, json, xml, sql… |
| Fonts | ttf, otf, woff, woff2 |
| Other | any other extension |

> On a Windows in Spanish the folders are created with their Spanish names
> (Imágenes, Documentos, Vídeos, Audio, Comprimidos, Programas, Código, Fuentes,
> Otros). The program never goes into a category folder in either language, so a
> folder created in one language is still respected if you switch to the other.

You can create your own rules with `--rule .ext=Folder`.

## Behaviour with subfolders

- **Without `--recursive`** (default): only the files sitting in the root of the
  chosen folder are sorted. Subfolders are left untouched.
- **With `--recursive`**: files inside subfolders are sorted too, but **each one
  inside its own subfolder** (files from different folders are never mixed and the
  structure is preserved).
  E.g. `Downloads\project\logo.png` → `Downloads\project\Images\logo.png`.
- It never goes inside the category folders it has already created.

## What is ignored (never moved)

- **Hidden or system** files (Windows attributes).
- Names starting with **`.`** (including the log file itself).
- `desktop.ini`, `Thumbs.db`.
- **Shortcuts** and links.

## Undo

Every time you apply changes, a file called
`.organizador-log-YYYYMMDD-HHMMSS.csv` is created in the organized folder. To revert
it you can use the **menu** (`Organizador.bat`, options **4** or **5**) or the command:

```bat
organizador.exe --undo "C:\Users\adria\Downloads\.organizador-log-20261015-120000.csv"
```

It restores every file to its original location (the ones still where they were moved).

## Technical details

- A single source file per version, C++20, `std::filesystem`.
- Native UTF-16 paths (supports accents and Spanish names).
- Console output in UTF-8.
- Name collisions are resolved by appending ` (1)`, ` (2)`…
- `-static`: the `.exe` is self-contained (no extra DLLs needed).
