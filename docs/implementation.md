# Implementation Guide: Game Screenshot Analysis System

## Overview

This document provides concrete implementation details for building the MVP: a C# core library with CLI client for analyzing game screenshots using LLMs.

## Technology Stack

- **Language**: C# / .NET 8+
- **Core**: .NET Class Library (cross-platform)
- **CLI**: System.CommandLine for argument parsing
- **Image Processing**: SixLabors.ImageSharp
- **LLM Integration**: 
  - **Primary Abstraction**: Microsoft.Extensions.AI (official Microsoft LLM abstraction)
  - **Alternative**: Semantic Kernel or individual provider SDKs
  - **Providers**: Perplexity (primary), OpenAI, Anthropic
- **Configuration**: Microsoft.Extensions.Configuration
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Serialization**: System.Text.Json with source generators

## Project Structure

```
mentor/
├── src/
│   ├── Mentor.Core/                    # Core Library (Reusable)
│   │   ├── Mentor.Core.csproj
│   │   ├── Models/
│   │   │   ├── Recommendation.cs       # Domain model
│   │   │   ├── RecommendationItem.cs
│   │   │   ├── AnalysisRequest.cs
│   │   │   └── Priority.cs
│   │   ├── Services/
│   │   │   ├── IAnalysisService.cs     # Service interface
│   │   │   ├── AnalysisService.cs      # Core orchestration
│   │   │   └── PromptBuilder.cs        # Prompt construction
│   │   ├── LLM/
│   │   │   └── LLMProviderAdapter.cs   # Thin wrapper over MS.Extensions.AI
│   │   ├── ImageProcessing/
│   │   │   └── ImageProcessor.cs       # ImageSharp utilities
│   │   ├── Configuration/
│   │   │   └── MentorConfig.cs         # Configuration models
│   │   └── Exceptions/
│   │       └── AnalysisException.cs
│   │
│   └── Mentor.CLI/                     # CLI Client
│       ├── Mentor.CLI.csproj
│       ├── Program.cs                  # Entry point + DI setup
│       ├── Commands/
│       │   └── AnalyzeCommand.cs       # Command implementation
│       ├── Output/
│       │   └── JsonFormatter.cs        # JSON output formatting
│       └── appsettings.json            # Configuration file
│
├── tests/
│   ├── Mentor.Core.Tests/              # Core library tests
│   │   ├── Mentor.Core.Tests.csproj
│   │   ├── Services/
│   │   │   └── AnalysisServiceTests.cs
│   │   └── Mocks/
│   │       └── MockLLMProvider.cs
│   │
│   └── Mentor.CLI.Tests/               # CLI integration tests
│       ├── Mentor.CLI.Tests.csproj
│       └── CommandTests.cs
│
├── docs/
│   ├── AGENTS.md
│   ├── architecture.md
│   ├── implementation.md
│   └── mvp_goal.md
│
├── .gitignore
├── Mentor.sln                          # Solution file
└── README.md
```

## Core Interfaces

### LLM Abstraction Strategy

**Use existing packages instead of custom abstractions:**

#### Microsoft.Extensions.AI (Recommended)
- Official Microsoft abstraction for AI services
- Supports OpenAI, Anthropic, and custom providers
- Built-in support for chat completion with images

Example usage:
```csharp
using Microsoft.Extensions.AI;

IChatClient chatClient = /* configure provider */;
var response = await chatClient.CompleteAsync(
    new ChatMessage[]
    {
        new(ChatRole.User, 
            [
                new TextContent(prompt),
                new ImageContent(imageBytes, "image/png")
            ])
    }
);
```

#### Alternative: Semantic Kernel
- More feature-rich but heavier
- Good for complex agent scenarios
- Consider for future extensions

### IAnalysisService Interface

```csharp
namespace Mentor.Core.Services;

public interface IAnalysisService
{
    /// <summary>
    /// Main entry point for screenshot analysis
    /// </summary>
    Task<Recommendation> AnalyzeAsync(
        AnalysisRequest request,
        CancellationToken cancellationToken = default
    );
}
```

**Implementation Note**: AnalysisService will use `IChatClient` from Microsoft.Extensions.AI internally, avoiding custom provider abstractions.

## Data Models

### Core Domain Model

