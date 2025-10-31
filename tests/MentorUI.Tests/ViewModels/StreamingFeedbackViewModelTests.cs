using System.Diagnostics.CodeAnalysis;
using Mentor.Uno.ViewModels;
using Microsoft.Extensions.AI;

namespace MentorUI.Tests.ViewModels;

[Experimental("MEAI001")]
public class StreamingFeedbackViewModelTests
{
    [Fact]
    public void HandleTextContent_AccumulatesText()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var textContent1 = new TextContent("Hello ");
        var textContent2 = new TextContent("World");

        // Act
        viewModel.HandleAIContent(textContent1);
        viewModel.HandleAIContent(textContent2);

        // Assert
        Assert.Equal("Hello World", viewModel.AccumulatedText);
        Assert.Contains("11 characters", viewModel.CurrentStatus);
    }

    [Fact]
    public void HandleFunctionCall_AddsEventToCollection()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var toolCall = new FunctionCallContent("testCallId", "TestTool", new { param = "value" });

        // Act
        viewModel.HandleAIContent(toolCall);

        // Assert
        Assert.Single(viewModel.Events);
        Assert.Equal(StreamingEventType.ToolCall, viewModel.Events[0].EventType);
        Assert.Equal("TestTool", viewModel.Events[0].ToolName);
        Assert.False(viewModel.Events[0].IsCompleted);
        Assert.Equal("Calling tool: TestTool", viewModel.CurrentStatus);
    }

    [Fact]
    public void HandleFunctionResult_AddsEventAndMarksPreviousAsCompleted()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var toolCall = new FunctionCallContent("testCallId", "TestTool");
        var toolResult = new FunctionResultContent("testCallId", "TestTool", "Result data");

        // Act
        viewModel.HandleAIContent(toolCall);
        viewModel.HandleAIContent(toolResult);

        // Assert
        Assert.Equal(2, viewModel.Events.Count);
        Assert.True(viewModel.Events[0].IsCompleted); // Call marked as completed
        Assert.Equal(StreamingEventType.ToolResult, viewModel.Events[1].EventType);
        Assert.True(viewModel.Events[1].IsCompleted);
    }

    [Fact]
    public void HandleMcpToolCall_AddsEventToCollection()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var mcpCall = new McpServerToolCallContent("mcpCallId", "McpTestTool", new { param = "value" });

        // Act
        viewModel.HandleAIContent(mcpCall);

        // Assert
        Assert.Single(viewModel.Events);
        Assert.Equal(StreamingEventType.McpToolCall, viewModel.Events[0].EventType);
        Assert.Equal("McpTestTool", viewModel.Events[0].ToolName);
        Assert.False(viewModel.Events[0].IsCompleted);
        Assert.Equal("Calling MCP tool: McpTestTool", viewModel.CurrentStatus);
    }

    [Fact]
    public void HandleMcpToolResult_AddsEventAndMarksPreviousAsCompleted()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var mcpCall = new McpServerToolCallContent("mcpCallId", "McpTestTool");
        var mcpResult = new McpServerToolResultContent("mcpCallId", "Result data");

        // Act
        viewModel.HandleAIContent(mcpCall);
        viewModel.HandleAIContent(mcpResult);

        // Assert
        Assert.Equal(2, viewModel.Events.Count);
        Assert.True(viewModel.Events[0].IsCompleted); // Call marked as completed
        Assert.Equal(StreamingEventType.McpToolResult, viewModel.Events[1].EventType);
        Assert.True(viewModel.Events[1].IsCompleted);
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        viewModel.HandleAIContent(new TextContent("Some text"));
        viewModel.HandleAIContent(new FunctionCallContent("id1", "Tool1"));

        // Act
        viewModel.Clear();

        // Assert
        Assert.Empty(viewModel.Events);
        Assert.Equal(string.Empty, viewModel.AccumulatedText);
        Assert.Equal("Initializing...", viewModel.CurrentStatus);
    }

    [Fact]
    public void HandleAIContent_WithNull_DoesNotThrow()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();

        // Act & Assert
        var exception = Record.Exception(() => viewModel.HandleAIContent(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void HandleTextContent_UpdatesStatus()
    {
        // Arrange
        var viewModel = new StreamingFeedbackViewModel();
        var textContent = new TextContent("Test content");

        // Act
        viewModel.HandleAIContent(textContent);

        // Assert
        Assert.Contains("Receiving response", viewModel.CurrentStatus);
        Assert.Contains("12 characters", viewModel.CurrentStatus);
    }
}

