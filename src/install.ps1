param(
    [string]$GameDir,
    [string]$DllPath
)

$ErrorActionPreference = "Stop"

if (-not $GameDir) {
    $candidate = Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Schedule I"
    if (Test-Path -LiteralPath $candidate) {
        $GameDir = $candidate
    }
}

if (-not $GameDir -or -not (Test-Path -LiteralPath (Join-Path $GameDir "Schedule I.exe"))) {
    throw "Schedule I install not found. Pass -GameDir explicitly."
}

if (-not $DllPath) {
    $DllPath = Join-Path $PSScriptRoot "bin\Release\net6.0\ScheduleIMegaExpansion.dll"
}

if (-not (Test-Path -LiteralPath $DllPath)) {
    throw "Build DLL not found at '$DllPath'. Run dotnet build -c Release first."
}

$modsDir = Join-Path $GameDir "Mods"
New-Item -ItemType Directory -Path $modsDir -Force | Out-Null
$target = Join-Path $modsDir "ScheduleIMegaExpansion.dll"
Copy-Item -LiteralPath $DllPath -Destination $target -Force

Write-Host "Installed diagnostic mod to $target"
Write-Host "Load a saved game to create the read-only native registry report."
