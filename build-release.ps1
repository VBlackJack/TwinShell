# TwinShell Release Build Script
# Creates both portable and installer-ready builds

param(
    [string]$Version = "1.0.0",
    [switch]$SkipClean
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  TwinShell Release Build v$Version" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$RootPath = $PSScriptRoot
$ProjectPath = Join-Path $RootPath "src\TwinShell.App\TwinShell.App.csproj"
$PublishPath = Join-Path $RootPath "publish"
$PortablePath = Join-Path $PublishPath "portable"
$InstallerPath = Join-Path $PublishPath "installer"
$ReleasePath = Join-Path $PublishPath "release"

# Clean previous builds
if (-not $SkipClean) {
    Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $PublishPath) {
        Remove-Item $PublishPath -Recurse -Force
    }
    dotnet clean $ProjectPath --configuration Release --verbosity quiet
    Write-Host "      Done!" -ForegroundColor Green
} else {
    Write-Host "[1/5] Skipping clean (as requested)" -ForegroundColor Gray
}

# Create output directories
Write-Host "[2/5] Creating output directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $PortablePath | Out-Null
New-Item -ItemType Directory -Force -Path $InstallerPath | Out-Null
New-Item -ItemType Directory -Force -Path $ReleasePath | Out-Null
Write-Host "      Done!" -ForegroundColor Green

# Build and publish application
Write-Host "[3/5] Publishing application (Release, win-x64, self-contained)..." -ForegroundColor Yellow
$publishOutput = Join-Path $PublishPath "temp"
dotnet publish $ProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishOutput `
    /p:PublishReadyToRun=true `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "      Done!" -ForegroundColor Green

# Create portable version
Write-Host "[4/5] Creating portable version..." -ForegroundColor Yellow
Copy-Item -Path "$publishOutput\*" -Destination $PortablePath -Recurse -Force

# Create README for portable version
$portableReadme = @"
TwinShell v$Version - Portable Edition
======================================

This is the portable version of TwinShell. No installation required!

To run TwinShell:
1. Extract this entire folder to any location
2. Run TwinShell.App.exe
3. Your data will be stored in: %LOCALAPPDATA%\TwinShell

System Requirements:
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included - self-contained)

For more information, visit: https://github.com/yourusername/TwinShell
"@

$portableReadme | Out-File -FilePath (Join-Path $PortablePath "README.txt") -Encoding UTF8

# Create ZIP for portable version
$portableZip = Join-Path $ReleasePath "TwinShell-v$Version-Portable-win-x64.zip"
Compress-Archive -Path "$PortablePath\*" -DestinationPath $portableZip -Force
$portableSize = (Get-Item $portableZip).Length / 1MB
Write-Host "      Created: $portableZip ($([math]::Round($portableSize, 2)) MB)" -ForegroundColor Green

# Prepare installer files
Write-Host "[5/5] Preparing installer files..." -ForegroundColor Yellow
Copy-Item -Path "$publishOutput\*" -Destination $InstallerPath -Recurse -Force
Write-Host "      Done! Files ready at: $InstallerPath" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Build Summary" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Version:          $Version" -ForegroundColor White
Write-Host "Portable ZIP:     $portableZip" -ForegroundColor White
Write-Host "Installer Files:  $InstallerPath" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run Inno Setup on: build-installer.iss" -ForegroundColor Gray
Write-Host "  2. Test both portable and installer versions" -ForegroundColor Gray
Write-Host "  3. Create GitHub release" -ForegroundColor Gray
Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
