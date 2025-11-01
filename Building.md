# Building and Development Guide

This guide covers building, running, and developing the Mentor application.

## Requirements

- **.NET 8 SDK** - Required for Mentor.Core and Mentor.CLI
- **.NET 9 SDK** - Required for Mentor.Uno (desktop GUI)
- **Windows, macOS, or Linux**

Verify installations:
```bash
dotnet --list-sdks
```

## Quick Start

### Clone and Run

```bash
# Clone the repository
git clone https://github.com/scornflake/mentor-game-analysis.git
cd mentor-game-analysis

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the desktop UI
dotnet run --project src/Mentor.Uno/Mentor.Uno
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

## Project Structure

```
mentor/
├── src/
│   ├── Mentor.Core/          # Core library with domain models and services
│   │   ├── Configuration/    # Configuration models
│   │   ├── Data/            # Database entities
│   │   ├── Interfaces/      # Service interfaces
│   │   ├── Models/          # Domain models (Recommendation, AnalysisRequest, etc.)
│   │   ├── Services/        # Core services (LLM client, configuration, export)
│   │   └── Tools/           # Analysis tools and web search implementations
│   ├── Mentor.CLI/          # Command-line interface
│   └── Mentor.Uno/          # Uno Platform desktop GUI application
│       └── Mentor.Uno/      # Main UI project
├── tests/
│   ├── Mentor.Core.Tests/   # Unit tests for Core library
│   └── MentorUI.Tests/      # Unit tests for UI ViewModels
├── docs/                    # Documentation
│   ├── build.md            # Detailed build instructions for distributions
│   ├── architecture.md     # Technical architecture
│   └── implementation.md   # Implementation details
├── scripts/                 # Build and distribution scripts
└── Mentor.sln              # Solution file
```

## Configuration

### API Keys Setup

Configure your API keys through the Settings UI or configuration files.

#### Option 1: Settings UI (Recommended)

1. Launch the application
2. Click the Settings button
3. Enter your API keys for:
   - LLM Providers (OpenAI, Perplexity, Claude, etc.)
   - Search Tools (Brave Search, Tavily)

Settings are stored locally in your user profile.

#### Option 2: Configuration Files

Create `appsettings.Development.json` (not committed to git):

**For Mentor.CLI:**
```json
{
  "LLM": {
    "Providers": {
      "openai": {
        "ApiKey": "your-openai-key"
      },
      "perplexity": {
        "ApiKey": "your-perplexity-key"
      },
      "anthropic": {
        "ApiKey": "your-anthropic-key"
      }
    }
  },
  "Tools": {
    "BraveSearch": {
      "ApiKey": "your-brave-key"
    },
    "TavilySearch": {
      "ApiKey": "your-tavily-key"
    }
  }
}
```

**For Mentor.Uno:**

Configuration is managed through the UI and stored in a local database. Manual configuration via files is not necessary.

#### Option 3: Environment Variables

```bash
# Linux/macOS
export LLM__OpenAI__ApiKey="your-openai-key"
export LLM__Perplexity__ApiKey="your-perplexity-key"

# Windows PowerShell
$env:LLM__OpenAI__ApiKey="your-openai-key"
$env:LLM__Perplexity__ApiKey="your-perplexity-key"
```

## Building for Distribution

### Desktop Application (Mentor.Uno)

The desktop application can be built for Windows, macOS, and Linux.

#### Quick Build Commands

```bash
cd src/Mentor.Uno/Mentor.Uno

# Windows (64-bit)
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=win-x64

# macOS (Apple Silicon)
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-arm64

# macOS (Intel)
dotnet publish -c Release -f net9.0-desktop /p:PublishProfile=osx-x64
```

#### Create Distribution Packages

**For Windows:**
```powershell
# From repository root
.\scripts\create-windows-dist.ps1 -Arch win-x64
```

**For macOS:**
```bash
# From repository root
./scripts/create-mac-app.sh osx-arm64  # For Apple Silicon
./scripts/create-mac-app.sh osx-x64    # For Intel
```

### Command-Line Interface (Mentor.CLI)

Build single-file executables:

```bash
# Windows
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj \
  -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS (Apple Silicon)
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj \
  -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS (Intel)
dotnet publish src/Mentor.CLI/Mentor.CLI.csproj \
  -c Release -r osx-x64 --self-contained \
  -p:PublishSingleFile=true -p:PublishTrimmed=true
```

For detailed build instructions including installers, code signing, and platform-specific notes, see [docs/build.md](docs/build.md).

## Automated Releases with GitHub Actions

The project includes a GitHub Actions workflow that automates the entire build, test, and release process for Windows.

### What the Workflow Does

The `Build and Release` workflow automatically:

1. **Builds** the solution in Release configuration
2. **Runs all tests** (workflow fails if any tests fail)
3. **Creates** a Windows distribution package (ZIP)
4. **Compiles** a Windows installer (EXE using Inno Setup)
5. **Generates** a changelog from commit messages
6. **Publishes** a GitHub release with both the installer and portable ZIP

### How to Trigger a Release

#### Manual Release (Current Setup)

1. Go to the GitHub repository: https://github.com/scornflake/mentor-game-analysis
2. Click the **Actions** tab at the top
3. Select **Build and Release** from the workflow list on the left
4. Click the **Run workflow** button (top right)
5. (Optional) Enter a version override (e.g., `1.0.50`) or leave blank for auto-versioning
6. (Optional) Check "Mark as pre-release" if this is a beta/preview release
7. Click **Run workflow** to start

The workflow takes approximately 5-10 minutes to complete.

#### Automatic Release (Future Options)

The workflow can be easily extended to trigger automatically. To enable:

**On Git Tag Push:**
Add to `.github/workflows/release.yml` line 4:
```yaml
on:
  push:
    tags:
      - 'v*.*.*'  # Triggers on tags like v1.0.50
  workflow_dispatch:
