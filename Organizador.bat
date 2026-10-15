@echo off
setlocal
title Organizador de carpetas

set "EXE=%~dp0organizador.exe"
set "DOWN=%USERPROFILE%\Downloads"

rem ============================================================
rem  MENU PRINCIPAL  (la cabecera la dibuja organizador.exe)
rem ============================================================
:menu
cls
"%EXE%" --menu
set "OP="
set /p "OP=Elige una opcion [0-5]: "
echo.

if "%OP%"=="1" goto :op1
if "%OP%"=="2" goto :op2
if "%OP%"=="3" goto :op3
if "%OP%"=="4" goto :op4
if "%OP%"=="5" goto :op5
if "%OP%"=="0" goto :salir

echo   Opcion no valida.
pause
goto :menu


rem ============================================================
rem  OPCION 1: elegir carpeta y organizarla
rem ============================================================
:op1
cls
echo.
echo   ORGANIZAR UNA CARPETA
echo.
echo   Escribe la ruta de la carpeta, o ARRASTRALA hasta esta ventana.
echo   Ejemplo:  C:\Users\adria\Documents\desorden
echo.
set "DIR="
set /p "DIR=Ruta de la carpeta: "
set DIR=%DIR:"=%
if "%DIR%"=="" goto :vacio
if not exist "%DIR%" goto :noexiste
if "%DIR:~-1%"=="\" set "DIR=%DIR:~0,-1%"
echo.
"%EXE%" --dir "%DIR%"
echo.
set /p "OK=Aplicar los cambios en esta carpeta? (S/N): "
if /i not "%OK%"=="S" goto :noaplicar
echo.
"%EXE%" --dir "%DIR%" --apply
echo.
pause
goto :menu


rem ============================================================
rem  OPCION 2: simular sobre Descargas
rem ============================================================
:op2
cls
"%EXE%" --dir "%DOWN%"
echo.
pause
goto :menu


rem ============================================================
rem  OPCION 3: aplicar en Descargas
rem ============================================================
:op3
cls
echo.
echo   Vas a organizar:  %DOWN%
echo.
set /p "OK=Continuar? (S/N): "
if /i not "%OK%"=="S" goto :noaplicar
echo.
"%EXE%" --dir "%DOWN%" --apply
echo.
pause
goto :menu


rem ============================================================
rem  OPCION 4: deshacer la ultima ejecucion de Descargas
rem ============================================================
:op4
cls
set "DIR=%DOWN%"
goto :undo


rem ============================================================
rem  OPCION 5: deshacer indicando la carpeta
rem ============================================================
:op5
cls
echo.
echo   Escribe la ruta de la carpeta, o ARRASTRALA hasta esta ventana.
echo.
set "DIR="
set /p "DIR=Ruta de la carpeta: "
set DIR=%DIR:"=%
if "%DIR%"=="" goto :vacio
if not exist "%DIR%" goto :noexiste
if "%DIR:~-1%"=="\" set "DIR=%DIR:~0,-1%"
goto :undo


rem ============================================================
rem  Rutina de deshacer (usa la variable DIR)
rem ============================================================
:undo
set "LOG="
for /f "delims=" %%L in ('dir /b /a-d /o-d "%DIR%\.organizador-log-*.csv" 2^>nul') do (
  if not defined LOG set "LOG=%DIR%\%%L"
)
if not defined LOG goto :nolog
echo.
echo   Carpeta :  %DIR%
echo   Registro:  %LOG%
echo.
set /p "OK=Devolver los archivos a su sitio original? (S/N): "
if /i not "%OK%"=="S" goto :noaplicar
echo.
"%EXE%" --undo "%LOG%"
echo.
pause
goto :menu


rem ============================================================
rem  Mensajes
rem ============================================================
:vacio
echo.
echo   No has indicado ninguna carpeta.
pause
goto :menu

:noexiste
echo.
echo   La carpeta no existe:  %DIR%
pause
goto :menu

:noaplicar
echo.
echo   No se ha movido nada.
pause
goto :menu

:nolog
echo.
echo   No se encontro ningun registro de organizacion en:
echo     %DIR%
pause
goto :menu

:salir
cls
echo.
echo   Hasta luego. :)
echo.
endlocal
exit /b 0
