# MVP Goal: Game Screenshot Analysis & Recommendation System

## Overview
Build a minimal viable product that analyzes game screenshots via LLM and returns structured improvement recommendations.

## Core Functionality

### Input
- **Image**: Game screenshot (common formats: PNG, JPEG, WebP)
- **Prompt**: Text query describing what to analyze/improve (e.g., "How can I improve my character build?", "What should I do next?")

### Output
- **Structured Recommendation**: JSON object with:
  ```json
  {
    "analysis": "string",        // Readable analysis of what's shown in the screenshot
    "summary": "string",         // High-level summary of changes user should make
    "recommendations": [
      {
        "priority": "high|medium|low",  // Individual recommendation priority
        "action": "string",             // What to do
        "reasoning": "string",          // Why to do it
        "context": "string"             // What was observed in the screenshot
      }
    ],
    "confidence": "number"       // 0-1 score of analysis confidence
  }
  ```

## Technical Requirements

### LLM Abstraction Layer
Must implement an interface that:
1. Accepts image + text prompt
2. Returns structured recommendation object
3. Allows swapping LLM providers (OpenAI, Anthropic, local models, etc.) without changing application code

**Interface Definition:**
```csharp
public interface ILLMProvider
{
    Task<Recommendation> AnalyzeScreenshotAsync(byte[] image, string prompt);
}
```

### MVP Constraints

#### In Scope
- Single screenshot analysis
- One game type for initial testing
- CLI or simple API interface
- Basic error handling
- Provider configuration via environment variables

#### Out of Scope
- Multi-image analysis
- Video/stream analysis
- User authentication
- Database/history storage
- Web UI
- Real-time game integration
- Multiple simultaneous games

## Success Criteria
1. Successfully analyze a game screenshot and return structured JSON
2. Swap between 2+ LLM providers without code changes
3. Recommendations are relevant and actionable
4. Process completes in <30 seconds

## Technical Stack Suggestions
- **Language**: C# / .NET 8+
- **Image Processing**: System.Drawing.Common or ImageSharp
- **LLM SDKs**: 
  - Primary: Perplexity API (via HttpClient)
  - Future: OpenAI SDK, Anthropic SDK
- **Configuration**: Microsoft.Extensions.Configuration (appsettings.json + environment variables)
- **Validation**: System.Text.Json with source generators for structured outputs
- **HTTP Client**: System.Net.Http.HttpClient with IHttpClientFactory

## Example Usage
```bash
# CLI interface
dotnet run --project Mentor.CLI -- --image screenshot.png --prompt "What should I improve?"

# Or as executable
./Mentor.CLI --image screenshot.png --prompt "What should I improve?"

# Returns structured JSON to stdout
```

## Implementation Phases
1. **Phase 1**: LLM interface definition + Perplexity provider implementation
2. **Phase 2**: Image ingestion + prompt composition
3. **Phase 3**: Structured output parsing + validation
4. **Phase 4**: Perplexity provider implementations to validate abstraction
5. **Phase 5**: Error handling + testing

## Non-Functional Requirements
- **Modularity**: LLM provider is a pluggable component
- **Testability**: Mock providers for testing without API calls
- **Cost-Awareness**: Log token usage per request
- **Clarity**: Clear error messages when analysis fails

