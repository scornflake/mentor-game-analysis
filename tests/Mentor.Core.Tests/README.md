# Mentor.Core.Tests

This project contains unit tests and integration tests for the Mentor.Core library.

## Test Types

### Unit Tests
- Run quickly without external dependencies
- Always execute by default
- Examples: `AnalysisServiceTests`, `LLMProviderFactoryTests`

### Integration Tests
- Make real API calls to external services (Perplexity)
- Require explicit opt-in to run
- Located in `AnalysisServiceIntegrationTests`

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

