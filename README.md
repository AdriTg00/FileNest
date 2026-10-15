# Organizador automático de carpetas

Utilidad en **C++** (sin dependencias) que ordena una carpeta moviendo
cada archivo a una subcarpeta según su **extensión** (y opcionalmente según su
**fecha**). Viene en **dos versiones** que comparten el mismo motor:

| Versión | Archivo | Cuándo usarla |
|---|---|---|
| 🪟 **Con ventana** | **`Organizador de carpetas.exe`** | Lo normal: doble clic, eliges carpeta con el selector de Windows y ves el resultado |
| ⌨️ **Consola** | `organizador.exe` (+ `Organizador.bat`) | Automatizar, scripts o si algo falla con la ventana |

> 🎨 **Icono propio** incrustado en los dos `.exe` (`recursos/organizador.ico` +
> `recursos/icono.rc`). Para cambiarlo: sustituye el `.ico` y recompila con `build.bat`.

> **Es seguro:** por defecto funciona en **modo simulación**. Solo muestra lo que
> haría; no mueve nada hasta que uses `--apply`. Además guarda un **registro** que
> permite deshacer toda la operación.

## La versión con ventana (recomendada)

Doble clic en **`Organizador de carpetas.exe`**. Verás:

- **Carpeta**: el cuadro con la ruta y el botón **Elegir carpeta…** (abre el
  selector nativo de Windows). También puedes **arrastrar una carpeta** y soltarla
  sobre la ventana.
- **Opciones**: *Agrupar por fecha (Año\Mes)* e *Incluir subcarpetas*.
- **La lista**: cada archivo con su icono, tamaño, categoría y destino.
- **Botones**:
  - **Simular**: solo calcula y rellena la lista (no toca nada).
  - **Aplicar**: mueve los archivos; cada fila se pone **verde** (movido) o
    **roja** (error) y la barra de progreso avanza.
  - **Deshacer**: revierte la última ejecución.
  - **Abrir carpeta**: abre la carpeta en el Explorador.

Al arrancar, calcula automáticamente la simulación de tu carpeta **Descargas**.

## Uso normal (versión consola): `Organizador.bat`

Doble clic en **`Organizador.bat`** y aparece un **menú**:

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  ORGANIZADOR DE CARPETAS                            v3.0 ║
  ╚══════════════════════════════════════════════════════════════╝

   [1]  Elegir una carpeta y organizarla   (simula y luego pregunta)
   [2]  Simular sobre mi carpeta Descargas
   [3]  Aplicar en mi carpeta Descargas
   [4]  Deshacer la ultima ejecucion de Descargas
   [5]  Deshacer indicando la carpeta
   [0]  Salir

Elige una opcion [0-5]:
```

- **Opción 1**: te pide una carpeta (puedes **arrastrarla hasta la ventana** o
  pegar la ruta), te muestra la simulación y te pregunta si aplicarla.
- Si no indicas ninguna carpeta, se usa por defecto tu carpeta **Descargas**
  (`%USERPROFILE%\Downloads`).

## Usarlo desde la línea de comandos

```bat
organizador.exe --menu                 :: muestra el menú (lo usa el .bat)
organizador.exe                        :: simula sobre tu carpeta Descargas
organizador.exe --dir "C:\ruta"        :: simula sobre otra carpeta
organizador.exe --apply                :: aplica de verdad los cambios
organizador.exe --apply --by-date      :: agrupa además en Año\Mes
organizador.exe --apply --recursive    :: incluye subcarpetas existentes
organizador.exe --rule .psd=Diseño     :: añade/edita una regla
organizador.exe --undo "C:\...\.organizador-log-XXXX.csv"
organizador.exe --color                :: fuerza los colores (o --no-color)
organizador.exe --help
```

## Aspecto (v3.0)

La interfaz de consola está cuidada: colores por categoría, un recuadro de
cabecera, iconos según el tipo de archivo, un gráfico de barras con el reparto
por categorías y una barra de progreso al mover archivos.

```
  ╔══════════════════════════════════════════════════════════════╗
  ║  🗂  ORGANIZADOR DE CARPETAS                            v3.0 ║
  ╚══════════════════════════════════════════════════════════════╝

   📁 Carpeta       C:\Users\adria\Downloads
   ⚙ Modo          SIMULACIÓN  ·  no se moverá nada
   📅 Fecha         no
   🔁 Subcarpetas   no

   11 analizados   ·   11 a clasificar

   CATEGORÍAS
   📄 Documentos     4  ████████████████████
   📷 Imágenes       2  ██████████░░░░░░░░░░
   🎵 Audio          1  █████░░░░░░░░░░░░░░░
   ...

   ARCHIVOS
   📄 informe final.pdf        →  Documentos\informe final.pdf
   📷 foto vacaciones.jpg      →  Imágenes\foto vacaciones.jpg
   ...

  ⚠ SIMULACIÓN: no se movió nada.
   Para aplicarlo: --apply  ·  o abriendo "Organizador.bat"
