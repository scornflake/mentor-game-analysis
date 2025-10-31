# Mentor.Core.Tests

This project contains unit tests and integration tests for the Mentor.Core library.

## Configuring API Keys for Tests

Tests that make real API calls (like web search integration tests) require API keys. You have three options to configure them:

### Option 1: User Secrets (Recommended for Local Development)

User secrets are stored outside your project directory and never get checked into source control:

```bash
# Initialize user secrets (already done if UserSecretsId exists in .csproj)
dotnet user-secrets init --project tests/Mentor.Core.Tests

# Set your Brave Search API key
dotnet user-secrets set "BraveSearch:ApiKey" "YOUR_ACTUAL_API_KEY" --project tests/Mentor.Core.Tests

# Set your Tavily Search API key
dotnet user-secrets set "TavilySearch:ApiKey" "YOUR_ACTUAL_API_KEY" --project tests/Mentor.Core.Tests

# List configured secrets
dotnet user-secrets list --project tests/Mentor.Core.Tests
```

### Option 2: appsettings.Development.json

Create `tests/Mentor.Core.Tests/appsettings.Development.json` (already in `.gitignore`):

```json
{
  "BraveSearch": {
    "ApiKey": "YOUR_ACTUAL_API_KEY",
    "BaseUrl": "https://api.search.brave.com/res/v1",
    "Timeout": 30
  },
  "TavilySearch": {
    "ApiKey": "YOUR_ACTUAL_API_KEY",
    "BaseUrl": "https://api.tavily.com",
    "Timeout": 30
  }
}
```

### Option 3: Environment Variables

```bash
# Set environment variables
export BRAVE_SEARCH_API_KEY="YOUR_ACTUAL_API_KEY"
export TAVILY_API_KEY="YOUR_ACTUAL_API_KEY"

# Or on Windows PowerShell
$env:BRAVE_SEARCH_API_KEY="YOUR_ACTUAL_API_KEY"
$env:TAVILY_API_KEY="YOUR_ACTUAL_API_KEY"
```

**Configuration Priority**: User Secrets → appsettings.Development.json → Environment Variables

## Test Types

### Unit Tests
- Run quickly without external dependencies
- Always execute by default
- Examples: `AnalysisServiceTests`, `LLMProviderFactoryTests`

### Integration Tests
- Make real API calls to external services (Perplexity, Brave Search, Tavily Search)
- Skip gracefully if API keys are not configured
- Located in `AnalysisServiceIntegrationTests`, `BraveWebSearchTests`, `TavilyWebSearchTests`, and `WebsearchIntegrationTest`

## Running Tests

### Run All Unit Tests (Fast)
```bash
dotnet test
```

This will run all unit tests and skip integration tests, completing in under a second.

### Run Integration Tests (Requires API Key)

Integration tests require two things:
1. **API Key**: Set in `src/Mentor.CLI/appsettings.Development.json` or via `LLM__OpenAI__ApiKey` environment variable
2. **Opt-In Flag**: Set `ENABLE_INTEGRATION_TESTS=true` environment variable

**From Terminal:**
```bash
# Run just integration tests
ENABLE_INTEGRATION_TESTS=true dotnet test --filter "FullyQualifiedName~AnalysisServiceIntegrationTests"

# Run all tests including integration tests
ENABLE_INTEGRATION_TESTS=true dotnet test
```

**From Your IDE:**

**Rider:**
1. Go to `Run` → `Edit Configurations...`
2. Select your test configuration
3. Add environment variable: `ENABLE_INTEGRATION_TESTS=true`
4. Apply and run tests

**Visual Studio:**
1. Go to `Test` → `Configure Run Settings` → `Select Solution Wide runsettings File`
2. Create a `.runsettings` file with:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <RunSettings>
     <RunConfiguration>
       <EnvironmentVariables>
         <ENABLE_INTEGRATION_TESTS>true</ENABLE_INTEGRATION_TESTS>
       </EnvironmentVariables>
     </RunConfiguration>
   </RunSettings>
   ```

**VS Code:**
1. Add to your `.vscode/settings.json`:
   ```json
   {
     "dotnet-test-explorer.testArguments": "",
     "terminal.integrated.env.osx": {
       "ENABLE_INTEGRATION_TESTS": "true"
     },
     "terminal.integrated.env.linux": {
       "ENABLE_INTEGRATION_TESTS": "true"
     },
     "terminal.integrated.env.windows": {
       "ENABLE_INTEGRATION_TESTS": "true"
     }
   }
   ```

## Integration Test Details

Integration tests in `AnalysisServiceIntegrationTests`:
- Make real API calls to Perplexity
- Use test images from `tests/media/`
- Log detailed output including API responses
- Take ~25-30 seconds each to complete

### What They Test:
1. **AnalyzeAsync_WithRealImage_ReturnsValidRecommendation** - Verifies complete API flow and response structure
2. **AnalyzeAsync_WithRealImage_ParsesRecommendationsCorrectly** - Validates recommendation parsing logic
3. **AnalyzeAsync_WithTimeout_CompletesWithinReasonableTime** - Ensures API calls complete within timeout

## Why This Design?

Having integration tests opt-in via environment variable allows:
- ✅ Fast test runs by default in IDE (unit tests only)
- ✅ No need to constantly edit `appsettings.Development.json`
- ✅ Easy to run integration tests when needed
- ✅ CI/CD can selectively run integration tests
- ✅ Developers can keep real API keys in config files safely

