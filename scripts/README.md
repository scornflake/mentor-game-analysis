# Distribution Scripts

This directory contains scripts for creating distribution packages from published Mentor.Uno builds.

## macOS

### create-mac-app.sh

Creates a proper macOS `.app` bundle from the published output.

**Usage:**
```bash
./scripts/create-mac-app.sh [osx-arm64|osx-x64]
```

**Output:** `dist/Mentor.app`

**What it does:**
- Creates standard macOS .app bundle structure
- Copies all published files to `Contents/MacOS/`
- Generates `Info.plist` with app metadata (executable name: `Mentor`)
- Sets executable permissions
- Provides instructions for code signing and security

**Prerequisites:**
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64
```

## Windows

### create-windows-dist.sh (Bash)

Creates a clean Windows distribution folder from the published output.

**Usage:**
```bash
./scripts/create-windows-dist.sh [win-x64|win-x86|win-arm64]
```

**Output:** `dist/Mentor-Windows-{arch}/`

**Optional:** Creates a ZIP archive if the `zip` command is available.

### create-windows-dist.ps1 (PowerShell)

Same functionality as the bash script, but for Windows PowerShell.

**Usage:**
```powershell
.\scripts\create-windows-dist.ps1 -Arch win-x64
.\scripts\create-windows-dist.ps1 -Arch win-x86
.\scripts\create-windows-dist.ps1 -Arch win-arm64
```

**Output:** `dist/Mentor-Windows-{arch}/`

**Optional:** Prompts to create a ZIP archive using `Compress-Archive`.

**What it does:**
- Creates clean distribution folder
- Copies all published files (executable: `Mentor.exe`)
- Generates README.txt with usage instructions
- Optionally creates ZIP archive

**Prerequisites:**
```powershell
cd src\Mentor.Uno\Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64
```

### build-installer.ps1 (PowerShell) - Windows Installer

Creates a professional Windows installer using Inno Setup. This is the recommended way to distribute the application on Windows.

**Prerequisites:**
- Install Inno Setup 6: https://jrsoftware.org/isdl.php
- Use default installation options

**Usage:**
```powershell
# Build with auto-determined version from git (recommended)
.\scripts\build-installer.ps1

# Build with specific version (for testing)
.\scripts\build-installer.ps1 -Version "1.0.45"

# Build for different architecture
.\scripts\build-installer.ps1 -Arch "win-x64"
```

**Output:** `dist/MentorSetup-{version}.exe`

**Version Numbering:**
- Version is automatically calculated from git commit count
- Format: `Major.Minor.BuildNumber` (e.g., `1.0.39`)
- To change Major.Minor, edit `VersionPrefix` in `Directory.Build.props` at repository root
- Build number auto-increments with each git commit

**What it does:**
- Automatically runs `create-windows-dist.ps1` to build distribution
- Compiles the Inno Setup script (`installer.iss`)
- Creates a professional Windows installer executable
- Opens the output folder in Explorer when complete

**Installer Features:**
- Standard Windows installation wizard
- Installs to Program Files by default
- Creates Start Menu shortcut
- Optional desktop shortcut
- Uninstaller automatically added to Windows Settings
- Silent install support (`/SILENT`, `/VERYSILENT`)
- Checks for .NET runtime (shows warning if missing)
- ~500KB installer overhead

**Testing:**
```powershell
# Test silent installation
.\dist\MentorSetup-1.0.0.exe /SILENT

# Test uninstallation
# Use Windows Settings → Apps → Mentor → Uninstall
```

**Troubleshooting:**
- If Inno Setup not found, install from https://jrsoftware.org/isdl.php
- Ensure the script runs from the repository root
- Check that `dist/Mentor-Windows-win-x64/` exists after build

## Complete Build Workflow

### macOS
```bash
# 1. Publish
cd src/Mentor.Uno/Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64

# 2. Create .app bundle
cd ../../../
./scripts/create-mac-app.sh osx-arm64

