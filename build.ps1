# PowerShell alternative to build.bat
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Write-Host 'dotnet SDK not found'; exit 1 }
if (-not ((Test-Path 'libs\SharpMonoInjector.dll') -or (Test-Path 'SharpMonoInjector.Console\SharpMonoInjector.dll'))) { Write-Host 'SharpMonoInjector.dll not found, see libs\README.md'; exit 1 }
if (-not ((Test-Path 'GamePath.local.props') -or $env:REPO_PATH)) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\find_game.ps1"
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
dotnet build REPOMenu.csproj -c Release -nologo -v q; if ($LASTEXITCODE -ne 0) { exit 1 }
dotnet build Loader\REPOLoader.csproj -c Release -nologo -v q; if ($LASTEXITCODE -ne 0) { exit 1 }
if (Test-Path dist) { Remove-Item dist -Recurse -Force }
New-Item dist -ItemType Directory | Out-Null
Copy-Item bin\Release\REPOMenu.dll dist\
Copy-Item Loader\bin\Release\Loader.exe dist\
Copy-Item Loader\bin\Release\SharpMonoInjector.dll dist\
Write-Host 'Done. Run: dist\Loader.exe'
