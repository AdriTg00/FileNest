@echo off
setlocal
chcp 65001 >nul

rem --- Localiza g++ (WinLibs instalado con winget) ---
set "GXX="
for /d %%D in ("%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs.POSIX.UCRT_*") do (
  if exist "%%D\mingw64\bin\g++.exe" set "GXX=%%D\mingw64\bin\g++.exe"
)
if not defined GXX set "GXX=g++"
for %%I in ("%GXX%") do set "GBIN=%%~dpI"
set "WINDRES=%GBIN%windres.exe"

echo Compilador: %GXX%
echo.

rem --- Icono (opcional: si falta el .ico o windres, se compila sin icono) ---
set "RES="
if exist recursos\organizador.ico if exist recursos\icono.rc if exist "%WINDRES%" (
  echo [1/3]  Icono  -^>  recursos\icono.res.o
  pushd recursos
  "%WINDRES%" -O coff -o icono.res.o icono.rc
  if not errorlevel 1 set "RES=recursos\icono.res.o"
  popd
)

echo [2/3]  Version CONSOLA  -^>  organizador.exe
"%GXX%" -std=c++20 -O2 -Wall -Wextra -static -o organizador.exe organizador.cpp %RES% -lshell32
if errorlevel 1 goto :err

echo [3/3]  Version VENTANA  -^>  Organizador de carpetas.exe
"%GXX%" -std=c++20 -O2 -Wall -Wextra -municode -mwindows -static -o "Organizador de carpetas.exe" gui.cpp %RES% -lcomctl32 -lole32 -luuid -lshell32
if errorlevel 1 goto :err

echo.
echo Compilado correctamente:
echo.
dir /b /a-d *.exe
echo.
echo Prueba:  organizador.exe --help              (consola)
echo         "Organizador de carpetas.exe"       (ventana)
goto :fin

:err
echo.
echo [ERROR] La compilacion ha fallado.
exit /b 1

:fin
endlocal
