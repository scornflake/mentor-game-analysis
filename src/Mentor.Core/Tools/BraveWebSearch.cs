using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;
using Mentor.Core.Data;
using Mentor.Core.Models;
using Mentor.Core.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentor.Core.Tools;

public class BraveWebSearch : IWebSearchTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BraveWebSearch> _logger;
    private ToolConfigurationEntity _config = new ToolConfigurationEntity();
    private static readonly ConcurrentDictionary<string, RateLimiter> _rateLimiters = new();
    private RateLimiter? _rateLimiter;

    public BraveWebSearch(IHttpClientFactory httpClientFactory, ILogger<BraveWebSearch> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger;
    }

    public void Configure(ToolConfigurationEntity configuration)
    {
        _config = configuration;
        
        // Create or get rate limiter for this API key
        if (!string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _rateLimiter = _rateLimiters.GetOrAdd(_config.ApiKey, key =>
            {
                _logger.LogDebug("Creating rate limiter for API key: {KeyPrefix}****", key.Substring(0, Math.Min(4, key.Length)));
                return new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(1),
                    SegmentsPerWindow = 1,
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
        query += ". do not include videos or images in the results.";
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

        ValidateQuery(query, 50, 400);

        var searchResponse = await ExecuteSearchWithRateLimit(query, maxResults);
        if (searchResponse?.Web?.Results == null || searchResponse.Web.Results.Count == 0)
        {
            return new List<SearchResult>();
        }

        var results = searchResponse.Web.Results.Take(maxResults).ToList();
        LogResults(query, results);
        return results;
    }

    private void ValidateQuery(string query, int maxWords, int maxCharacters) {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        if (query.Length > maxCharacters)
            {
            throw new ArgumentException($"Query cannot be longer than {maxCharacters} characters", nameof(query));
        }

        if (query.GetNumberOfWords() > maxWords)
        {
            throw new ArgumentException($"Query cannot be longer than {maxWords} words", nameof(query));
        }
    }

    private async Task<BraveSearchResponse?> ExecuteSearchWithRateLimit(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        // If no rate limiter configured, execute directly
        if (_rateLimiter == null)
        {
            return await ExecuteSearch(query, maxResults);
        }

        // Try to acquire rate limiter permit, retry once if needed
        for (int attempt = 0; attempt < 2; attempt++)
        {
            using (var lease = await _rateLimiter.AcquireAsync(1, cancellationToken))
            {
                if (lease.IsAcquired)
                {
                    _logger.LogDebug("Rate limit acquired for search request");
                    return await ExecuteSearch(query, maxResults);
                }
                
                if (attempt == 0)
                {
                    // Calculate wait time: use RetryAfter if available, otherwise wait full window period
                    var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue) 
                        ? retryAfterValue 
                        : (TimeSpan?)null;
                    
                    var waitTime = retryAfter ?? TimeSpan.FromSeconds(1);
                    
                    _logger.LogWarning("Rate limit exceeded, waiting {WaitTime}ms before retry", waitTime.TotalMilliseconds);
                    await Task.Delay(waitTime, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("Rate limit exceeded after retry. Maximum 1 request per second allowed.");
                }
            }
        }

        // Should never reach here
        throw new InvalidOperationException("Unexpected rate limiting flow");
    }

    private async Task<BraveSearchResponse?> ExecuteSearch(string query, int maxResults)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_config.Timeout);

        // actually we can ignore the base url of the provider here since we know what it is
        var baseUrl = "https://api.search.brave.com/res/v1/web/search";
        
        var requestUrl = $"{baseUrl}?q={Uri.EscapeDataString(query)}&count={maxResults}";
        _logger.LogInformation("Executing search with URL: {requestUrl}", requestUrl);
        // _logger.LogInformation("Using token: {token}", _config.ApiKey);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Add("X-Subscription-Token", _config.ApiKey);
        request.Headers.Add("Accept", "application/json");

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Brave Search API returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        try
        {
            var jsonOptions = MentorJsonSerializerContext.CreateOptions();
            var searchResponse = JsonSerializer.Deserialize<BraveSearchResponse>(responseContent, jsonOptions);
            return searchResponse;
        }
        catch (JsonException ex)
        {
            // Save the result for debugging, to an html file
            var filePath = Path.Combine(AppContext.BaseDirectory, "debug", $"brave_search_{Guid.NewGuid()}.html");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, responseContent);

            _logger.LogError("Error deserializing Brave Search response: {ex}", ex);
            _logger.LogInformation("The returned content was: {responseContent}", responseContent);
            throw new InvalidOperationException("Failed to parse search results from Brave Search API.", ex);
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

        var responseContent = await ExecuteSearchWithRateLimit(query, maxResults);
        var results = responseContent?.Web?.Results;
        if (results == null || results.Count == 0)
        {
            return new List<SearchResult>();
        }

        results = results.Take(maxResults).ToList();
        LogResults(query, results);

        return results;
    }

    private void LogResults(string query, List<SearchResult> results)
    {
        _logger.LogInformation("Search results for '{query}':", query);
        foreach (var result in results)
        {
            _logger.LogInformation("Title: {Title}", result.Title);
            _logger.LogInformation("URL: {Url}", result.Url);
            _logger.LogInformation("Description: {Description}", result.Content);
        }
    }
}