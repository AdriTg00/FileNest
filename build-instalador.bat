@echo off
setlocal
chcp 65001 >nul

rem ============================================================================
rem  build-instalador.bat  --  crea salida\FileNest-<version>-instalador.exe
rem
rem  Antes hay que ejecutar build.bat (necesita la carpeta "publish").
rem  La version NO se escribe aqui: se lee de Directory.Build.props.
rem ============================================================================

rem --- Version (unico sitio donde se define) ---
set "VER="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(Select-Xml -Path 'Directory.Build.props' -XPath '/*/*/Version').Node.InnerText"`) do set "VER=%%v"
if not defined VER set "VER=2.0.0"
echo Version: %VER%
echo.

rem --- Localiza ISCC.exe (compilador de Inno Setup) ---
set "ISCC="
for /d %%D in ("%LOCALAPPDATA%\Programs\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC for /d %%D in ("%ProgramFiles(x86)%\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC for /d %%D in ("%ProgramFiles%\Inno Setup *") do if exist "%%D\ISCC.exe" set "ISCC=%%D\ISCC.exe"
if not defined ISCC set "ISCC=ISCC.exe"

echo Compilador Inno Setup: %ISCC%
echo.

rem --- Los .exe tienen que existir (los hace build.bat) ---
if not exist "publish\Organizador de carpetas.exe" goto :sinExe
if not exist "publish\organizador.exe" goto :sinExe

echo Compilando el instalador ...
"%ISCC%" /DMiVersion=%VER% instalador.iss
if errorlevel 1 (
  echo.
  echo [ERROR] Fallo al crear el instalador.
  exit /b 1
)

rem --- Firmar el instalador (si hay certificado) ---
for %%F in (salida\FileNest-*-instalador.exe) do (
  if exist "%%F" if exist "recursos\firma-autofirmada.pfx" (
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
echo [ERROR] Faltan los .exe en "publish". Ejecuta antes build.bat
exit /b 1

:fin
endlocal
exit /b 0
