#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
  dotnet publish ./tools/Build/Build.csproj --artifacts-path ./artifacts --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  dotnet ./artifacts/publish/Build/release_win-x64/Build.dll $args
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
  Pop-Location
}
