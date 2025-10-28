using System.Text;
using System.Text.Json;
using Mentor.Core.Data;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentor.Core.Tools;

public class BraveWebSearch : IWebSearchTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BraveWebSearch> _logger;
    private ToolConfigurationEntity _config = new ToolConfigurationEntity();

    public BraveWebSearch(IHttpClientFactory httpClientFactory, ILogger<BraveWebSearch> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    public void Configure(ToolConfigurationEntity configuration)
    {
        _config = configuration;
    }

    private string FormQuery(string userPrompt)
    {
        var query = userPrompt;
        query += ". do not include videos or images in the results.";
        query += ". Prefer recent results (within the last year) where possible.";
        return query;
    }
    
    public async Task<IList<SearchResult>> SearchStructured(string query, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        query = FormQuery(query);
        _logger.LogInformation("Searching for {query} using format 'structured'", query);
        
        var searchResponse = await ExecuteSearch(query, maxResults);
        if (searchResponse?.Web?.Results == null || searchResponse.Web.Results.Count == 0)
        {
            return new List<SearchResult>();
        }

        var results = searchResponse.Web.Results.Take(maxResults).ToList();
        LogResults(query, results);
        return results;
    }

    private async Task<BraveSearchResponse?> ExecuteSearch(string query, int maxResults)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);

        var requestUrl = $"{_config.BaseUrl}/web/search?q={Uri.EscapeDataString(query)}&count={maxResults}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Add("X-Subscription-Token", _config.ApiKey);
        request.Headers.Add("Accept", "application/json");

        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Brave Search API returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<BraveSearchResponse>(responseContent);
        return searchResponse;
    }


    public async Task<string> Search(string query, SearchOutputFormat format, int maxResults = 5)
    {
        if (format == SearchOutputFormat.Structured)
        {
            throw new NotSupportedException("Use the 'structured' method call to return structured results.");
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        query = FormQuery(query);
        _logger.LogInformation("Searching for {query} using format: {format}", query, format);

        var responseContent = await ExecuteSearch(query, maxResults);
        var results = responseContent?.Web?.Results;
        if (results == null || results.Count == 0)
        {
            return "No results found for the given query.";
        }

        results = results.Take(maxResults).ToList();
        LogResults(query, results);

        return format switch
        {
            SearchOutputFormat.Snippets => FormatAsSnippets(results),
            SearchOutputFormat.Summary => FormatAsSummary(query, results),
            _ => throw new ArgumentException($"Unknown format: {format}", nameof(format))
        };
    }

    private void LogResults(string query, List<SearchResult> results)
    {
        _logger.LogInformation("Search results for '{query}':", query);
        foreach (var result in results)
        {
            _logger.LogInformation("Title: {Title}", result.Title);
            _logger.LogInformation("URL: {Url}", result.Url);
            _logger.LogInformation("Description: {Description}", result.Description);
        }

    }

    private static string FormatAsSnippets(List<SearchResult> results)
    {
        var sb = new StringBuilder();
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.Description))
            {
                sb.AppendLine(result.Description);
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatAsSummary(string query, List<SearchResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Search results for '{query}':");
        sb.AppendLine();
        sb.AppendLine($"Found {results.Count} result(s):");
        
        foreach (var result in results)
        {
            if (!string.IsNullOrWhiteSpace(result.Description))
            {
                // Take first sentence or first 150 characters
                var snippet = result.Description.Length > 150 
                    ? result.Description[..150] + "..." 
                    : result.Description;
                sb.AppendLine($"- {snippet}");
            }
        }
        
        return sb.ToString().TrimEnd();
    }

    private class BraveSearchResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("web")]
        public WebResults? Web { get; set; }
    }

    private class WebResults
    {
        [System.Text.Json.Serialization.JsonPropertyName("results")]
        public List<SearchResult> Results { get; set; } = new();
    }
}