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

Write-Host "Installed Schedule I Mega Expansion Harborline runtime prototype to $target"
Write-Host "Required dependencies: MelonLoader 0.7.3+, S1API Forked 3.2.2+, S1MAPI 2.0.1+."
Write-Host "Load a single-player save. After the native save-load lifecycle completes, the mod attempts automatic Harborline geometry generation."
Write-Host "Check UserData\ScheduleIMegaExpansion\Diagnostics and Harborline for runtime survey and placement reports."
Write-Warning "This build is not a complete district: native buildings, NPCs, shops, Realtor, police, map integration, persistence and multiplayer remain unimplemented or unverified."
