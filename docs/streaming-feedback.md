# Streaming LLM Feedback with Microsoft.Extensions.AI

## Overview

The analysis service now uses **streaming responses** from Microsoft.Extensions.AI to provide real-time feedback about LLM operations, including:

- Tool/MCP calls as they happen
- Tool call arguments and results
- Real-time text generation
- Completion statistics

## Implementation

### What Changed

The `AnalysisService.ExecuteAndParse()` method now uses `GetStreamingResponseAsync()` instead of `GetResponseAsync<T>()`. This allows the system to:

1. **Stream responses** - Get updates as the LLM generates content
2. **Log tool calls in real-time** - See when the LLM decides to call tools/MCP functions
3. **Monitor tool results** - See what data tools return
4. **Track completion details** - Understand finish reasons and statistics

### Code Changes

**Before:**
```csharp
var completion = await _llmClient.ChatClient.GetResponseAsync<LLMResponse>(
    messages, jsonOptions, options, cancellationToken);
var jsonResponse = completion.Result;
```

**After:**
```csharp
await foreach (var update in _llmClient.ChatClient.GetStreamingResponseAsync(
    messages, options, cancellationToken))
{
    foreach (var content in update.Contents)
    {
        if (content is FunctionCallContent toolCall)
        {
            _logger.LogInformation("🔧 LLM calling tool: {ToolName}", toolCall.Name);
            _logger.LogDebug("Tool call ID: {CallId}, Arguments: {Args}", 
                toolCall.CallId, toolCall.Arguments);
        }
        else if (content is FunctionResultContent toolResult)
        {
            _logger.LogInformation("✅ Tool call {CallId} completed", toolResult.CallId);
            _logger.LogDebug("Tool result preview: {Preview}", preview);
        }
    }
}
```

## Viewing Feedback

### Configure Logging Levels

To see the detailed LLM feedback, configure your logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Extensions.AI": "Debug"
    }
  }
}
```

**Logging Levels:**
- `Information` - Shows tool calls, completions, and high-level operations
- `Debug` - Adds tool arguments, result previews, and detailed metadata
- `Trace` - Shows every text chunk received during streaming

### Example Output

When running an analysis with tools enabled, you'll see logs like:

```
[12:34:56 INF] Starting LLM streaming request with 2 tools available
[12:34:58 INF] 🔧 LLM calling tool: SearchTheWebSummary
[12:34:58 DBG] Tool call ID: call_abc123, Arguments: {"query":"Warframe best builds 2024"}
[12:35:02 INF] ✅ Tool call call_abc123 completed
[12:35:02 DBG] Tool result preview: Found 10 articles about Warframe builds. Top results include...
[12:35:03 INF] 🔧 LLM calling tool: ReadArticleContent
[12:35:03 DBG] Tool call ID: call_def456, Arguments: {"url":"https://example.com/warframe-builds"}
[12:35:05 INF] ✅ Tool call call_def456 completed
[12:35:05 DBG] Tool result preview: # Warframe Build Guide\n\nThis comprehensive guide covers...
[12:35:10 INF] LLM streaming completed. Finish reason: Stop
[12:35:10 INF] Total tool interactions - Calls: 2, Results: 2
[12:35:10 DBG] Full response text length: 2456 chars
```

## MCP Calls

When using **MCP (Model Context Protocol)** tools through the server's built-in MCP support:

1. The LLM will call MCP tools through the server
2. Each MCP call will be logged with:
   - Tool name
   - Call ID
   - Arguments passed
   - Result summary
3. The middleware logging (`.UseLogging()`) will also capture these

### MCP-Specific Logging

The Microsoft.Extensions.AI logging middleware automatically captures:
- MCP tool registration
- MCP function invocations
- MCP response handling
- Token usage

To see this, ensure your logging configuration includes:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.AI": "Debug",
      "Microsoft.Extensions.AI.FunctionInvocationLoggingChatClient": "Debug"
    }
  }
}
```

## Benefits

### For Development
- **Debug tool calls** - See exactly when and why tools are called
- **Inspect arguments** - Verify the LLM is passing correct parameters
- **Monitor performance** - Track how long tool calls take
- **Catch issues early** - See errors in tool invocation immediately

### For Production
- **Observability** - Understand LLM behavior in real-time
- **Audit trail** - Complete log of all AI operations
- **Performance monitoring** - Track token usage and completion times
- **Debugging** - Diagnose issues with tool integration

## Related Middleware

The `LLMProviderFactory` configures these middleware layers:

```csharp
var client = openAIClient.AsIChatClient()
    .AsBuilder()
    .UseLogging()              // Logs all operations
    .UseFunctionInvocation()   // Handles tool calls automatically
    .Build(_serviceProvider);
```

### Middleware Features

1. **UseLogging()** - Structured logging of all chat operations
2. **UseFunctionInvocation()** - Automatic tool call handling and retry logic

Both work together to provide comprehensive feedback about LLM operations.

## Future Enhancements

Potential improvements:

1. **Progress reporting** - Surface tool calls to UI via `IProgress<AnalysisProgress>`
2. **Metrics collection** - Track tool usage statistics
3. **OpenTelemetry** - Add `.UseOpenTelemetry()` for distributed tracing
4. **Custom middleware** - Add project-specific logging or telemetry

## See Also

- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)
- `src/Mentor.Core/Tools/AnalysisService.cs` - Implementation
- `src/Mentor.Core/Services/LLMProviderFactory.cs` - Middleware configuration

