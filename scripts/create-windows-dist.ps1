# Script to create a Windows distribution folder from published Mentor.Uno output
# Usage: .\scripts\create-windows-dist.ps1 [-Arch win-x64]
# Or: .\scripts\create-windows-dist.ps1 -Arch win-x86
# Or: .\scripts\create-windows-dist.ps1 -Arch win-arm64

param(
    [Parameter()]
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string]$Arch = 'win-x64'
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$PublishDir = Join-Path $ProjectRoot "src\Mentor.Uno\Mentor.Uno\bin\Release\net9.0-desktop\$Arch\publish"
$DistDir = Join-Path $ProjectRoot "dist\Mentor-Windows-$Arch"

Write-Host "Creating Windows distribution for $Arch..." -ForegroundColor Cyan

# Check if publish directory exists
if (-not (Test-Path $PublishDir)) {
    Write-Host "Error: Publish directory not found at $PublishDir" -ForegroundColor Red
    Write-Host "Run: cd src\Mentor.Uno\Mentor.Uno; dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=$Arch" -ForegroundColor Yellow
    exit 1
}

# Create dist directory
$DistRootDir = Join-Path $ProjectRoot "dist"
if (-not (Test-Path $DistRootDir)) {
    New-Item -ItemType Directory -Path $DistRootDir | Out-Null
}

# Remove old distribution if it exists
if (Test-Path $DistDir) {
    Write-Host "Removing existing distribution..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DistDir
}

# Create distribution directory
Write-Host "Creating distribution structure..." -ForegroundColor Green
New-Item -ItemType Directory -Path $DistDir | Out-Null

# Copy all published files
Write-Host "Copying published files..." -ForegroundColor Green
Copy-Item -Path "$PublishDir\*" -Destination $DistDir -Recurse -Force

# Create a README for the distribution
$ReadmeContent = @"
Mentor - Game Analysis
======================

To run the application:
  Double-click: Mentor.exe

System Requirements:
- Windows 10 version 1809 or later
- No additional software installation required (self-contained)

For support or issues, please visit:
https://github.com/yourusername/mentor

"@

$ReadmeContent | Out-File -FilePath (Join-Path $DistDir "README.txt") -Encoding utf8

Write-Host ""
Write-Host "✅ Windows distribution created successfully!" -ForegroundColor Green
Write-Host "Location: $DistDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Contents:" -ForegroundColor Yellow
Write-Host "  Executable: Mentor.exe"
Write-Host ""
Write-Host "To run the app:" -ForegroundColor Yellow
Write-Host "  $DistDir\Mentor.exe"
Write-Host ""

# Optionally create a ZIP file
$CreateZip = Read-Host "Create ZIP archive? (y/n)"
if ($CreateZip -eq 'y' -or $CreateZip -eq 'Y') {
    $ZipName = "Mentor-Windows-$Arch.zip"
    $ZipPath = Join-Path $DistRootDir $ZipName
    
    # Remove old ZIP if exists
    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }
    
    Write-Host "Creating $ZipName..." -ForegroundColor Green
    Compress-Archive -Path $DistDir -DestinationPath $ZipPath -CompressionLevel Optimal
    Write-Host "✅ ZIP archive created: dist\$ZipName" -ForegroundColor Green
}