# 3. Run the app
open dist/Mentor.app
```

### Windows (using Bash)
```bash
# 1. Publish
cd src/Mentor.Uno/Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64

# 2. Create distribution
cd ../../../
./scripts/create-windows-dist.sh win-x64

# 3. Package (optional)
cd dist
zip -r Mentor-Windows-win-x64.zip Mentor-Windows-win-x64/
```

### Windows (using PowerShell)
```powershell
# Option 1: Create ZIP distribution
cd src\Mentor.Uno\Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64
cd ..\..\..\
.\scripts\create-windows-dist.ps1 -Arch win-x64

# Option 2: Create installer (RECOMMENDED)
.\scripts\build-installer.ps1 -Version "1.0.0"
```

## Output Structure

```
dist/
├── Mentor.app/                          # macOS application bundle
│   └── Contents/
│       ├── Info.plist
│       ├── MacOS/
│       │   └── Mentor (+ all dependencies)
│       └── Resources/
│
├── Mentor-Windows-win-x64/              # Windows distribution folder
│   ├── Mentor.exe
│   ├── *.dll
│   ├── Assets/
│   └── README.txt
│
├── Mentor-Windows-win-x64.zip           # Optional ZIP archive
│
└── MentorSetup-1.0.0.exe                # Windows installer (recommended)
```

## Distribution

- **macOS:** Distribute `Mentor.app` (optionally in a DMG or ZIP)
- **Windows:** Distribute `MentorSetup-{version}.exe` (recommended) or ZIP folder

## Versioning System

The project uses automatic version numbering based on git commit count.

### Version Format

- **Format:** `Major.Minor.BuildNumber` (e.g., `1.0.39`)
- **Major.Minor:** Manually set in `Directory.Build.props` at repository root
- **BuildNumber:** Automatically calculated from git commit count

### How It Works

1. **Single Source of Truth:** `Directory.Build.props` contains `VersionPrefix` (e.g., `1.0`)
2. **Automatic Build Number:** MSBuild automatically runs `git rev-list --count HEAD` during build
3. **Version Propagation:** All projects inherit the version automatically
4. **Version Display:**
   - App window title: "Mentor - Game Analysis v1.0.39"
   - Installer filename: `MentorSetup-1.0.39.exe`
   - Assembly version: 1.0.39
   - Windows file properties: Version 1.0.39

### Changing the Version

To change the Major.Minor version:

1. Edit `Directory.Build.props` at repository root
2. Update `<VersionPrefix>` to desired value (e.g., `1.1` or `2.0`)
3. Commit the change
4. Build number will continue auto-incrementing from git commit count

### Manual Version Override

For testing or special builds, you can override the version:

```powershell
# Override version for installer build
.\scripts\build-installer.ps1 -Version "1.0.999"
```

### CI/CD Support

The versioning system automatically detects:
- `GITHUB_RUN_NUMBER` (GitHub Actions)
- `BUILD_BUILDNUMBER` (Azure DevOps)
- Falls back to git commit count
- Falls back to `.0` if git is not available

### Version Helper Script

`scripts/Get-Version.ps1` can be used to query the current version:

```powershell
# Get full version (with build number)
.\scripts\Get-Version.ps1 -VersionOnly
# Output: 1.0.39

# Get just build number
.\scripts\Get-Version.ps1 -BuildNumberOnly
# Output: 39

# Get detailed version info
.\scripts\Get-Version.ps1
# Output:
# Version Information:
#   Version Prefix: 1.0
#   Build Number:   39
#   Full Version:   1.0.39
```

## Notes

- All distributions are self-contained (no .NET runtime required)
- macOS apps may show security warnings if unsigned
- Windows installer created with Inno Setup provides professional installation experience
- Installer size: ~70-80MB (includes all dependencies)
- For code signing Windows installers, see Inno Setup documentation
- Version number automatically increments with each commit
- See `docs/build.md` for detailed documentation

