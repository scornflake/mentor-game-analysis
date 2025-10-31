using System;

namespace Mentor.Uno.ViewModels;

public enum StreamingEventType
{
    ToolCall,
    ToolResult,
    McpToolCall,
    McpToolResult,
    TextChunk
}

public class StreamingEvent
{
    public StreamingEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    
    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");
    
    public string Icon => EventType switch
    {
        StreamingEventType.ToolCall => "🔧",
        StreamingEventType.ToolResult => "✅",
        StreamingEventType.McpToolCall => "🔌",
        StreamingEventType.McpToolResult => "✓",
        StreamingEventType.TextChunk => "📝",
        _ => "•"
    };
}

