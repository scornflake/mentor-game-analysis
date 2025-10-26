using System.Text;
using System.Text.Json;
using Mentor.Core.Configuration;
using Mentor.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentor.Core.Services;

public interface IWebsearch
{
    Task<string> Search(string query, SearchOutputFormat format, int maxResults = 5);
}

public class Websearch : IWebsearch
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Websearch> _logger;
    private readonly BraveSearchConfiguration _config;

    public Websearch(IHttpClientFactory httpClientFactory, IOptions<BraveSearchConfiguration> config, ILogger<Websearch> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<string> Search(string query, SearchOutputFormat format, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        _logger.LogInformation("Searching for {query} using format: {format}", query, format);
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

        if (searchResponse?.Web?.Results == null || searchResponse.Web.Results.Count == 0)
        {
            return "No results found for the given query.";
        }

        var results = searchResponse.Web.Results.Take(maxResults).ToList();

        return format switch
        {
            SearchOutputFormat.Snippets => FormatAsSnippets(results),
            SearchOutputFormat.Structured => FormatAsStructured(results),
            SearchOutputFormat.Summary => FormatAsSummary(query, results),
            _ => throw new ArgumentException($"Unknown format: {format}", nameof(format))
        };
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

    private static string FormatAsStructured(List<SearchResult> results)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            sb.AppendLine($"[{i + 1}] {result.Title}");
            sb.AppendLine($"URL: {result.Url}");
            if (!string.IsNullOrWhiteSpace(result.Description))
            {
                sb.AppendLine($"Description: {result.Description}");
            }
            if (i < results.Count - 1)
            {
                sb.AppendLine();
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

    private class SearchResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}