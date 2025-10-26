# OpenAI-Compatible Provider Setup (Perplexity)

This document explains how to configure and use OpenAI-compatible providers (like Perplexity AI) with the Mentor CLI.

## Configuration

### 1. Get a Perplexity API Key

Sign up for a Perplexity API key at [https://www.perplexity.ai/](https://www.perplexity.ai/)

### 2. Configure the API Key

You have three options for setting your API key:

#### Option A: Development Configuration File (Recommended for local development)

Edit `src/Mentor.CLI/appsettings.Development.json`:

```json
{
  "LLM": {
    "OpenAI": {
      "ApiKey": "your-actual-api-key-here"
    }
  }
}
```

**Note:** This file is already in `.gitignore` to prevent accidental commits of your API key.

#### Option B: Environment Variable (Recommended for production)

Set the environment variable:

```bash
export LLM__OpenAI__ApiKey="your-actual-api-key-here"
```

Or on Windows (PowerShell):

```powershell
$env:LLM__OpenAI__ApiKey="your-actual-api-key-here"
```

#### Option C: Base Configuration File (Not recommended - for reference only)

The base configuration is in `src/Mentor.CLI/appsettings.json`:

```json
{
  "LLM": {
    "DefaultProvider": "openai",
    "OpenAI": {
      "ApiKey": "",
      "Model": "sonar",
      "BaseUrl": "https://api.perplexity.ai",
      "Timeout": 60
    }
  }
}
```

## Usage

### Basic Usage

Analyze a game screenshot:

```bash
dotnet run --project src/Mentor.CLI -- --image path/to/screenshot.png
```

### With Custom Prompt

```bash
dotnet run --project src/Mentor.CLI -- --image path/to/screenshot.png --prompt "What weapons should I use?"
```

### Specify Provider (defaults to openai)

```bash
dotnet run --project src/Mentor.CLI -- --image path/to/screenshot.png --provider openai
```

### Help

```bash
dotnet run --project src/Mentor.CLI -- --help
```

## Testing

### Run Unit Tests

```bash
dotnet test
```

### Integration Test (requires valid API key)

To test the actual Perplexity integration:

1. Set up your API key using one of the methods above
2. Run the CLI with a test image:

```bash
dotnet run --project src/Mentor.CLI -- --image tests/media/phantasma\ rad\ build.png --prompt "Analyze this Warframe build"
```

## Troubleshooting

### "OpenAI-compatible API key is not configured"

Make sure you've set the API key using one of the methods above. The application checks in this order:
1. Environment variable `LLM__OpenAI__ApiKey`
2. `appsettings.Development.json`
3. `appsettings.json`

### Configuration Priority

Configuration values are applied in this order (later values override earlier ones):
1. `appsettings.json`
2. `appsettings.Development.json` (if it exists)
3. Environment variables

## Model Information

- **Model**: `sonar`
- **Capabilities**: Text + Image (multimodal)
- **Base URL**: `https://api.perplexity.ai`
- **Timeout**: 60 seconds

## Architecture

The implementation uses:
- **OpenAI SDK** for API communication (compatible with OpenAI and Perplexity)
- **Factory Pattern** for provider selection
- **Dependency Injection** for configuration management
- **Options Pattern** for strongly-typed configuration

The configuration uses "OpenAI" naming since we're using the OpenAI SDK. Any OpenAI-compatible endpoint (like Perplexity) can be configured by setting the `BaseUrl` property.

See `docs/architecture.md` for more details.

