# Setup and Run Guide

Quick guide to get Mentor up and running in minutes.

## Installation

Download the latest release from the [Releases](https://github.com/scornflake/mentor-game-analysis/releases) page.

### Option 1: Windows Installer (.exe)

**Note:** The installer is not code-signed. If you're concerned about this, use the portable ZIP version instead.

1. Download `MentorSetup-{version}.exe`
2. Run the installer
3. Windows may show a SmartScreen warning - click "More info" then "Run anyway"
4. Follow the installation wizard
5. Launch Mentor from the Start Menu

### Option 2: Portable ZIP

1. Download `Mentor-Windows-win-x64.zip`
2. Extract to a folder of your choice
3. Run `Mentor.exe` from the extracted folder
4. No installation or admin rights required

## Initial Setup

### Quick Start (Recommended)

The easiest and cheapest way to get started:

1. **Get an API Key** from one of:
   - **Perplexity**: [perplexity.ai/settings/api](https://www.perplexity.ai/settings/api) (~2-4¢ per analysis)
   - **Anthropic Claude**: [console.anthropic.com](https://console.anthropic.com/) (~2-4¢ per analysis)

2. **Configure in Mentor**:
   - Launch Mentor
   - Click the **Settings** button (⚙) in the top-right
   - Scroll to your provider (Perplexity or Anthropic)
   - Enter your API key
   - Close settings (saves automatically)

3. **Done!** You're ready to analyze screenshots.

**Cost Note:** Both Perplexity and Claude are incredibly cheap for this use case - typically 2-4 cents per analysis. You can run dozens of analyses for a dollar.

### Alternative: Local LLM Setup

If you want to run a local LLM (slower, requires powerful hardware):

1. **Setup your local LLM server**:
   - Install [Ollama](https://ollama.ai/), [LM Studio](https://lmstudio.ai/), or similar
   - Configure it to provide an OpenAI-compatible API endpoint
   - Note: See [Local LLMs.md](Local%20LLMS.md) for caveats and limitations

2. **Get a Tavily API Key**:
   - Sign up at [tavily.com](https://tavily.com/)
   - Very affordable (~$5 for thousands of searches)
   - **Why needed?** Local LLMs need web search to provide current game information

3. **Configure in Mentor**:
   - Open Settings
   - Configure your local LLM provider (OpenAI-compatible endpoint)
   - Add your Tavily API key in the Tools section
   - Close settings

**Reality Check:** Unless you have specific privacy requirements or already run local LLMs, just use Perplexity or Claude. They're faster, better, and cost pennies.

## First Analysis

1. Launch Mentor
2. Load a game screenshot:
   - Click **Browse** to select a file
   - Or paste from clipboard (Ctrl+V)
   - Or drag & drop an image
3. Enter your question (e.g., "What should I focus on next?")
4. Select your configured AI provider
5. Click **Analyze Screenshot**
6. Wait 10-30 seconds for results

## Troubleshooting

### "No providers configured"
- Open Settings and add at least one API key

### "Analysis failed" or timeout errors
- Check your internet connection
- Verify your API key is correct
- Try a different provider

### Local LLM not working
- See [Local LLMs.md](Local%20LLMS.md) for detailed troubleshooting
- Ensure your local server is running
- Verify the endpoint URL is correct
- Check that you have Tavily configured

## Need Help?

- Check [Building.md](../Building.md) for advanced configuration
- See [Local LLMs.md](Local%20LLMS.md) if using local models
- Open an [issue on GitHub](https://github.com/scornflake/mentor-game-analysis/issues)

