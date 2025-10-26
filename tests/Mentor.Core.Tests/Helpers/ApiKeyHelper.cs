namespace Mentor.Core.Tests.Helpers;

/// <summary>
/// Helper class for retrieving API keys from environment variables or configuration files.
/// </summary>
public static class ApiKeyHelper
{
    /// <summary>
    /// Gets the OpenAI API key from environment variable or appsettings.Development.json.
    /// </summary>
    /// <returns>The API key if found, otherwise null.</returns>
    public static string? GetOpenAIApiKey()
    {
        // Check environment variable first
        var envKey = Environment.GetEnvironmentVariable("LLM__OpenAI__ApiKey");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey;
        }

        // Check for appsettings.Development.json in the CLI project
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = FindProjectRoot(baseDir);
        
        if (projectRoot != null)
        {
            var devSettingsPath = Path.Combine(projectRoot, "src", "Mentor.CLI", "appsettings.Development.json");
            if (File.Exists(devSettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(devSettingsPath);
                    // Simple JSON parsing to extract the API key
                    if (json.Contains("ApiKey"))
                    {
                        var lines = json.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains("\"ApiKey\""))
                            {
                                var parts = line.Split(':');
                                if (parts.Length > 1)
                                {
                                    var key = parts[1].Trim().Trim('"').Trim(',').Trim();
                                    if (!string.IsNullOrWhiteSpace(key) && key != "your-api-key-here")
                                    {
                                        return key;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // If we can't read the file, just return null
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the project root directory by searching for Mentor.sln.
    /// </summary>
    public static string? FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Mentor.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}

