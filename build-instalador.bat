@echo off
setlocal
chcp 65001 >nul

rem --- Localiza ISCC.exe (compilador de Inno Setup) ---
set "ISCC="
for /d %%D in ("%LOCALAPPDATA%\Programs\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC for /d %%D in ("%ProgramFiles(x86)%\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC for /d %%D in ("%ProgramFiles%\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC set "ISCC=ISCC.exe"

echo Compilador Inno Setup: %ISCC%
echo.

rem --- Los .exe tienen que existir (los hace build.bat) ---
if not exist "Organizador de carpetas.exe" goto :sinExe
if not exist "organizador.exe" goto :sinExe

echo Compilando el instalador ...
"%ISCC%" instalador.iss
if errorlevel 1 (
  echo.
  echo [ERROR] Fallo al crear el instalador.
  exit /b 1
)

rem --- Firmar el instalador (si hay firmar.ps1 y certificado) ---
for %%F in (salida\FileNest-*-instalador.exe) do (
  if exist "%%F" if exist firmar.ps1 (
    echo.
    echo Firmando el instalador ...
    powershell -NoProfile -ExecutionPolicy Bypass -File firmar.ps1 "%%F"
  )
)

echo.
echo Listo. Instalador en la carpeta "salida":
echo.
dir /b salida\*.exe
goto :fin

:sinExe
echo [ERROR] Faltan los .exe. Ejecuta antes build.bat
exit /b 1

:fin
endlocal
