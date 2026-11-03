param(
    [Parameter(Mandatory=$true)]
    [string]$GameDir,
    [string]$S1MAPIRef = ""
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "src\HarborlineExpansion.csproj"

if (-not (Test-Path $GameDir)) {
    throw "GameDir does not exist: $GameDir"
}

if ([string]::IsNullOrWhiteSpace($S1MAPIRef)) {
    $S1MAPIRef = Join-Path $PSScriptRoot "Libs\S1MAPI_Mono.dll"
}
if (-not (Test-Path $S1MAPIRef)) {
    throw "Missing S1MAPI compile reference. Download S1MAPI and copy S1MAPI_Mono.dll to Libs, or pass -S1MAPIRef."
}

dotnet build $project -c Il2cpp "-p:GameDir=$GameDir" "-p:S1MAPIRef=$S1MAPIRef"
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

Write-Host "Build finished. Check src\bin\Il2cpp for HarborlineExpansion.dll"