```

Then create a release by pushing a tag:
```bash
git tag v1.0.50
git push origin v1.0.50
```

**On Main Branch Push:**
Add to `.github/workflows/release.yml` line 4:
```yaml
on:
  push:
    branches:
      - main
  workflow_dispatch:
```

This will create a release every time changes are merged to main.

### Release Artifacts

Each release includes:

- **MentorSetup-{version}.exe** - Windows installer with setup wizard
- **Mentor-Windows-win-x64.zip** - Portable Windows distribution (extract and run)
- **Auto-generated changelog** - Categorized list of changes since last release

### Changelog Generation

The workflow automatically generates a changelog from commit messages using conventional commit format:

- `feat:` or `feature:` → 🚀 Features section
- `fix:` or `bug:` → 🐛 Fixes section
- `test:` → 🧪 Tests section
- `docs:` → 📝 Documentation section
- All other commits → 🔧 Other Changes section

**Example commit messages:**
```bash
git commit -m "feat: add support for Claude Opus 4"
git commit -m "fix: correct image upload validation"
git commit -m "docs: update API configuration guide"
```

If commits don't follow this format, they'll still be included in the changelog under "Other Changes".

### Versioning

Version numbers are automatically calculated from git commit count (defined in `Directory.Build.props`):
- Format: `Major.Minor.BuildNumber` (e.g., `1.0.203`)
- Major/Minor are set in `Directory.Build.props` (currently `1.0`)
- BuildNumber is the total git commit count
- Override by specifying a version when triggering the workflow manually

To change the major/minor version:
1. Edit `Directory.Build.props` and update `<VersionPrefix>1.1</VersionPrefix>`
2. Commit the change
3. All subsequent builds will use the new version prefix

## Development

### Architecture Overview

**Domain Models:**
- `Priority`: Enum for recommendation priority levels (High, Medium, Low)
- `RecommendationItem`: Individual recommendation with action, reasoning, and context
- `Recommendation`: Complete analysis result with multiple recommendations
- `AnalysisRequest`: Request payload with image data and prompt

**Core Services:**
- `IAnalysisService`: Interface for screenshot analysis
- `AnalysisService`: Base implementation with structured output parsing
- `LLMClient`: Client for interacting with various LLM providers
- `ConfigurationRepository`: Manages provider and tool configurations

**UI Architecture:**
- MVVM pattern using CommunityToolkit.Mvvm
- Uno Platform for cross-platform support
- View separation: Main page, Settings page, Analysis views

### Testing

#### Run Unit Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Mentor.Core.Tests

# With detailed output
dotnet test --verbosity detailed
```

#### Integration Tests

Some tests require API keys and are opt-in:

```bash
# Set API keys in appsettings.Development.json or user secrets
cd tests/Mentor.Core.Tests
dotnet user-secrets set "BraveSearch:ApiKey" "your-key"
dotnet user-secrets set "TavilySearch:ApiKey" "your-key"

# Run integration tests
ENABLE_INTEGRATION_TESTS=true dotnet test
```

See [tests/Mentor.Core.Tests/README.md](tests/Mentor.Core.Tests/README.md) for details.

### Development Guidelines

The project follows these principles:

- **SOLID principles** - Follow single responsibility, dependency inversion
- **Test-Driven Development** - Write tests first, then implementation
- **Keep code focused** - Don't over-engineer solutions
- **Compile and test** - Always verify changes with compilation and tests

### Adding New LLM Providers

1. Add provider configuration to `AIConfiguration.cs`
2. Update `LLMProviderFactory` to handle the new provider
3. Add provider option in UI (`SettingsPage.xaml`)
4. Add tests for the new provider

### Adding New Search Tools

1. Implement the search tool interface in `Mentor.Core/Tools/`
2. Add configuration model
3. Register in `ToolFactory.cs`
4. Add UI configuration in Settings page
5. Write tests for the new tool

## Troubleshooting

### Build Fails

**SDK Not Found:**
```bash
# Check installed SDKs
dotnet --list-sdks

# Ensure you have both .NET 8 and .NET 9
```

**NuGet Restore Fails:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

### Runtime Issues

**API Key Not Found:**
- Check Settings UI for proper configuration
- Verify `appsettings.Development.json` is in the correct location
- Check environment variables are set correctly

**macOS Security Warning:**
```bash
# Remove quarantine attribute
xattr -cr /path/to/Mentor.app
```

**Windows Antivirus Blocks:**
- Self-contained executables may trigger false positives
- Add exception in antivirus software

## Additional Resources

- **Detailed Build Instructions**: [docs/build.md](docs/build.md)
- **Architecture Documentation**: [docs/architecture.md](docs/architecture.md)
- **Implementation Notes**: [docs/implementation.md](docs/implementation.md)
- **Test Configuration**: [tests/Mentor.Core.Tests/README.md](tests/Mentor.Core.Tests/README.md)

## Contributing

When contributing:

1. Create a feature branch
2. Write tests first (TDD)
3. Implement the feature
4. Ensure all tests pass
5. Update documentation as needed
6. Submit a pull request

## License

[To be determined]

