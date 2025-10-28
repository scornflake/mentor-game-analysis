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
# 1. Publish
cd src\Mentor.Uno\Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64

# 2. Create distribution
cd ..\..\..\
.\scripts\create-windows-dist.ps1 -Arch win-x64

# The script will prompt to create a ZIP archive
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
├── Mentor-Windows-win-x64/              # Windows distribution
│   ├── Mentor.exe
│   ├── *.dll
│   ├── Assets/
│   └── README.txt
│
└── Mentor-Windows-win-x64.zip           # Optional ZIP archive
```

## Distribution

- **macOS:** Distribute `Mentor.app` (optionally in a DMG or ZIP)
- **Windows:** Distribute the folder as a ZIP, or create an installer

## Notes

- All distributions are self-contained (no .NET runtime required)
- macOS apps may show security warnings if unsigned
- Windows distributions can be packaged into installers using tools like Inno Setup or WiX
- See `docs/build.md` for detailed documentation

