# Mentor.Uno - Uno Platform UI

Cross-platform UI for the Mentor application built with Uno Platform, targeting Windows and macOS (desktop).

## Overview

This is an Uno Platform implementation of the Mentor screenshot analysis application. It provides the same functionality as `Mentor.UI` (Avalonia) but uses Uno Platform with standard WinUI styling.

## Platform Support

- ✅ **macOS** (net9.0-desktop) - Skia renderer
- ✅ **Windows** (net9.0-windows) - Native WinAppSDK/WinUI 3

## Requirements

- .NET 9 SDK
- macOS workload (for macOS builds): `dotnet workload install macos`

## Building

### Build for macOS
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet build /p:TargetFramework=net9.0-desktop
```

### Build for Windows
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet build /p:TargetFramework=net9.0-windows10.0.26100
```

### Build All Targets
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet build
```

## Running

### On macOS
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet run --framework net9.0-desktop
```

### On Windows
```bash
cd src/Mentor.Uno/Mentor.Uno
dotnet run --framework net9.0-windows10.0.26100
```

## Features

- Screenshot image selection with cross-platform file picker
- LLM provider selection (OpenAI, Perplexity, Local)
- Analysis prompt customization
- Real-time progress indication
- Formatted results display with:
  - Summary
  - Detailed analysis
  - Prioritized recommendations
- Error handling and display
- Standard Uno Platform / WinUI styling

## Architecture

### Dependencies
- **Mentor.Core** - Core analysis services and business logic
- **Uno.WinUI** - Uno Platform WinUI implementation
- **CommunityToolkit.Mvvm** - MVVM helpers
- **Microsoft.Extensions.*** - Dependency injection, configuration, and hosting
- **Serilog** - Logging

### Project Structure
```
Mentor.Uno/
├── App.xaml/.cs           - Application entry point with DI setup
├── MainPage.xaml/.cs      - Main UI page
├── MainPageViewModel.cs   - MVVM ViewModel
├── Converters/            - Value converters for data binding
├── appsettings.json       - Configuration
└── Platforms/Desktop/     - Desktop-specific code
```

### Dependency Injection

Services are configured in `App.xaml.cs` using `IHost`:
- `IAnalysisService` - Screenshot analysis service
- `ILLMProviderFactory` - LLM provider factory
- `IWebsearch` - Web search service  
- `MainPageViewModel` - Main page view model

Configuration is loaded from `appsettings.json` and `appsettings.Development.json`.

## Configuration

Update `appsettings.Development.json` with your API keys:

```json
{
  "LLM": {
    "Providers": {
      "openai": {
        "ApiKey": "your-openai-key",
        "ModelId": "gpt-4o"
      },
      "perplexity": {
        "ApiKey": "your-perplexity-key"
      }
    }
  },
  "BraveSearch": {
    "ApiKey": "your-brave-search-key"
  }
}
```

## Development

The project uses:
- **x:Bind** for compiled bindings (better performance)
- **ItemsRepeater** for efficient list rendering
- **Standard WinUI controls** and styling
- **Cross-platform file picker** using `Windows.Storage.Pickers`

## Comparison with Avalonia UI

| Feature | Mentor.UI (Avalonia) | Mentor.Uno (Uno Platform) |
|---------|----------------------|---------------------------|
| Framework | Avalonia UI | Uno Platform / WinUI |
| XAML Dialect | Avalonia XAML | WinUI/UWP XAML |
| macOS Support | ✅ | ✅ |
| Windows Support | ✅ | ✅ (Native WinUI 3) |
| Linux Support | ✅ | ❌ (not configured) |
| .NET Version | .NET 8 | .NET 9 |
| Styling | Custom/Fluent | Standard WinUI |

## Troubleshooting

### "SDK does not support .NET 9"
Install .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0

### "macOS workload not found"
Run: `sudo dotnet workload install macos`

### Missing API Keys
Configure your API keys in `appsettings.Development.json`

## Notes

- The Windows target requires Windows to build (uses native WinAppSDK)
- The macOS (desktop) target can be built on macOS
- Both targets share the same codebase with platform-specific compilation
- Uses Uno Platform 6.3+ which requires .NET 9
