using Xunit;

namespace Mentor.Core.Tests.Helpers;

/// <summary>
/// Conditional fact that skips the test if integration tests are not explicitly enabled.
/// Requires ENABLE_INTEGRATION_TESTS=true environment variable to be set.
/// Also requires an OpenAI API key to be available.
/// </summary>
public class RequiresOpenAIKeyFactAttribute : FactAttribute
{
    public RequiresOpenAIKeyFactAttribute()
    {
        // First check if integration tests are explicitly enabled
        var enableIntegrationTests = Environment.GetEnvironmentVariable("ENABLE_INTEGRATION_TESTS");
        if (string.IsNullOrWhiteSpace(enableIntegrationTests) || 
            !bool.TryParse(enableIntegrationTests, out var enabled) || 
            !enabled)
        {
            Skip = "Integration tests disabled. Set ENABLE_INTEGRATION_TESTS=true to run these tests.";
            return;
        }
        
        // Then check if API key is available
        if (string.IsNullOrWhiteSpace(ApiKeyHelper.GetOpenAIApiKey()))
        {
            Skip = "OpenAI API key not available. Set LLM__OpenAI__ApiKey environment variable or configure appsettings.Development.json";
        }
    }
}

