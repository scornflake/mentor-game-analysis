# Script to build Mentor Windows installer using Inno Setup
# Usage: 
#   .\scripts\build-installer.ps1                    # Auto-determines version from git
#   .\scripts\build-installer.ps1 -Version "1.0.45"  # Override version
#   .\scripts\build-installer.ps1 -Arch win-x64      # Specify architecture

param(
    [Parameter()]
    [string]$Version = "",
    
    [Parameter()]
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string]$Arch = 'win-x64'
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$InnoScriptPath = Join-Path $ScriptDir "installer.iss"
$DistWinDistScript = Join-Path $ScriptDir "create-windows-dist.ps1"
$DistDir = Join-Path $ProjectRoot "dist"
$GetVersionScript = Join-Path $ScriptDir "Get-Version.ps1"

# Determine version automatically if not provided
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Determining version from git..." -ForegroundColor Yellow
    
    if (-not (Test-Path $GetVersionScript)) {
        Write-Host "❌ ERROR: Get-Version.ps1 not found at: $GetVersionScript" -ForegroundColor Red
        exit 1
    }
    
    try {
        $Version = & $GetVersionScript -VersionOnly
        if ([string]::IsNullOrWhiteSpace($Version)) {
            throw "Get-Version.ps1 returned empty version"
        }
    }
    catch {
        Write-Host "❌ ERROR: Failed to determine version automatically" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "You can manually specify a version using: -Version `"x.y.z`"" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Mentor Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Architecture: $Arch" -ForegroundColor White
Write-Host ""

# Step 1: Check if Inno Setup is installed
Write-Host "Step 1: Checking for Inno Setup..." -ForegroundColor Yellow

$InnoSetupPath = $null

# Check common installation paths
$PossiblePaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

foreach ($Path in $PossiblePaths) {
    if (Test-Path $Path) {
        $InnoSetupPath = $Path
        break
    }
}

# Check registry as fallback
if (-not $InnoSetupPath) {
    try {
        $RegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1"
        if (Test-Path $RegPath) {
            $InstallLocation = (Get-ItemProperty -Path $RegPath).InstallLocation
            $PotentialPath = Join-Path $InstallLocation "ISCC.exe"
            if (Test-Path $PotentialPath) {
                $InnoSetupPath = $PotentialPath
            }
        }
    }
    catch {
        # Registry check failed, continue
    }
}

if (-not $InnoSetupPath) {
    Write-Host ""
    Write-Host "❌ ERROR: Inno Setup 6 not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Inno Setup 6 to build the installer:" -ForegroundColor Yellow
    Write-Host "  Download: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host "  Install using default options" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "After installation, run this script again." -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "✅ Found Inno Setup: $InnoSetupPath" -ForegroundColor Green
Write-Host ""

# Step 2: Check if Inno Setup script exists
if (-not (Test-Path $InnoScriptPath)) {
    Write-Host "❌ ERROR: Inno Setup script not found at: $InnoScriptPath" -ForegroundColor Red
    exit 1
}

# Step 3: Build Windows distribution
Write-Host "Step 2: Building Windows distribution..." -ForegroundColor Yellow
Write-Host ""

try {
    & $DistWinDistScript -Arch $Arch
    if ($LASTEXITCODE -ne 0) {
        throw "Distribution build failed"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR: Failed to build Windows distribution" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Windows distribution created" -ForegroundColor Green
Write-Host ""

# Step 4: Verify distribution folder exists
$ExpectedDistFolder = Join-Path $DistDir "Mentor-Windows-$Arch"
if (-not (Test-Path $ExpectedDistFolder)) {
    Write-Host "❌ ERROR: Expected distribution folder not found: $ExpectedDistFolder" -ForegroundColor Red
    exit 1
}

# Step 5: Compile installer with Inno Setup
Write-Host "Step 3: Compiling installer with Inno Setup..." -ForegroundColor Yellow
Write-Host ""

try {
    # Run Inno Setup compiler with version parameter
    $InnoArgs = @(
        "/DMyAppVersion=$Version",
        $InnoScriptPath
    )
    
    Write-Host "Running: $InnoSetupPath $InnoArgs" -ForegroundColor Gray
    Write-Host ""
    
    & $InnoSetupPath $InnoArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host ""
    Write-Host "❌ ERROR: Installer compilation failed" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Installer compiled successfully" -ForegroundColor Green
Write-Host ""

# Step 6: Verify installer was created
$InstallerName = "MentorSetup-$Version.exe"
$InstallerPath = Join-Path $DistDir $InstallerName

if (-not (Test-Path $InstallerPath)) {
    Write-Host "❌ ERROR: Installer not found at expected location: $InstallerPath" -ForegroundColor Red
    exit 1
}

# Get installer file size
$InstallerSize = (Get-Item $InstallerPath).Length
$InstallerSizeMB = [math]::Round($InstallerSize / 1MB, 2)

# Step 7: Success!
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ INSTALLER BUILD COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installer Details:" -ForegroundColor Cyan
Write-Host "  Name: $InstallerName" -ForegroundColor White
Write-Host "  Location: $InstallerPath" -ForegroundColor White
Write-Host "  Size: $InstallerSizeMB MB" -ForegroundColor White
Write-Host "  Architecture: $Arch" -ForegroundColor White
Write-Host ""
Write-Host "Testing the installer:" -ForegroundColor Yellow
Write-Host "  1. Run the installer on a test machine" -ForegroundColor White
Write-Host "  2. Verify installation completes successfully" -ForegroundColor White
Write-Host "  3. Test the application launches" -ForegroundColor White
Write-Host "  4. Test uninstallation" -ForegroundColor White
Write-Host ""
Write-Host "Silent install: $InstallerName /SILENT" -ForegroundColor Gray
Write-Host "Very silent: $InstallerName /VERYSILENT" -ForegroundColor Gray
Write-Host ""

# Step 8: Open folder in Explorer
Write-Host "Opening installer location in Explorer..." -ForegroundColor Yellow
Start-Process explorer.exe -ArgumentList "/select,`"$InstallerPath`""

Write-Host "✅ Done!" -ForegroundColor Green
Write-Host ""

