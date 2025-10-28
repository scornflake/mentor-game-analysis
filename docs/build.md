# Building Mentor Executables

This guide covers how to build distributable executables for the Mentor project on macOS and Windows.

## Prerequisites

- **.NET 8 SDK** - Required for Mentor.CLI and Mentor.Core
- **.NET 9 SDK** - Required for Mentor.Uno (desktop GUI app)

Verify installations:
```bash
dotnet --list-sdks
```

## Projects Overview

The Mentor solution contains two executable projects:

1. **Mentor.CLI** - Command-line interface (.NET 8)
2. **Mentor.Uno** - Desktop GUI application (.NET 9, Uno Platform with Skia renderer)

---

## Building Mentor.CLI (Command-Line App)

The CLI can be built as a single executable file for easy distribution.

### macOS

**Intel Macs (x64):**
```bash
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
```

**Apple Silicon Macs (M1/M2/M3 - ARM64):**
```bash
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
```

**Output:** `src/Mentor.CLI/bin/Release/net8.0/{runtime-id}/publish/Mentor.CLI`

### Windows

**64-bit Windows (x64):**
```bash
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**ARM64 Windows:**
```bash
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**32-bit Windows (x86):**
```bash
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**Output:** `src/Mentor.CLI/bin/Release/net8.0/{runtime-id}/publish/Mentor.CLI.exe`

---

## Building Mentor.Uno (Desktop GUI App)

The Uno Platform app uses pre-configured publish profiles for streamlined builds.

### From Project Directory

Navigate to the project:
```bash
cd src/Mentor.Uno/Mentor.Uno
```

### Windows

**64-bit Windows (most common):**
```bash
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64
```

**ARM64 Windows:**
```bash
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-arm64
```

**32-bit Windows:**
```bash
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x86
```

**Output:** `src/Mentor.Uno/Mentor.Uno/bin/Release/net9.0-desktop/{runtime-id}/publish/`

### macOS

**Intel Macs (x64):**
```bash
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-x64
```

**Apple Silicon Macs (ARM64):**
```bash
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64
```

**Output:** `src/Mentor.Uno/Mentor.Uno/bin/Release/net9.0-desktop/{runtime-id}/publish/`

### From Solution Root

You can also build from the solution root by specifying the full project path:

```bash
# Windows x64
dotnet publish src/Mentor.Uno/Mentor.Uno/Mentor.Uno.csproj -c Release -f net9.0-desktop /p:PublishProfile=win-x64

# macOS ARM64
dotnet publish src/Mentor.Uno/Mentor.Uno/Mentor.Uno.csproj -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64
```

---

## Creating macOS .app Bundle

After publishing the Uno app for macOS, you can create a proper `.app` bundle for easier distribution and double-click launching.

### Automated Script

Use the provided script to automatically create the .app bundle:

```bash
# For Apple Silicon (M1/M2/M3)
./scripts/create-mac-app.sh osx-arm64

# For Intel Macs
./scripts/create-mac-app.sh osx-x64
```

The script will:
1. Create the proper `.app` bundle structure
2. Copy all published files into `Contents/MacOS/`
3. Generate an `Info.plist` with app metadata
4. Set executable permissions
5. Output the bundle to `dist/Mentor.app`

### Running the App

```bash
# Open the app
open dist/Mentor.app

# Or double-click in Finder
```

### First Run Security

macOS may block unsigned apps. To allow it:

```bash
# Remove quarantine attribute
xattr -cr dist/Mentor.app

# Or right-click → Open (hold Control key)
```

### Code Signing (Optional)

For distribution, you should sign the app:

```bash
# Ad-hoc signing (local use)
codesign --force --deep --sign - dist/Mentor.app

# Developer ID signing (distribution)
codesign --force --deep --sign "Developer ID Application: Your Name" dist/Mentor.app
```

### Manual Bundle Creation

If you prefer to create the bundle manually:

```bash
# Create structure
mkdir -p MyApp.app/Contents/MacOS
mkdir -p MyApp.app/Contents/Resources

