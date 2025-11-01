# Screenshots Directory

This directory contains screenshots used in the project documentation.

## Required Screenshots

Please add the following screenshots to showcase the application:

### 1. main-analysis.png
**What to capture:** The main application window showing:
- Left panel: Image upload/preview area with a game screenshot loaded
- Center/Right panel: Input fields (game name, prompt)
- Provider selection dropdown
- "Analyze Screenshot" button
- Ideally showing the analysis in progress or ready state

**Recommended size:** 1920x1080 or similar widescreen resolution

### 2. analysis-results.png
**What to capture:** The results panel showing:
- A completed analysis with multiple recommendations
- Recommendations displayed with priority levels (High/Medium/Low)
- Each recommendation showing:
  - Priority badge/indicator
  - Action text
  - Reasoning explanation
  - Context information
- The image preview on the left side

**Recommended size:** 1920x1080 or similar widescreen resolution

### 3. settings-page.png
**What to capture:** The settings page showing:
- LLM Provider configuration section (with example API key fields visible but obscured)
- Search tool configuration (Brave Search, Tavily)
- Toggle visibility buttons for API keys
- Clean, organized layout of all configuration options

**Recommended size:** 1280x720 or larger

### 4. icon.png (optional)
The application icon/logo if one exists.

**Recommended size:** 512x512 pixels, PNG with transparency

## Screenshot Guidelines

- Use clean, representative examples
- Ensure text is readable
- Hide/obscure real API keys if visible
- Use light theme for consistency
- Crop to remove unnecessary desktop elements
- Save as PNG format for best quality
- Optimize file sizes (use tools like TinyPNG if needed)

## Taking Screenshots

### Windows
- **Full window:** Alt + PrtScn, then paste into image editor
- **Snipping Tool:** Win + Shift + S
- **Game Bar:** Win + Alt + PrtScn

### macOS
- **Full window:** Cmd + Shift + 4, then Space, then click window
- **Selection:** Cmd + Shift + 4, then select area
- **Screenshots saved to Desktop by default**

### Linux
- **gnome-screenshot:** Use Screenshot utility
- **flameshot:** Recommended for annotations
- **spectacle:** KDE screenshot tool

## Image Optimization

After capturing screenshots, consider optimizing them:

```bash
# Using ImageMagick (if installed)
convert input.png -quality 85 -resize 1920x1080 output.png

# Or use online tools:
# - tinypng.com
# - squoosh.app
```

## Current Status

- [ ] main-analysis.png - **NEEDED**
- [ ] analysis-results.png - **NEEDED**
- [ ] settings-page.png - **NEEDED**
- [ ] icon.png - Optional

