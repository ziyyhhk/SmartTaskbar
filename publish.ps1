# Builds a Release, self-contained, single-file win-x64 SmartTaskbar.exe (Windows 10/11)
# and packages it into SmartTaskbar-win-x64.zip next to this script.
#
# Requirements (on Windows): .NET 6 SDK (https://dotnet.microsoft.com/download/dotnet/6.0)
# Run from PowerShell:  .\publish.ps1

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'Sources\SmartTaskbar\SmartTaskbar.csproj'
$outDir  = Join-Path $PSScriptRoot 'publish\win-x64'
$zipPath = Join-Path $PSScriptRoot 'SmartTaskbar-win-x64.zip'

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $outDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $outDir 'SmartTaskbar.exe'
if (-not (Test-Path $exe)) { throw "Expected output not found: $exe" }

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $exe -DestinationPath $zipPath

Write-Host ""
Write-Host "Built : $exe"
Write-Host "Zip   : $zipPath"
