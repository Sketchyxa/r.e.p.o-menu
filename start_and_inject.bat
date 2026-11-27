@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion
rem ---- настройки ----
set "SMI=%~dp0smi.exe"
if not exist "%SMI%" set "SMI=%~dp0SharpMonoInjector.Console\smi.exe"
set "DLL=%~dp0bin\Release\REPOMenu.dll"
set "PROC=REPO"
set "APPID=3241660"
set "WAIT_SECONDS=25"
rem -------------------

if /i "%~1"=="unload" goto :unload

if not exist "%SMI%" (
  echo [!] Не найден smi.exe: %SMI%
  echo     Положи SharpMonoInjector smi.exe рядом с этим .bat или поправь путь в начале файла.
  pause & exit /b 1
)
if not exist "%DLL%" (
  echo [!] Не найден %DLL%
  echo     Сначала собери проект: dotnet build -c Release
  pause & exit /b 1
)

tasklist /FI "IMAGENAME eq %PROC%.exe" 2>nul | find /I "%PROC%.exe" >nul
if errorlevel 1 (
  echo Запускаю игру через Steam...
  start "" "steam://rungameid/%APPID%"
  echo Жду процесс %PROC%.exe...
  :waitproc
  timeout /t 2 /nobreak >nul
  tasklist /FI "IMAGENAME eq %PROC%.exe" 2>nul | find /I "%PROC%.exe" >nul
  if errorlevel 1 goto :waitproc
  echo Процесс есть. Жду %WAIT_SECONDS% c, пока загрузится Mono и главное меню...
  timeout /t %WAIT_SECONDS% /nobreak >nul
) else (
  echo Игра уже запущена.
)

echo Инжект...
echo smi: %SMI%
echo dll: %DLL%
"%SMI%" inject -p %PROC% -a "%DLL%" -n r.e.p.o_cheat -c Loader -m Init
echo smi exit code: %errorlevel%
if errorlevel 1 (
  echo [!] smi.exe вернул ошибку. Подожди, пока игра догрузится, и запусти .bat ещё раз.
  pause & exit /b 1
)
echo Готово. Меню: Insert. Выгрузка: F10. Проверка: файл %TEMP%\REPOMenu_loaded.txt
pause
exit /b 0

:unload
"%SMI%" inject -p %PROC% -a "%DLL%" -n r.e.p.o_cheat -c Loader -m UnloadCheat
exit /b %errorlevel%
