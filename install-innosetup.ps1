# Install Inno Setup
$ErrorActionPreference = "Stop"

Write-Host "Downloading Inno Setup..." -ForegroundColor Yellow
$url = "https://jrsoftware.org/download.php/is.exe"
$output = Join-Path $env:TEMP "innosetup-installer.exe"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $url -OutFile $output -UseBasicParsing

Write-Host "Download complete!" -ForegroundColor Green
Write-Host "Installing Inno Setup (silent install)..." -ForegroundColor Yellow

Start-Process -FilePath $output -ArgumentList "/VERYSILENT","/SUPPRESSMSGBOXES","/NORESTART","/SP-" -Wait

Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "Inno Setup has been installed to: C:\Program Files (x86)\Inno Setup 6\" -ForegroundColor Cyan

# Clean up
Remove-Item $output -Force
