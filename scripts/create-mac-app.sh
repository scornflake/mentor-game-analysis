#!/bin/bash
# Script to create a macOS .app bundle from the published Mentor.Uno output
# Usage: ./scripts/create-mac-app.sh [osx-arm64|osx-x64]

set -e

# Default to arm64 if no argument provided
ARCH="${1:-osx-arm64}"

# Validate architecture
if [[ "$ARCH" != "osx-arm64" && "$ARCH" != "osx-x64" ]]; then
    echo "Error: Invalid architecture. Use 'osx-arm64' or 'osx-x64'"
    exit 1
fi

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$PROJECT_ROOT/src/Mentor.Uno/Mentor.Uno/bin/Release/net9.0-desktop/$ARCH/publish"
APP_NAME="Mentor"
APP_BUNDLE="$PROJECT_ROOT/dist/$APP_NAME.app"

echo "Creating macOS .app bundle for $ARCH..."

# Check if publish directory exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory not found at $PUBLISH_DIR"
    echo "Run: cd src/Mentor.Uno/Mentor.Uno && dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=$ARCH"
    exit 1
fi

# Create dist directory
mkdir -p "$PROJECT_ROOT/dist"

# Remove old bundle if it exists
if [ -d "$APP_BUNDLE" ]; then
    echo "Removing existing app bundle..."
    rm -rf "$APP_BUNDLE"
fi

# Create app bundle structure
echo "Creating app bundle structure..."
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy all published files to MacOS directory
echo "Copying published files..."
cp -R "$PUBLISH_DIR/"* "$APP_BUNDLE/Contents/MacOS/"

# Create Info.plist
echo "Creating Info.plist..."
cat > "$APP_BUNDLE/Contents/Info.plist" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>Mentor</string>
    <key>CFBundleExecutable</key>
    <string>Mentor</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.mentor.uno</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>Mentor</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2024</string>
</dict>
</plist>
EOF

# Make the executable actually executable
echo "Setting executable permissions..."
chmod +x "$APP_BUNDLE/Contents/MacOS/Mentor"

# Copy icon if it exists (optional)
ICON_SOURCE="$PROJECT_ROOT/src/Mentor.Uno/Mentor.Uno/Assets/Icons/iconapp.png"
if [ -f "$ICON_SOURCE" ]; then
    echo "Icon found, but .icns conversion requires iconutil/sips (skipping for now)"
    # To convert PNG to ICNS properly, you'd need:
    # mkdir MyIcon.iconset
    # sips -z 16 16 icon.png --out MyIcon.iconset/icon_16x16.png
    # ... (multiple sizes)
    # iconutil -c icns MyIcon.iconset
fi

echo ""
echo "✅ macOS .app bundle created successfully!"
echo "Location: $APP_BUNDLE"
echo ""
echo "Contents:"
echo "  Executable: Mentor"
echo ""
echo "To run the app:"
echo "  open \"$APP_BUNDLE\""
echo ""
echo "To remove quarantine attribute (if needed):"
echo "  xattr -cr \"$APP_BUNDLE\""
echo ""
echo "Note: The app is unsigned. For distribution, you should sign it with:"
echo "  codesign --force --deep --sign - \"$APP_BUNDLE\""