# Copy published files
cp -R src/Mentor.Uno/Mentor.Uno/bin/Release/net9.0-desktop/osx-arm64/publish/* MyApp.app/Contents/MacOS/

# Create Info.plist (see scripts/create-mac-app.sh for template)
nano MyApp.app/Contents/Info.plist

# Make executable
chmod +x MyApp.app/Contents/MacOS/Mentor
```

---

## Creating Windows Distribution

After publishing the Uno app for Windows, you can create a clean distribution folder for easier packaging and distribution.

### Automated Scripts

#### Using PowerShell (on Windows)

```powershell
# For 64-bit Windows (most common)
.\scripts\create-windows-dist.ps1 -Arch win-x64

# For 32-bit Windows
.\scripts\create-windows-dist.ps1 -Arch win-x86

# For ARM64 Windows
.\scripts\create-windows-dist.ps1 -Arch win-arm64
```

#### Using Bash (cross-platform)

```bash
# For 64-bit Windows (most common)
./scripts/create-windows-dist.sh win-x64

# For 32-bit Windows
./scripts/create-windows-dist.sh win-x86

# For ARM64 Windows
./scripts/create-windows-dist.sh win-arm64
```

The script will:
1. Create a clean distribution folder in `dist/Mentor-Windows-{arch}/`
2. Copy all published files
3. Generate a README.txt with usage instructions
4. Optionally create a ZIP archive for distribution

### Distribution Output

The distribution will be created at:
```
dist/
  └── Mentor-Windows-{arch}/
      ├── Mentor.exe           (Main executable)
      ├── *.dll                (Dependencies)
      ├── Assets/              (Application resources)
      └── README.txt           (Usage instructions)
```

### Running the App

Users can simply:
1. Extract the ZIP file (if archived)
2. Double-click `Mentor.exe`

No .NET installation required (self-contained).

### Creating Installers (Optional)

For professional distribution, consider creating installers using:

- **Inno Setup** (Free): https://jrsoftware.org/isinfo.php
- **WiX Toolset** (Free): https://wixtoolset.org/
- **Advanced Installer** (Commercial): https://www.advancedinstaller.com/

Example Inno Setup script snippet:
```ini
[Setup]
AppName=Mentor
AppVersion=1.0
DefaultDirName={pf}\Mentor
DefaultGroupName=Mentor
OutputDir=dist
OutputBaseFilename=MentorSetup

[Files]
Source: "dist\Mentor-Windows-win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\Mentor"; Filename: "{app}\Mentor.exe"
Name: "{commondesktop}\Mentor"; Filename: "{app}\Mentor.exe"
```

---

## Build Options Explained

### Self-Contained (`--self-contained true`)
Includes the .NET runtime with the application. Users don't need .NET installed.
- **Pros:** No runtime dependencies, works on any compatible system
- **Cons:** Larger file size (~60-100MB)

### Framework-Dependent (`--self-contained false`)
Requires .NET runtime to be installed on the target system.
- **Pros:** Smaller file size (~500KB-5MB)
- **Cons:** Users must have the correct .NET version installed

### PublishSingleFile
Bundles the application into a single executable (CLI only).
- Makes distribution easier
- Slightly slower first startup (extracts to temp directory)
- Not recommended for Uno apps with resources

### PublishTrimmed
Removes unused code from the published application.
- Significantly reduces file size
- Enabled by default in Release mode for Uno app
- May cause issues if reflection is used incorrectly

### ReadyToRun (R2R)
Pre-compiles the application for faster startup.
- Enabled by default in Release mode for Uno app
- Slightly larger file size
- Better startup performance

---

## Platform-Specific Notes

### macOS

- **Code Signing:** macOS executables may require signing for distribution. Unsigned apps will show security warnings.
- **Cross-compilation:** You can build macOS executables from Windows/Linux, but creating `.app` bundles typically requires macOS.
- **Architecture Detection:** Modern Macs with Apple Silicon can run x64 binaries through Rosetta 2, but ARM64 binaries offer better performance.

### Windows

- **Cross-compilation:** Windows executables can be built from macOS/Linux without issues.
- **Antivirus:** Self-contained executables may trigger antivirus warnings on first run.
- **Architecture:** Most users need x64. ARM64 is for Windows on ARM devices (Surface Pro X, etc.).

### Linux

While not currently configured, the Uno Platform with Skia renderer supports Linux. You can add Linux runtime identifiers:
- `linux-x64` - Most Linux distributions
- `linux-arm64` - ARM-based Linux (Raspberry Pi, etc.)

---

## Configuration Files

Both projects include `appsettings.json` files that are copied to the output directory:
- `src/Mentor.CLI/appsettings.json`
- `src/Mentor.Uno/Mentor.Uno/appsettings.json`

These files are included automatically in published applications and can be edited post-deployment for configuration changes.

---

## Quick Reference

### Recommended Builds for Distribution

**CLI:**
```bash
# Mac (Universal recommended for widest compatibility)
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# Windows
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

**Uno Desktop App:**
```bash
cd src/Mentor.Uno/Mentor.Uno

# Mac - Publish
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-x64

# Mac - Create .app bundle (run from repo root)
cd ../../../
./scripts/create-mac-app.sh osx-arm64
./scripts/create-mac-app.sh osx-x64

# Windows - Publish
cd src/Mentor.Uno/Mentor.Uno
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64

# Windows - Create distribution (run from repo root)
cd ../../../
./scripts/create-windows-dist.sh win-x64
# Or on Windows: .\scripts\create-windows-dist.ps1 -Arch win-x64
```

---

## Troubleshooting

### Build Fails with SDK Not Found
Ensure you have both .NET 8 and .NET 9 SDKs installed:
```bash
dotnet --list-sdks
```

### Output Directory Not Found
The publish directory is created during the build. If not found, check the build output for errors.

### macOS "Cannot be opened because the developer cannot be verified"
Run this command to allow the app:
```bash
xattr -cr /path/to/Mentor.app
```

Or right-click the app and select "Open" while holding the Control key.

### Large File Sizes
- Use `PublishTrimmed=true` to reduce size
- Consider framework-dependent deployment if you can ensure runtime is installed
- Trim aggressively with `<PublishTrimmed>true</PublishTrimmed>` and `<TrimMode>link</TrimMode>`

---

## CI/CD Integration

Example GitHub Actions workflow snippet for building all platforms:

```yaml
- name: Publish CLI
  run: |
    dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
    dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
    dotnet publish src/Mentor.CLI/Mentor.CLI.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

- name: Publish Uno App
  run: |
    cd src/Mentor.Uno/Mentor.Uno
    dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64
    dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-x64
    dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64

- name: Create Distribution Packages
  run: |
    # macOS .app bundles
    ./scripts/create-mac-app.sh osx-arm64
    ./scripts/create-mac-app.sh osx-x64
    
    # Windows distributions
    ./scripts/create-windows-dist.sh win-x64

- name: Upload Artifacts
  uses: actions/upload-artifact@v3
  with:
    name: Mentor-Distributions
    path: |
      dist/Mentor.app
      dist/Mentor-Windows-win-x64/
```

