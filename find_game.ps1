# Finds the R.E.P.O. install folder and writes GamePath.local.props next to this script.
param()
$ErrorActionPreference = 'SilentlyContinue'

function Test-Game($p) { return ($p -and (Test-Path (Join-Path $p 'REPO.exe'))) }

$found = $null

# 1) environment variable
if (Test-Game $env:REPO_PATH) { $found = $env:REPO_PATH }

# 2) running game process
if (-not $found) {
    $proc = Get-Process -Name REPO | Select-Object -First 1
    if ($proc -and $proc.Path) {
        $d = Split-Path $proc.Path
        if (Test-Game $d) { $found = $d }
    }
}

# 3) Steam: registry + libraryfolders.vdf
if (-not $found) {
    $steam = @()
    foreach ($k in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam', 'HKLM:\SOFTWARE\Valve\Steam') {
        $v = Get-ItemProperty $k
        if ($v.SteamPath) { $steam += $v.SteamPath }
        if ($v.InstallPath) { $steam += $v.InstallPath }
    }
    $libs = @()
    foreach ($s in $steam) {
        $s = $s -replace '/', '\'
        $libs += $s
        $vdf = Join-Path $s 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            $text = Get-Content $vdf -Raw
            foreach ($m in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $libs += ($m.Groups[1].Value -replace '\\\\', '\')
            }
        }
    }
    foreach ($l in ($libs | Select-Object -Unique)) {
        $c = Join-Path $l 'steamapps\common\REPO'
        if (Test-Game $c) { $found = $c; break }
    }
}

# 4) common library locations on every drive
if (-not $found) {
    foreach ($d in (Get-PSDrive -PSProvider FileSystem)) {
        foreach ($rel in 'SteamLibrary\steamapps\common\REPO', 'Steam\steamapps\common\REPO', 'Games\Steam\steamapps\common\REPO', 'Program Files (x86)\Steam\steamapps\common\REPO', 'Program Files\Steam\steamapps\common\REPO') {
            $c = Join-Path $d.Root $rel
            if (Test-Game $c) { $found = $c; break }
        }
        if ($found) { break }
    }
}

# 5) ask the user
if (-not $found) {
    $in = Read-Host 'R.E.P.O. was not found automatically. Enter the folder that contains REPO.exe'
    if ($in) { $in = $in.Trim('"').Trim() }
    if (Test-Game $in) { $found = $in }
}

if (-not $found) {
    Write-Host 'R.E.P.O. folder not found.'
    exit 1
}

$esc = [System.Security.SecurityElement]::Escape($found)
$xml = "<Project><PropertyGroup><GamePath>$esc</GamePath></PropertyGroup></Project>"
Set-Content -Path (Join-Path $PSScriptRoot 'GamePath.local.props') -Value $xml -Encoding UTF8
Write-Host "Game folder: $found"
exit 0
