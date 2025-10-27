# MentorUI - Avalonia Desktop Application

A cross-platform desktop GUI for the Mentor screenshot analysis tool, built with Avalonia UI.

## Features

- **Image Selection**: Browse and select game screenshots from your file system
- **Custom Prompts**: Enter custom analysis prompts or use the default "What should I do next?"
- **Provider Selection**: Choose between OpenAI, Perplexity, or local LLM providers
- **Visual Results**: View analysis results with formatted recommendations and priority badges
- **Cross-Platform**: Runs on Windows, macOS, and Linux

## Running the Application

### From the command line:

```bash
# From the repository root
dotnet run --project src/MentorUI/MentorUI.csproj

# Or from the MentorUI directory
cd src/MentorUI
dotnet run
```

### Publishing the Application:

```bash
# Create a self-contained executable
dotnet publish src/MentorUI/MentorUI.csproj -c Release -r osx-arm64 --self-contained

# Or for other platforms:
# Windows: -r win-x64
# Linux: -r linux-x64
# macOS Intel: -r osx-x64
```

## Configuration

The application uses the same configuration as the CLI. Configure your API keys in:

1. `appsettings.Development.json` (not committed to git)
2. Environment variables

Example `appsettings.Development.json`:

```json
{
  "LLM": {
    "Providers": {
      "openai": {
        "ApiKey": "your-openai-key"
      },
      "perplexity": {
        "ApiKey": "your-perplexity-key"
      }
    }
  }
}
```

## Architecture

- **MVVM Pattern**: Uses Model-View-ViewModel architecture with CommunityToolkit.Mvvm
- **Dependency Injection**: Services are registered and injected using Microsoft.Extensions.DependencyInjection
- **Shared Core**: Uses the same `Mentor.Core` library as the CLI for consistency
- **Testable**: ViewModels are fully unit tested

## Project Structure

```
MentorUI/
├── ViewModels/          # ViewModel classes
│   └── MainWindowViewModel.cs
├── Views/               # View (XAML) files
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── Converters/          # Value converters for data binding
│   └── PriorityColorConverter.cs
├── App.axaml            # Application resources
├── App.axaml.cs         # Application startup and DI configuration
├── Program.cs           # Entry point
└── appsettings.json     # Configuration
```

## Testing

Tests are located in `tests/MentorUI.Tests/` and use xUnit with Moq.

Run tests:

```bash
dotnet test tests/MentorUI.Tests/MentorUI.Tests.csproj
```

## Dependencies

- **Avalonia 11.2.2**: Cross-platform UI framework
- **CommunityToolkit.Mvvm 8.3.2**: MVVM helpers and source generators
- **Mentor.Core**: Shared analysis service and models
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Configuration**: Configuration management

## Development

The application follows SOLID principles and uses:

- Observable properties for automatic UI updates
- RelayCommands for user actions
- Proper separation of concerns between View and ViewModel
- Dependency injection for loose coupling

