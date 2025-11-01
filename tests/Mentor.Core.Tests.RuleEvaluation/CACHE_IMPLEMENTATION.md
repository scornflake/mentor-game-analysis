# Recommendation Caching and Objective-Only Mode Implementation

## Overview

This implementation adds two major features to the Rule Evaluation project:

1. **Recommendation Caching**: Caches generated LLM recommendations to avoid regenerating them on re-evaluation
2. **Objective-Only Mode**: Skip subjective evaluation and only run objective metrics from MetricsCalculator

## New Components

### RecommendationCacheService

**Location**: `Services/RecommendationCacheService.cs`

A service that handles caching of `Recommendation` objects to disk:
- Uses `IUserDataPathService` with 'evaluation' subfolder
- Cache location: `%LOCALAPPDATA%/Mentor/evaluation/`
- Cache key generation: SHA256 hash of screenshot path + prompt + provider + rules enabled + rule files
- Separate cache files for baseline and rule-augmented analyses

**Methods**:
- `TryGetCachedAsync()` - Retrieves cached recommendation if available
- `SaveToCacheAsync()` - Saves recommendation to cache
- `GetCacheKey()` - Generates cache key from analysis parameters

**Cache File Naming**:
- `{cache_key}_baseline.json` - Baseline analysis (no rules)
- `{cache_key}_ruleaugmented.json` - Rule-augmented analysis

### Updated Components

#### RuleComparisonHarness
- Added optional `RecommendationCacheService` constructor parameter
- Added `useCache` parameter to comparison methods
- Modified `RunAnalysisAsync()` to check cache before generating recommendations
- Saves recommendations to cache after generation
- Logs "[cached]" when using cached recommendations

#### Program.cs
- Added `--no-cache` flag parsing
- Added `--objective-only` flag parsing
- Conditional subjective evaluation based on `--objective-only` flag
- Registered `RecommendationCacheService` in DI container
- Updated help text with new flags

## Usage

### Evaluation with Caching (Default)

```bash
dotnet run -- evaluation-acceltra.json
```

This will:
1. Check cache for existing recommendations
2. Use cached recommendations if available
3. Generate new recommendations if not cached
4. Save new recommendations to cache
5. Run both objective and subjective evaluations (if criteria provided)

### Force Regeneration

```bash
dotnet run -- evaluation-acceltra.json --no-cache
```

This will:
1. Ignore existing cache
2. Generate all recommendations from scratch
3. Save new recommendations to cache (overwriting old ones)
4. Run both objective and subjective evaluations (if criteria provided)

### Objective Metrics Only

```bash
dotnet run -- evaluation-acceltra.json --objective-only
```

This will:
1. Use cached recommendations if available
2. Run only objective metrics (specificity, terminology, actionability)
3. Skip subjective evaluation entirely
4. Faster execution (no LLM-based evaluation calls)

### Combined Flags

```bash
dotnet run -- evaluation-acceltra.json --no-cache --objective-only
```

This will:
1. Ignore existing cache
2. Generate all recommendations from scratch
3. Save new recommendations to cache
4. Run only objective metrics (no subjective evaluation)

## Benefits

### Caching Benefits
- **Speed**: Avoid regenerating LLM recommendations (expensive and slow)
- **Consistency**: Compare metrics on the same recommendations across runs
- **Cost**: Reduce API costs for cloud LLM providers
- **Iteration**: Quickly iterate on metrics without regenerating recommendations

### Objective-Only Benefits
- **Speed**: Skip time-consuming subjective evaluation
- **Focus**: Iterate on objective metrics alone
- **Cost**: Avoid additional LLM evaluation calls
- **Development**: Faster feedback loop when tuning objective metrics

## Cache Invalidation

Cache is automatically invalidated when:
- Screenshot path changes
- Prompt changes
- Provider changes
- Rules enabled/disabled toggles
- Rule files list changes

To manually invalidate cache:
1. Delete cache files: `%LOCALAPPDATA%/Mentor/evaluation/*.json`
2. Use `--no-cache` flag

## Implementation Details

### Cache Key Generation
```csharp
// Components that affect cache key:
- Screenshot path (absolute)
- Prompt text
- Provider name
- Rules enabled (true/false)
- Rule files (sorted list)

// Hash with SHA256 for fixed-length key
var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
var cacheKey = Convert.ToHexString(hash).ToLowerInvariant();
```

### Service Registration
```csharp
services.AddSingleton<RecommendationCacheService>();
```

The service is registered as a singleton because:
- It's stateless (only reads/writes files)
- Shared across all evaluations in a run
- Thread-safe for concurrent access

## Testing

Build verification:
```bash
dotnet build tests/Mentor.Core.Tests.RuleEvaluation/Mentor.Core.Tests.RuleEvaluation.csproj
```

Help text verification:
```bash
dotnet run --project tests/Mentor.Core.Tests.RuleEvaluation -- --help
```

## Future Enhancements

Potential improvements:
- Add cache statistics (hits/misses) to report
- Add `--clear-cache` command to delete all cached recommendations
- Add cache expiration based on timestamp
- Add cache size management (delete old entries)
- Support for cache versioning (invalidate on schema changes)

