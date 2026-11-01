param(
    [string]$GameDir
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

$il2CppAssemblies = Join-Path $GameDir "MelonLoader\Il2CppAssemblies"
$melondll = Join-Path $GameDir "MelonLoader\net6\Il2CppInterop.Runtime.dll"

if (-not (Test-Path -LiteralPath (Join-Path $il2CppAssemblies "Assembly-CSharp.dll"))) {
    throw "Assembly-CSharp.dll not found. Install MelonLoader for the IL2CPP game build first."
}
if (-not (Test-Path -LiteralPath $melondll)) {
    throw "Il2CppInterop.Runtime.dll not found. Check MelonLoader installation."
}

$escapedGameDir = [System.Security.SecurityElement]::Escape($GameDir)
$escapedAssemblies = [System.Security.SecurityElement]::Escape($il2CppAssemblies)
$escapedMelon = [System.Security.SecurityElement]::Escape((Join-Path $GameDir "MelonLoader\net6"))

$content = @"
<Project>
  <PropertyGroup>
    <LocalGameDir>$escapedGameDir</LocalGameDir>
    <Il2CppAssembliesPath>$escapedAssemblies</Il2CppAssembliesPath>
    <MelonLoaderAssembliesPath>$escapedMelon</MelonLoaderAssembliesPath>
  </PropertyGroup>
</Project>
"@

$propsPath = Join-Path $PSScriptRoot "local.build.props"
Set-Content -LiteralPath $propsPath -Value $content -Encoding UTF8
Write-Host "Wrote $propsPath"
Write-Host "Game path: $GameDir"
Write-Host "Build with: dotnet build .\ScheduleIMegaExpansion.csproj -c Release"
