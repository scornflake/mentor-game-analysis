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
$ProjectPath = Join-Path $ProjectRoot "src\Mentor.Uno\Mentor.Uno\Mentor.Uno.csproj"
$PublishDir = Join-Path $ProjectRoot "src\Mentor.Uno\Mentor.Uno\bin\Release\net9.0-desktop\$Arch\publish"
$DistDir = Join-Path $ProjectRoot "dist\Mentor-Windows-$Arch"
$DistRootDir = Join-Path $ProjectRoot "dist"

Write-Host "Creating Windows distribution for $Arch..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean Release build
Write-Host "Step 1: Cleaning Release build..." -ForegroundColor Yellow
$cleanResult = dotnet clean $ProjectPath -c Release -f net9.0-desktop
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Clean completed" -ForegroundColor Green
Write-Host ""

# Step 2: Publish Release build
Write-Host "Step 2: Publishing Release build..." -ForegroundColor Yellow
$publishResult = dotnet publish $ProjectPath -c Release -f net9.0-desktop /p:PublishProfile=$Arch
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Publish completed" -ForegroundColor Green
Write-Host ""

# Step 3: Verify publish directory exists
if (-not (Test-Path $PublishDir)) {
    Write-Host "Error: Publish directory not found at $PublishDir" -ForegroundColor Red
    exit 1
}

# Step 4: Create dist directory
if (-not (Test-Path $DistRootDir)) {
    New-Item -ItemType Directory -Path $DistRootDir
}

# Step 5: Remove old distribution if it exists
if (Test-Path $DistDir) {
    Write-Host "Step 3: Removing existing distribution..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DistDir
}

# Step 6: Create distribution directory
Write-Host "Step 4: Creating distribution structure..." -ForegroundColor Green
New-Item -ItemType Directory -Path $DistDir 

# Step 7: Copy all published files
Write-Host "Step 5: Copying published files..." -ForegroundColor Green
Copy-Item -Path "$PublishDir\*" -Destination $DistDir -Recurse -Force

# Step 8: Create a README for the distribution
Write-Host "Step 6: Creating README..." -ForegroundColor Green
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

# Step 9: Create ZIP archive
$ZipName = "Mentor-Windows-$Arch.zip"
$ZipPath = Join-Path $DistRootDir $ZipName

Write-Host "Step 7: Creating ZIP archive..." -ForegroundColor Green

# Remove old ZIP if exists
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive -Path $DistDir -DestinationPath $ZipPath -CompressionLevel Optimal
Write-Host "✅ ZIP archive created: $ZipPath" -ForegroundColor Green
Write-Host ""

# Step 10: Show ZIP in Explorer
Write-Host "Step 8: Opening ZIP location in Explorer..." -ForegroundColor Green
Start-Process explorer.exe -ArgumentList "/select,`"$ZipPath`""
Write-Host "✅ Explorer opened" -ForegroundColor Green
Write-Host ""
Write-Host "Distribution complete! ZIP file location:" -ForegroundColor Cyan
Write-Host "  $ZipPath" -ForegroundColor White
Write-Host ""