```csharp
namespace Mentor.Core.Models;

public class Recommendation
{
    public string Analysis { get; set; }
    public string Summary { get; set; }
    public List<RecommendationItem> Recommendations { get; set; }
    public double Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string ProviderUsed { get; set; }
}

public class RecommendationItem
{
    public Priority Priority { get; set; }
    public string Action { get; set; }
    public string Reasoning { get; set; }
    public string Context { get; set; }
}

public enum Priority
{
    High,
    Medium,
    Low
}

public class AnalysisRequest
{
    public byte[] ImageData { get; set; }
    public string Prompt { get; set; }
    public string? PreferredProvider { get; set; }
}
```

## Dependency Injection Setup

```csharp
// In Program.cs (CLI)
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // Configuration
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();
    
    services.AddSingleton<IConfiguration>(configuration);
    
    // LLM Provider (using Microsoft.Extensions.AI)
    var providerName = configuration["LLM:Provider"] ?? "perplexity";
    var apiKey = configuration[$"LLM:{providerName}:ApiKey"] 
                 ?? Environment.GetEnvironmentVariable($"{providerName.ToUpper()}_API_KEY");
    
    // Configure IChatClient based on provider
    services.AddSingleton<IChatClient>(sp =>
    {
        return providerName.ToLower() switch
        {
            "perplexity" => new OpenAIChatClient(apiKey, new() 
                { Endpoint = new Uri("https://api.perplexity.ai") }),
            "openai" => new OpenAIChatClient(apiKey),
            "anthropic" => new AnthropicChatClient(apiKey),
            _ => throw new ArgumentException($"Unknown provider: {providerName}")
        };
    });
    
    // Core Services
    services.AddSingleton<IAnalysisService, AnalysisService>();
    
    return services.BuildServiceProvider();
}
```

**Note**: Exact client names depend on the chosen package. Microsoft.Extensions.AI and provider-specific packages provide these implementations.

## Configuration

### appsettings.json (Mentor.CLI)

```json
{
  "LLM": {
    "Provider": "perplexity",
    "perplexity": {
      "Model": "llama-3.1-sonar-large-128k-online",
      "MaxTokens": 2000,
      "Temperature": 0.7
    },
    "openai": {
      "Model": "gpt-4o",
      "MaxTokens": 2000,
      "Temperature": 0.7
    },
    "anthropic": {
      "Model": "claude-3-5-sonnet-20241022",
      "MaxTokens": 2000,
      "Temperature": 0.7
    }
  },
  "Analysis": {
    "TimeoutSeconds": 30,
    "MaxImageSizeMB": 10
  }
}
```

### Environment Variables

- `PERPLEXITY_API_KEY`: Perplexity API key (primary)
- `OPENAI_API_KEY`: OpenAI API key (optional)
- `ANTHROPIC_API_KEY`: Anthropic API key (optional)
- `LLM__PROVIDER`: Override provider (e.g., "openai")
- Command-line: `--provider perplexity` overrides all

## Package Dependencies

### Mentor.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- LLM Abstraction -->
    <PackageReference Include="Microsoft.Extensions.AI" Version="8.x" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="8.x" />
    <PackageReference Include="Microsoft.Extensions.AI.Anthropic" Version="8.x" />
    
    <!-- Image Processing -->
    <PackageReference Include="SixLabors.ImageSharp" Version="3.x" />
    
    <!-- Configuration & DI -->
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.x" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.x" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="8.x" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.x" />
    
    <!-- JSON Serialization -->
    <PackageReference Include="System.Text.Json" Version="8.x" />
  </ItemGroup>
</Project>
```

### Mentor.CLI.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mentor.Core\Mentor.Core.csproj" />
    
    <!-- CLI Framework -->
    <PackageReference Include="System.CommandLine" Version="2.x" />
  </ItemGroup>
</Project>
```

**Note**: For Perplexity, you may need to use the OpenAI-compatible client with custom base URL, as Microsoft.Extensions.AI may not have a dedicated Perplexity package yet.

## LLM Provider Implementation

### Perplexity Integration

Perplexity API is OpenAI-compatible, so use the OpenAI client with custom base URL:

```csharp
var client = new OpenAIChatClient("your-api-key", new OpenAIClientOptions
{
    Endpoint = new Uri("https://api.perplexity.ai")
});
```

### Multi-Provider Support

Use a factory or configuration to instantiate the correct client:

