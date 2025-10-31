using System;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mentor.Uno.ViewModels;

public partial class StreamingFeedbackViewModel : ObservableObject
{
    [ObservableProperty]
    private string _accumulatedText = string.Empty;
    
    [ObservableProperty]
    private string _currentStatus = "Initializing...";
    
    public ObservableCollection<StreamingEvent> Events { get; } = new();
    
    private readonly StringBuilder _textBuilder = new();
    private string _currentToolCall = string.Empty;
    private readonly Dictionary<string, StreamingEvent> _activeToolCalls = new();
    
    public void HandleAIContent(AIContent content)
    {
        if (content == null) return;
        
        switch (content)
        {
            case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                HandleTextContent(textContent);
                break;
                
            case FunctionCallContent toolCall:
                HandleFunctionCall(toolCall);
                break;
                
            case FunctionResultContent toolResult:
                HandleFunctionResult(toolResult);
                break;
                
            case McpServerToolCallContent mcpCall:
                HandleMcpToolCall(mcpCall);
                break;
                
            case McpServerToolResultContent mcpResult:
                HandleMcpToolResult(mcpResult);
                break;
        }
    }
    
    private void HandleTextContent(TextContent textContent)
    {
        _textBuilder.Append(textContent.Text);
        AccumulatedText = _textBuilder.ToString();
        CurrentStatus = $"Receiving response... ({AccumulatedText.Length} characters)";
    }
    
    private void HandleFunctionCall(FunctionCallContent toolCall)
    {
        if (_currentToolCall != toolCall.Name)
        {
            _currentToolCall = toolCall.Name ?? string.Empty;
            
            var streamingEvent = new StreamingEvent
            {
                EventType = StreamingEventType.ToolCall,
                Timestamp = DateTime.Now,
                ToolName = toolCall.Name ?? "Unknown",
                Details = $"Arguments: {toolCall.Arguments?.ToString() ?? "(none)"}",
                IsCompleted = false
            };
            
            Events.Add(streamingEvent);
            
            // Track this call for completion updates
            if (!string.IsNullOrEmpty(toolCall.CallId))
            {
                _activeToolCalls[toolCall.CallId] = streamingEvent;
            }
            
            CurrentStatus = $"Calling tool: {toolCall.Name}";
        }
    }
    
    private void HandleFunctionResult(FunctionResultContent toolResult)
    {
        var resultPreview = toolResult.Result?.ToString();
        var preview = resultPreview != null && resultPreview.Length > 100
            ? resultPreview.Substring(0, 100) + "..."
            : resultPreview ?? "(null)";
        
        var streamingEvent = new StreamingEvent
        {
            EventType = StreamingEventType.ToolResult,
            Timestamp = DateTime.Now,
            ToolName = toolResult.CallId ?? "Unknown",
            Details = $"Result: {preview}",
            IsCompleted = true
        };
        
        Events.Add(streamingEvent);
        
        // Mark the corresponding call as completed if we can find it
        if (!string.IsNullOrEmpty(toolResult.CallId) && _activeToolCalls.TryGetValue(toolResult.CallId, out var callEvent))
        {
            callEvent.IsCompleted = true;
            _activeToolCalls.Remove(toolResult.CallId);
        }
        
        CurrentStatus = $"Tool completed: {toolResult.CallId}";
        _currentToolCall = string.Empty;
    }
    
    private void HandleMcpToolCall(McpServerToolCallContent mcpCall)
    {
        var streamingEvent = new StreamingEvent
        {
            EventType = StreamingEventType.McpToolCall,
            Timestamp = DateTime.Now,
            ToolName = mcpCall.ToolName ?? "Unknown",
            Details = $"Arguments: {mcpCall.Arguments?.ToString() ?? "(none)"}",
            IsCompleted = false
        };
        
        Events.Add(streamingEvent);
        
        // Track this call for completion updates
        if (!string.IsNullOrEmpty(mcpCall.CallId))
        {
            _activeToolCalls[mcpCall.CallId] = streamingEvent;
        }
        
        CurrentStatus = $"Calling MCP tool: {mcpCall.ToolName}";
    }
    
    private void HandleMcpToolResult(McpServerToolResultContent mcpResult)
    {
        var resultPreview = mcpResult.Output?.ToString();
        var preview = resultPreview != null && resultPreview.Length > 100
            ? resultPreview.Substring(0, 100) + "..."
            : resultPreview ?? "(null)";
        
        var streamingEvent = new StreamingEvent
        {
            EventType = StreamingEventType.McpToolResult,
            Timestamp = DateTime.Now,
            ToolName = mcpResult.CallId ?? "Unknown",
            Details = $"Result: {preview}",
            IsCompleted = true
        };
        
        Events.Add(streamingEvent);
        
        // Mark the corresponding call as completed if we can find it
        if (!string.IsNullOrEmpty(mcpResult.CallId) && _activeToolCalls.TryGetValue(mcpResult.CallId, out var callEvent))
        {
            callEvent.IsCompleted = true;
            _activeToolCalls.Remove(mcpResult.CallId);
        }
        
        CurrentStatus = "MCP tool completed";
    }
    
    public void Clear()
    {
        Events.Clear();
        _textBuilder.Clear();
        AccumulatedText = string.Empty;
        CurrentStatus = "Initializing...";
        _currentToolCall = string.Empty;
        _activeToolCalls.Clear();
    }
}