```

Los colores se activan solos si la salida es una consola (tu ventana normal) y
se desactivan si rediriges la salida a un archivo. Puedes forzarlos con
`--color` o quitarlos con `--no-color` (también respeta la variable de entorno
`NO_COLOR`).

> Para que los emojis y las tildes se vean bien, se recomienda **Windows
> Terminal** (viene con Windows 11) o la fuente *Consolas*/*Cascadia Mono*.

## Estructura del proyecto

```
organizador/
├── nucleo.hpp          Motor compartido (categorías, planificar, aplicar, deshacer)
├── gui.cpp             Versión con ventana   →  Organizador de carpetas.exe
├── organizador.cpp     Versión consola       →  organizador.exe
├── Organizador.bat     Menú de la versión consola
├── build.bat           Compila todo (icono + consola + ventana)
├── README.md
├── .gitignore
└── recursos/
    ├── organizador.ico     Icono del programa (6 tamaños)
    ├── icono.rc            Recurso que lo incrusta en los .exe
    └── icono-fuente.png    Imagen original de la que salió el .ico
```

> Los `.exe` y los ficheros `.o` **no se suben** al repositorio: se generan con
> `build.bat`. El icono tampoco hace falta compilarlo: `recursos/organizador.ico`
> ya está hecho (de `icono-fuente.png`).

## Compilar

Doble clic en `build.bat` (compila las dos versiones), o a mano:

```bat
:: version consola
g++ -std=c++20 -O2 -Wall -Wextra -static -o organizador.exe organizador.cpp recursos/icono.res.o -lshell32

:: version con ventana (sin consola: -mwindows)
g++ -std=c++20 -O2 -Wall -Wextra -municode -mwindows -static -o "Organizador de carpetas.exe" gui.cpp recursos/icono.res.o -lcomctl32 -lole32 -luuid -lshell32
```

(el `recursos/icono.res.o` es opcional: se genera con `windres -O coff -o icono.res.o icono.rc`
desde dentro de `recursos/`. Si no lo incluyes, los `.exe` salen sin icono propio.)

## Categorías por defecto

| Carpeta | Extensiones |
|---|---|
| Imágenes | jpg, jpeg, png, gif, bmp, webp, tif, svg, ico, heic, raw, psd, avif… |
| Documentos | pdf, doc(x), txt, rtf, odt, xls(x), ppt(x), csv, md, epub… |
| Vídeos | mp4, mkv, avi, mov, wmv, webm, mpg, 3gp… |
| Audio | mp3, wav, flac, aac, ogg, wma, m4a, opus, mid… |
| Comprimidos | zip, rar, 7z, tar, gz, xz, iso, cab… |
| Programas | exe, msi, msix, appx, bat, cmd, apk, deb… |
| Código | c, cpp, h, cs, java, py, js, ts, html, css, php, rs, go, json, xml, sql… |
| Fuentes | ttf, otf, woff, woff2 |
| Otros | cualquier otra extensión |

Puedes crear tus propias reglas con `--rule .ext=Carpeta`.

## Comportamiento con subcarpetas

- **Sin `--recursive`** (por defecto): solo se ordenan los archivos que están en la
  raíz de la carpeta indicada. Las subcarpetas se dejan intactas.
- **Con `--recursive`**: también se ordenan los archivos que hay dentro de las
  subcarpetas, pero **cada uno dentro de su propia subcarpeta** (no se mezclan
  archivos de carpetas distintas y se conserva la estructura).
  Ej: `Downloads\proyecto\logo.png` → `Downloads\proyecto\Imágenes\logo.png`.
- En ningún caso se entra dentro de las carpetas de categoría ya creadas.

## Qué se ignora (nunca se mueve)

- Archivos **ocultos o de sistema** (atributos de Windows).
- Nombres que empiezan por **`.`** (incluido el propio archivo de registro).
- `desktop.ini`, `Thumbs.db`.
- **Accesos directos** y enlaces.

## Deshacer

Cada vez que aplicas cambios se crea un archivo
`.organizador-log-AAAAMMDD-HHMMSS.csv` en la carpeta organizada. Para revertir
puedes usar el **menú** (`Organizador.bat`, opciones **4** o **5**) o el comando:

```bat
organizador.exe --undo "C:\Users\adria\Downloads\.organizador-log-20261015-120000.csv"
```

Restaura cada archivo a su ubicación original (los que sigan donde se movieron).

## Detalles técnicos

- Un solo archivo fuente (`organizador.cpp`), C++20, `std::filesystem`.
- Rutas UTF-16 nativas (soporta acentos y nombres en español).
- Salida de consola en UTF-8.
- Resuelve colisiones de nombres añadiendo ` (1)`, ` (2)`…
- `-static`: el `.exe` es autónomo (no necesita DLLs extra).
