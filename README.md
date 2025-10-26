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
│   └── Mentor.CLI/           # Command-line interface
├── tests/
│   └── Mentor.Core.Tests/    # Unit tests for Core library
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

### CLI
- Basic command-line interface with argument parsing
- Dependency injection setup
- Integration with Core library

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

# Analyze a screenshot (stub implementation)
dotnet run --project src/Mentor.CLI -- \
    --image path/to/screenshot.png \
    --prompt "What should I do next?"
```

## Requirements

- .NET 8 SDK
- macOS, Windows, or Linux

## Next Steps

The current implementation is a validated skeleton. Next phases will include:

1. **Image Processing**: Add actual image loading with SixLabors.ImageSharp
2. **LLM Integration**: Integrate Microsoft.Extensions.AI with real providers (Perplexity, OpenAI, Anthropic)
3. **Enhanced CLI**: Add more options and output formatting (JSON support)
4. **Configuration**: Add appsettings.json and environment variable support
5. **Error Handling**: Add proper exception handling and validation

## Development Guidelines

- Follow SOLID principles
- Write tests first (TDD approach)
- Keep code tight and focused on the task at hand
- Always compile and test after changes

## License

[To be determined]

