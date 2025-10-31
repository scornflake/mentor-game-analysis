using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Mentor.Core.Data;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace Mentor.Core.Tools;

public class TavilyWebSearch : IWebSearchTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TavilyWebSearch> _logger;
    private ToolConfigurationEntity _config = new ToolConfigurationEntity();
    private static readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new();
    private RateLimiter? _rateLimiter;

    public TavilyWebSearch(IHttpClientFactory httpClientFactory, ILogger<TavilyWebSearch> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    public void Configure(ToolConfigurationEntity configuration)
    {
        _config = configuration;
        
        // Create or get rate limiter for this API key (100 requests per minute)
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _rateLimiter = _rateLimiters.GetOrAdd(_config.ApiKey, key =>
            {
                _logger.LogDebug("Creating rate limiter for API key: {KeyPrefix}****", key.Substring(0, Math.Min(4, key.Length)));
                return new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queuing, fail immediately if limit exceeded
                });
            });
        }
    }

    private string FormQuery(SearchContext context)
    {
        var query = "";
        if (!string.IsNullOrEmpty(context.GameName))
        {
            query += $"{context.GameName}, ";
        }
        query += $"Give me accurate information about: {context.Query}";
        query += ". Prefer recent results.";
        return query;
    }

    public async Task<IList<SearchResult>> SearchStructured(SearchContext context, int maxResults = 5)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        
        if (string.IsNullOrWhiteSpace(context.Query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(context.Query));
        }

        var query = FormQuery(context);
        _logger.LogInformation("Searching for {query} using format 'structured'", query);

        ValidateQuery(query);

        var searchResponse = await ExecuteSearchWithRateLimit(query, maxResults, searchDepth: "basic", includeAnswer: false);
        if (searchResponse?.Results == null || searchResponse.Results.Count == 0)
        {
            return new List<SearchResult>();
        }

        var results = searchResponse.Results.Take(maxResults)
            .Select(r => new SearchResult
            {
                Title = r.Title,
                Url = r.Url,
                Content = r.Content,
                Score = r.Score
            })
            .ToList();
            
        LogResults(query, results);
        return results;
    }

    private void ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        // Tavily has a 400 character limit
        if (query.Length > 400)
        {
            throw new ArgumentException("Query cannot be longer than 400 characters", nameof(query));
        }
    }

    private async Task<TavilySearchResponse?> ExecuteSearchWithRateLimit(string query, int maxResults, string searchDepth = "basic", bool includeAnswer = false, CancellationToken cancellationToken = default)
    {
        // If no rate limiter configured, execute directly
        if (_rateLimiter == null)
        {
            return await ExecuteSearch(query, maxResults, searchDepth, includeAnswer);
        }

        // Try to acquire rate limiter permit
        using (var lease = await _rateLimiter.AcquireAsync(1, cancellationToken))
        {
            if (lease.IsAcquired)
            {
                _logger.LogDebug("Rate limit acquired for search request");
                return await ExecuteSearch(query, maxResults, searchDepth, includeAnswer);
            }
            
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue) 
                ? retryAfterValue 
                : (TimeSpan?)null;
            
            var waitTime = retryAfter ?? TimeSpan.FromSeconds(1);
            throw new InvalidOperationException($"Rate limit exceeded. Maximum 100 requests per minute allowed. Retry after {waitTime.TotalSeconds} seconds.");
        }
    }

    private async Task<TavilySearchResponse?> ExecuteSearch(string query, int maxResults, string searchDepth, bool includeAnswer)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);

        var baseUrl = "https://api.tavily.com/search";
        
        _logger.LogInformation("Executing Tavily search with query: {query}", query);
        
        var requestBody = new TavilySearchRequest
        {
            ApiKey = _config.ApiKey,
            Query = query,
            MaxResults = maxResults,
            SearchDepth = searchDepth,
            IncludeAnswer = includeAnswer,
            IncludeImages = false
        };

        var jsonContent = JsonSerializer.Serialize(requestBody, MentorJsonSerializerContext.CreateOptions());
        var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Tavily Search API returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        try
        {
            var jsonOptions = MentorJsonSerializerContext.CreateOptions();
            var searchResponse = JsonSerializer.Deserialize<TavilySearchResponse>(responseContent, jsonOptions);
            return searchResponse;
        }
        catch (JsonException ex)
        {
            // Save the result for debugging
            var filePath = Path.Combine(AppContext.BaseDirectory, "debug", $"tavily_search_{Guid.NewGuid()}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, responseContent);

            _logger.LogError("Error deserializing Tavily Search response: {ex}", ex);
            _logger.LogInformation("The returned content was: {responseContent}", responseContent);
            throw new InvalidOperationException("Failed to parse search results from Tavily Search API.", ex);
        }
    }

    public async Task<List<SearchResult>> Search(SearchContext context, int maxResults = 5)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        
        if (string.IsNullOrWhiteSpace(context.Query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(context.Query));
        }

        var query = FormQuery(context);
        _logger.LogInformation("Searching for {query}", query);

        ValidateQuery(query);

        var responseContent = await ExecuteSearchWithRateLimit(query, maxResults, searchDepth: "basic", includeAnswer: false);
        var results = responseContent?.Results;
        
        if (results == null || results.Count == 0)
        {
            return new List<SearchResult>();
        }

        var searchResults = results.Take(maxResults)
            .Select(r => new SearchResult
            {
                Title = r.Title,
                Url = r.Url,
                Content = r.Content,
                Score = r.Score
            })
            .ToList();
            
        LogResults(query, searchResults);

        return searchResults;
    }

    private void LogResults(string query, List<SearchResult> results)
    {
        _logger.LogInformation("Search results for '{query}':", query);
        foreach (var result in results)
        {
            _logger.LogInformation("Title: {Title}", result.Title);
            _logger.LogInformation("URL: {Url}", result.Url);
            _logger.LogInformation("Description: {Description}", result.Content);
            if (result.Score.HasValue)
            {
                _logger.LogInformation("Score: {Score}", result.Score.Value);
            }
        }
    }
}

