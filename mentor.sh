#!/bin/bash

# Set your API key here
# export LLM__OpenAI__ApiKey="your-api-key-here"

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Path to the Mentor.CLI executable
CLI_PATH="$SCRIPT_DIR/src/Mentor.CLI/bin/Debug/net8.0/Mentor.CLI"

# Check if the executable exists
if [ ! -f "$CLI_PATH" ]; then
    echo "Error: Mentor.CLI not found at $CLI_PATH"
    echo "Please build the project first with: dotnet build"
    exit 1
fi

# Execute the CLI with all arguments passed through
echo "Executing CLI at $CLI_PATH"
# PARET_CLI_DIR=$(dirname $CLI_PATH)
# echo "Parent CLI directory: $PARET_CLI_DIR"
"$CLI_PATH" "$@"

# pop back to the original directory
# cd -

