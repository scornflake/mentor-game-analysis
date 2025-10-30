namespace Mentor.Core.Tools;

public interface ITextSummarizer
{
    /// <summary>
    /// Summarizes the provided content using an LLM.
    /// </summary>
    /// <param name="content">The content to summarize.</param>
    /// <param name="prompt">Custom prompt to guide the summarization. If null or empty, a default prompt will be used.</param>
    /// <param name="targetWordCount">Target word count for the summary.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The summarized text.</returns>
    Task<string> SummarizeAsync(string content, string prompt, int targetWordCount, CancellationToken cancellationToken = default);
}