```csharp
IChatClient CreateChatClient(string provider, string apiKey)
{
    return provider.ToLower() switch
    {
        "perplexity" => new OpenAIChatClient(apiKey, new() 
            { Endpoint = new Uri("https://api.perplexity.ai") }),
        "openai" => new OpenAIChatClient(apiKey),
        "anthropic" => new AnthropicChatClient(apiKey),
        _ => throw new ArgumentException($"Unknown provider: {provider}")
    };
}
```

## Deployment

### CLI Application

```bash
# Run as command-line tool
dotnet run --project src/Mentor.CLI -- \
    --image screenshot.png \
    --prompt "What should I improve?" \
    --provider perplexity

# Publish as self-contained executable
dotnet publish -c Release -r osx-arm64 --self-contained
dotnet publish -c Release -r win-x64 --self-contained

# Example output (JSON to stdout)
{
  "analysis": "...",
  "summary": "...",
  "recommendations": [...],
  "confidence": 0.85
}
```

### Using Core Library Directly

```csharp
// Reference Mentor.Core from another project
using Mentor.Core.Services;
using Mentor.Core.Models;

var analysisService = serviceProvider.GetRequiredService<IAnalysisService>();
var result = await analysisService.AnalyzeAsync(new AnalysisRequest
{
    ImageData = imageBytes,
    Prompt = "What should I improve?"
});
```

## Getting Started

### Initial Setup

```bash
# Create solution
dotnet new sln -n Mentor

# Create projects
dotnet new classlib -n Mentor.Core -o src/Mentor.Core -f net8.0
dotnet new console -n Mentor.CLI -o src/Mentor.CLI -f net8.0

# Create test projects
dotnet new xunit -n Mentor.Core.Tests -o tests/Mentor.Core.Tests -f net8.0

# Add to solution
dotnet sln add src/Mentor.Core/Mentor.Core.csproj
dotnet sln add src/Mentor.CLI/Mentor.CLI.csproj
dotnet sln add tests/Mentor.Core.Tests/Mentor.Core.Tests.csproj

# Set up project references
dotnet add src/Mentor.CLI reference src/Mentor.Core
dotnet add tests/Mentor.Core.Tests reference src/Mentor.Core
```

### Add Packages

```bash
# Add packages to Core
cd src/Mentor.Core
dotnet add package Microsoft.Extensions.AI
dotnet add package SixLabors.ImageSharp
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package System.Text.Json
cd ../..

# Add packages to CLI
cd src/Mentor.CLI
dotnet add package System.CommandLine --prerelease
cd ../..

# Restore and build
dotnet restore
dotnet build
```

### Run the CLI

```bash
# Display help
dotnet run --project src/Mentor.CLI -- --help

# Analyze a screenshot
dotnet run --project src/Mentor.CLI -- \
    --image path/to/screenshot.png \
    --prompt "What should I do next in this game?"
```

## Development Workflow

1. **Phase 1: Core Models** - Implement domain models (Recommendation, AnalysisRequest, etc.)
2. **Phase 2: LLM Integration** - Set up Microsoft.Extensions.AI with Perplexity
3. **Phase 3: Analysis Service** - Build core orchestration logic
4. **Phase 4: CLI** - Create command-line interface with System.CommandLine
5. **Phase 5: Testing** - End-to-end tests with real screenshots
6. **Phase 6: Multi-Provider** - Add OpenAI and Anthropic support
7. **Phase 7: Polish** - Error handling, logging, documentation

## Testing

### Unit Tests

```csharp
// Example: Test AnalysisService with mock IChatClient
public class AnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ReturnsRecommendation()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient
            .Setup(x => x.CompleteAsync(It.IsAny<IList<ChatMessage>>(), null, default))
            .ReturnsAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, "...")));
        
        var service = new AnalysisService(mockChatClient.Object);
        
        // Act
        var result = await service.AnalyzeAsync(new AnalysisRequest
        {
            ImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            Prompt = "Test prompt"
        });
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Analysis);
    }
}
```

### Integration Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Mentor.Core.Tests
```

## Future Implementations

**Not in MVP, but documented for reference:**

### HTTP API (Mentor.API)
- ASP.NET Core Web API
- REST endpoints for analysis
- Multi-part form upload for images

### Desktop UI (Mentor.UI)
- Avalonia UI framework
- MVVM pattern with ReactiveUI
- Cross-platform (macOS, Windows, Linux)

### Additional Features
- Plugin system for custom providers
- Local LLM support (Ollama, LM Studio)
- Result caching
- Batch processing

