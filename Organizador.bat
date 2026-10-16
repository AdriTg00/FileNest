@echo off
setlocal
set "EXE=%~dp0organizador.exe"
set "DOWN=%USERPROFILE%\Downloads"

rem ---------------------------------------------------------------
rem  Idioma: se lo preguntamos al propio programa (es / en)
rem ---------------------------------------------------------------
set "IDIOMA="
for /f "delims=" %%i in ('"%EXE%" --lang 2^>nul') do set "IDIOMA=%%i"
if /i not "%IDIOMA%"=="es" set "IDIOMA=en"

if /i "%IDIOMA%"=="es" (
  set "TITULO=Organizador de carpetas"
  set "P_OPCION=Elige una opcion [0-5]: "
  set "P_RUTA=Ruta de la carpeta: "
  set "P_APLICAR=Aplicar los cambios en esta carpeta? (S/N): "
  set "P_SEGUIR=Continuar? (S/N): "
  set "P_DEVOLVER=Devolver los archivos a su sitio original? (S/N): "
  set "T_ORGANIZAR=ORGANIZAR UNA CARPETA"
  set "T_AYUDA=Escribe la ruta de la carpeta, o ARRASTRALA hasta esta ventana."
  set "T_EJEMPLO=Ejemplo:  C:\Users\adria\Documents\desorden"
  set "T_VAS=A vas a organizar:  %DOWN%"
  set "T_CARPETA=Carpeta :  %DIR%"
  set "T_REGISTRO=Registro:  %LOG%"
  set "T_INVALIDA=Opcion no valida."
  set "T_VACIO=No has indicado ninguna carpeta."
  set "T_NOEXISTE=La carpeta no existe:"
  set "T_NADA=No se ha movido nada."
  set "T_NOLOG=No se encontro ningun registro de organizacion en:"
  set "T_ADIOS=Hasta luego. :)"
) else (
  set "TITULO=Folder organizer"
  set "P_OPCION=Choose an option [0-5]: "
  set "P_RUTA=Folder path: "
  set "P_APLICAR=Apply the changes in this folder? (Y/N): "
  set "P_SEGUIR=Continue? (Y/N): "
  set "P_DEVOLVER=Put the files back to their original place? (Y/N): "
  set "T_ORGANIZAR=ORGANIZE A FOLDER"
  set "T_AYUDA=Type the folder path, or DRAG the folder into this window."
  set "T_EJEMPLO=Example:  C:\Users\adria\Documents\mess"
  set "T_VAS=You are going to organize:  %DOWN%"
  set "T_CARPETA=Folder:  %DIR%"
  set "T_REGISTRO=Log:     %LOG%"
  set "T_INVALIDA=Invalid option."
  set "T_VACIO=You did not give any folder."
  set "T_NOEXISTE=The folder does not exist:"
  set "T_NADA=Nothing was moved."
  set "T_NOLOG=No organization log found in:"
  set "T_ADIOS=See you later. :)"
)

title %TITULO%

rem ============================================================
rem  MENU PRINCIPAL  (la cabecera la dibuja organizador.exe)
rem ============================================================
:menu
cls
"%EXE%" --menu
set "OP="
set /p "OP=%P_OPCION%"
echo.

if "%OP%"=="1" goto :op1
if "%OP%"=="2" goto :op2
if "%OP%"=="3" goto :op3
if "%OP%"=="4" goto :op4
if "%OP%"=="5" goto :op5
if "%OP%"=="0" goto :salir

echo   %T_INVALIDA%
pause
goto :menu


rem ============================================================
rem  OPCION 1: elegir carpeta y organizarla
rem ============================================================
:op1
cls
echo.
echo   %T_ORGANIZAR%
echo.
echo   %T_AYUDA%
echo   %T_EJEMPLO%
echo.
set "DIR="
set /p "DIR=%P_RUTA%"
set DIR=%DIR:"=%
if "%DIR%"=="" goto :vacio
if not exist "%DIR%" goto :noexiste
if "%DIR:~-1%"=="\" set "DIR=%DIR:~0,-1%"
echo.
"%EXE%" --dir "%DIR%"
echo.
set /p "OK=%P_APLICAR%"
if /i not "%OK%"=="S" if /i not "%OK%"=="Y" goto :noaplicar
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
echo   %T_VAS%
echo.
set /p "OK=%P_SEGUIR%"
if /i not "%OK%"=="S" if /i not "%OK%"=="Y" goto :noaplicar
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
echo   %T_AYUDA%
echo.
set "DIR="
set /p "DIR=%P_RUTA%"
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
echo   %T_CARPETA%
echo   %T_REGISTRO%
echo.
set /p "OK=%P_DEVOLVER%"
if /i not "%OK%"=="S" if /i not "%OK%"=="Y" goto :noaplicar
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
echo   %T_VACIO%
pause
goto :menu

:noexiste
echo.
echo   %T_NOEXISTE%  %DIR%
pause
goto :menu

:noaplicar
echo.
echo   %T_NADA%
pause
goto :menu

:nolog
echo.
echo   %T_NOLOG%
echo     %DIR%
pause
goto :menu

:salir
cls
echo.
echo   %T_ADIOS%
echo.
endlocal
exit /b 0
