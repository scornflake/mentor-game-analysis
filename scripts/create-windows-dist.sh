#!/bin/bash
# Script to create a Windows distribution folder from published Mentor.Uno output
# Usage: ./scripts/create-windows-dist.sh [win-x64|win-x86|win-arm64]

set -e

# Default to x64 if no argument provided
ARCH="${1:-win-x64}"

# Validate architecture
if [[ "$ARCH" != "win-x64" && "$ARCH" != "win-x86" && "$ARCH" != "win-arm64" ]]; then
    echo "Error: Invalid architecture. Use 'win-x64', 'win-x86', or 'win-arm64'"
    exit 1
fi

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$PROJECT_ROOT/src/Mentor.Uno/Mentor.Uno/bin/Release/net9.0-desktop/$ARCH/publish"
DIST_DIR="$PROJECT_ROOT/dist/Mentor-Windows-$ARCH"

echo "Creating Windows distribution for $ARCH..."

# Check if publish directory exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory not found at $PUBLISH_DIR"
    echo "Run: cd src/Mentor.Uno/Mentor.Uno && dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=$ARCH"
    exit 1
fi

# Create dist directory
mkdir -p "$PROJECT_ROOT/dist"

# Remove old distribution if it exists
if [ -d "$DIST_DIR" ]; then
    echo "Removing existing distribution..."
    rm -rf "$DIST_DIR"
fi

# Create distribution directory
echo "Creating distribution structure..."
mkdir -p "$DIST_DIR"

# Copy all published files
echo "Copying published files..."
cp -R "$PUBLISH_DIR/"* "$DIST_DIR/"

# Create a README for the distribution
cat > "$DIST_DIR/README.txt" << 'EOF'
Mentor - Game Analysis
======================

To run the application:
  Double-click: Mentor.exe

System Requirements:
- Windows 10 version 1809 or later
- No additional software installation required (self-contained)

For support or issues, please visit:
https://github.com/yourusername/mentor

EOF

echo ""
echo "✅ Windows distribution created successfully!"
echo "Location: $DIST_DIR"
echo ""
echo "Contents:"
echo "  Executable: Mentor.exe"
echo ""
echo "To run the app on Windows:"
echo "  $DIST_DIR/Mentor.exe"
echo ""
echo "To create a ZIP archive:"
echo "  cd $PROJECT_ROOT/dist"
echo "  zip -r Mentor-Windows-$ARCH.zip Mentor-Windows-$ARCH/"
echo ""

# Optionally create a ZIP file if zip command is available
if command -v zip &> /dev/null; then
    read -p "Create ZIP archive? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        cd "$PROJECT_ROOT/dist"
        ZIP_NAME="Mentor-Windows-$ARCH.zip"
        echo "Creating $ZIP_NAME..."
        zip -r -q "$ZIP_NAME" "Mentor-Windows-$ARCH/"
        echo "✅ ZIP archive created: dist/$ZIP_NAME"
    fi
fi

