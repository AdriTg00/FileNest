@echo off
setlocal
chcp 65001 >nul

rem ============================================================================
rem  build.bat  --  compila FileNest (C# / .NET 8)
rem
rem  Genera en la carpeta "publish":
rem     organizador.exe                 (consola)
rem     Organizador de carpetas.exe     (ventana, WPF)
rem     Organizador.bat  +  README.md   (para el instalador)
rem
rem  Los dos .exe son AUTOCONTENIDOS: no hace falta instalar .NET en el PC.
rem ============================================================================

rem --- Localiza el SDK de .NET (en el PATH o instalado en el perfil) ---
set "DOTNET="
where dotnet >nul 2>nul && set "DOTNET=dotnet"
if not defined DOTNET if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" set "DOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
if not defined DOTNET if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

if not defined DOTNET (
  echo [ERROR] No encuentro el SDK de .NET 8.
  echo         Instalalo con:  winget install Microsoft.DotNet.SDK.8
  exit /b 1
)

echo Compilador: %DOTNET%
echo.

echo [1/4]  Pruebas del motor ...
"%DOTNET%" test tests\FileNest.Tests\FileNest.Tests.csproj -c Release --nologo --verbosity quiet
if errorlevel 1 goto :err

echo.
echo [2/4]  Version CONSOLA  -^>  publish\organizador.exe
"%DOTNET%" publish src\FileNest.Cli\FileNest.Cli.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish --nologo
if errorlevel 1 goto :err

echo.
echo [3/4]  Version VENTANA  -^>  publish\Organizador de carpetas.exe
"%DOTNET%" publish src\FileNest.App\FileNest.App.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o publish --nologo
if errorlevel 1 goto :err

echo.
echo [4/4]  Lanzador y README ...
copy /y "Organizador.bat" "publish\" >nul
copy /y "README.md"       "publish\" >nul
del /q "publish\*.pdb" 2>nul

rem --- Firma digital (opcional: solo si existe firmar.ps1 y el certificado) ---
if exist firmar.ps1 if exist "recursos\firma-autofirmada.pfx" (
  echo.
  echo [firma] Firmando los .exe ...
  powershell -NoProfile -ExecutionPolicy Bypass -File firmar.ps1
)

echo.
echo Compilado correctamente en la carpeta "publish":
echo.
dir /b publish\*.exe
echo.
echo Prueba:  publish\organizador.exe --help
echo         "publish\Organizador de carpetas.exe"
goto :fin

:err
echo.
echo [ERROR] La compilacion ha fallado.
exit /b 1

:fin
endlocal
exit /b 0
