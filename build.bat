@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 goto :nodotnet

if exist "libs\SharpMonoInjector.dll" goto :smiok
if exist "SharpMonoInjector.Console\SharpMonoInjector.dll" goto :smiok
echo [!] SharpMonoInjector.dll not found. See libs\README.md
goto :fail
:smiok

if exist "GamePath.local.props" goto :builddll
if not "%REPO_PATH%"=="" goto :builddll
echo [0/3] Searching for R.E.P.O. ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0find_game.ps1"
if errorlevel 1 goto :fail

:builddll
echo [1/3] Building REPOMenu.dll ...
dotnet build REPOMenu.csproj -c Release -nologo -v q
if errorlevel 1 goto :fail

echo [2/3] Building Loader.exe ...
dotnet build Loader\REPOLoader.csproj -c Release -nologo -v q
if errorlevel 1 goto :fail

echo [3/3] Packing dist ...
if exist dist rmdir /s /q dist
mkdir dist
copy /y bin\Release\REPOMenu.dll dist\ >nul
copy /y Loader\bin\Release\Loader.exe dist\ >nul
copy /y Loader\bin\Release\SharpMonoInjector.dll dist\ >nul

echo.
echo Done. Run: dist\Loader.exe
pause
exit /b 0

:nodotnet
echo [!] dotnet SDK not found. Install from https://dotnet.microsoft.com/download
goto :fail

:fail
echo.
echo [!] Build failed. Delete GamePath.local.props if the game folder is wrong.
pause
exit /b 1
