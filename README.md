# Mentor - Game Screenshot Analysis System

A C# .NET 8 application for analyzing game screenshots using LLMs to provide actionable recommendations.

## Project Status

✅ **MVP Foundation Complete** - Basic structure validated and working

## Project Structure

```
mentor/
├── src/
│   ├── Mentor.Core/          # Core library with domain models and services
│   │   ├── Models/           # Domain models (Recommendation, AnalysisRequest, etc.)
│   │   └── Services/         # Analysis service interface and implementation
│   ├── Mentor.CLI/           # Command-line interface
│   └── MentorUI/             # Avalonia desktop GUI application
├── tests/
│   ├── Mentor.Core.Tests/    # Unit tests for Core library
│   └── MentorUI.Tests/       # Unit tests for UI ViewModels
├── docs/                     # Documentation
└── Mentor.sln                # Solution file
```

## Current Implementation

### Domain Models
- **Priority**: Enum for recommendation priority levels (High, Medium, Low)
- **RecommendationItem**: Individual recommendation with action, reasoning, and context
- **Recommendation**: Complete analysis result with multiple recommendations
- **AnalysisRequest**: Request payload with image data and prompt

### Services
- **IAnalysisService**: Interface for screenshot analysis
- **AnalysisService**: Stub implementation returning hardcoded recommendations (for validation)

### Interfaces

#### CLI
- Command-line interface with argument parsing
- Dependency injection setup
- Integration with Core library
- Support for multiple LLM providers (OpenAI, Perplexity, local)

#### Desktop UI (MentorUI)
- Cross-platform desktop application built with Avalonia
- MVVM architecture using CommunityToolkit.Mvvm
- Visual file picker for screenshots
- Real-time analysis results display
- Priority-based recommendation formatting
- Runs on Windows, macOS, and Linux

## Building and Running

### Build the solution
```bash
dotnet build
```

### Run tests
```bash
dotnet test
```

### Run the CLI
```bash
# Show help
dotnet run --project src/Mentor.CLI -- --help

# Analyze a screenshot
dotnet run --project src/Mentor.CLI -- \
    --image path/to/screenshot.png \
    --prompt "What should I do next?" \
    --provider perplexity
```

### Run the Desktop UI
```bash
# Run the Avalonia UI application
dotnet run --project src/MentorUI

# Or publish as a standalone executable
dotnet publish src/MentorUI -c Release -r osx-arm64 --self-contained
```

## Requirements

- .NET 8 SDK
- macOS, Windows, or Linux

## Configuration

Both the CLI and Desktop UI use the same configuration system. Configure your API keys in:

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

## Features

- ✅ **Multi-provider LLM support**: OpenAI, Perplexity, local models
- ✅ **CLI Interface**: Command-line tool for automation
- ✅ **Desktop GUI**: Cross-platform Avalonia application
- ✅ **Structured output**: JSON-formatted recommendations with priorities
- ✅ **Web search integration**: Brave Search API support
- ✅ **Fully tested**: Comprehensive unit tests for core services and UI

## Next Steps

Potential future enhancements:

1. **History tracking**: Save and browse previous analyses
2. **Batch processing**: Analyze multiple screenshots at once
3. **Export options**: Save results as PDF, HTML, or markdown
4. **Comparison mode**: Compare recommendations across multiple screenshots
5. **Plugin system**: Support for game-specific analysis plugins

## Development Guidelines

- Follow SOLID principles
- Write tests first (TDD approach)
- Keep code tight and focused on the task at hand
- Always compile and test after changes

## License

[To be determined]

