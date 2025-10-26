# Architecture: Game Screenshot Analysis & Recommendation System

## Overview

This system is built as a **minimal, focused .NET library** with a simple **CLI client** for MVP. The architecture prioritizes small, tight, reusable code over complex abstractions.

**MVP Scope**: Core library + CLI client  
**Future**: HTTP API, desktop UI (added when needed)

## Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│  CLI Client (Mentor.CLI)                             │
│  - Command parsing                                   │
│  - Output formatting                                 │
│  - File I/O                                          │
└──────────────────┬──────────────────────────────────┘
                   │ References
┌──────────────────▼──────────────────────────────────┐
│  Core Library (Mentor.Core)                          │
│  - Domain Models (Recommendation, AnalysisRequest)   │
│  - Analysis Service (orchestration)                  │
│  - LLM Provider Integration                          │
│  - Image Processing                                  │
│  - Configuration                                     │
│  └─► Uses Microsoft.Extensions.AI                   │
└─────────────────────────────────────────────────────┘

Future Extensions (Out of MVP Scope):
  - Mentor.API (HTTP API with ASP.NET Core)
  - Mentor.UI (Desktop UI with Avalonia)
```

## Key Design Decisions

### 1. Minimal, Focused Scope

**Decision**: Build one core library with all LLM logic, plus one CLI client for testing/usage. No UI, no HTTP API initially.

**Rationale**:
- Faster time to MVP
- Easier to test and iterate
- Core library remains reusable for future projects
- Reduces complexity and maintenance burden
- Focus on getting the core functionality right first

**Tradeoffs**:
- Less impressive demo (CLI vs GUI)
- Fewer distribution options initially
- But: cleaner codebase, better foundation

### 2. Use Existing Abstractions (Don't Reinvent the Wheel)

**Decision**: Use Microsoft.Extensions.AI for LLM abstraction instead of building custom interfaces.

**Rationale**:
- Official Microsoft package with long-term support
- Already handles multi-provider scenarios
- Built-in support for image + text prompts
- Active development and community
- Reduces our maintenance burden

**Tradeoffs**:
- Locked into Microsoft's abstraction layer
- May need thin wrappers for providers not officially supported (e.g., Perplexity)
- But: proven, tested, maintained by experts

### 3. Separation of Concerns

**Decision**: Core library has zero dependencies on CLI specifics. CLI is just one consumer.

**Rationale**:
- Core can be used in API, UI, tests, or other contexts
- Library consumers don't carry unnecessary dependencies
- Easier to test core logic in isolation
- Enables future expansion without refactoring

**Implementation Pattern**:
- Core exports services via interfaces
- CLI references Core and provides entry point
- Future API/UI projects also reference Core

### 4. Provider Flexibility

**Decision**: Configuration-driven provider selection, runtime switching without code changes.

**Rationale**:
- Different users may prefer different providers
- Costs vary across providers
- Easy to test across multiple models
- No recompilation needed to try new provider

**Configuration Hierarchy** (highest precedence first):
1. CLI argument (`--provider perplexity`)
2. Environment variable (`LLM__PROVIDER=openai`)
3. Config file (`appsettings.json`)
4. Default hardcoded value

### 5. Testability First

**Decision**: All dependencies injected via interfaces, no static state.

**Rationale**:
- Mock LLM providers to avoid API costs in tests
- Faster test execution
- Deterministic test results
- Enable TDD workflow

**Testing Strategy**:
- **Unit Tests**: Mock `IChatClient`, test business logic
- **Integration Tests**: Real LLM calls with actual providers
- **CLI Tests**: End-to-end with file I/O

### 6. Configuration Over Convention

**Decision**: Explicit configuration for all settings (API keys, models, timeouts).

**Rationale**:
- Clear visibility into what's configurable
- Environment-specific overrides (dev vs prod)
- Secrets management via environment variables
- No "magic" behavior

**Configuration Sources**:
- JSON files for defaults
- Environment variables for secrets (preferred for API keys)
- CLI arguments for one-off overrides

## Architectural Patterns

### Dependency Injection

All services registered in DI container at startup:
- Simplifies testing (can swap implementations)
- Manages object lifetimes automatically
- Makes dependencies explicit

### Service Layer Pattern

`AnalysisService` orchestrates the analysis workflow:
1. Validate input (image size, format)
2. Process image (resize, optimize)
3. Build prompt from template
4. Call LLM via `IChatClient`
5. Parse and validate response
6. Return structured recommendation

This keeps CLI thin - it just handles I/O and delegates to the service.

### Adapter Pattern

Thin wrappers around third-party LLM clients where needed:
- Perplexity: Use OpenAI client with custom endpoint
- Other providers: Use official SDK clients

Core code depends on `IChatClient` interface, not concrete implementations.

## Data Flow

```
User Command
    │
    ▼
CLI Parser (System.CommandLine)
    │
    ▼
Load Image from Disk (byte[])
    │
    ▼
IAnalysisService.AnalyzeAsync()
    │
    ├─► ImageProcessor: Validate & optimize
    │
    ├─► PromptBuilder: Construct prompt from template
    │
    ├─► IChatClient: Send to LLM
    │       │
    │       └─► [External API: Perplexity/OpenAI/Anthropic]
    │
    ├─► Parse JSON response
    │
    └─► Return Recommendation
    │
    ▼
CLI Output Formatter
    │
    ▼
JSON to stdout
```

## Performance Considerations

### Async I/O
All I/O operations (file, network) are async to avoid blocking:
- Better responsiveness
- Scalable for future API/UI scenarios
- Cancellation token support

### Image Optimization
Large screenshots are expensive to send:
- Resize to max dimension (e.g., 2048px)
- Compress to reasonable quality
- Convert to efficient format (WebP/PNG)
- Balance: quality vs cost/latency

### Connection Pooling
Use `IHttpClientFactory` for HTTP clients:
- Reuses connections
- Handles DNS changes
- Prevents socket exhaustion

### Cancellation Support
All async methods accept `CancellationToken`:
- User can cancel long-running requests
- Timeouts prevent hung operations
- Clean resource cleanup

## Security Considerations

### API Key Management

**Never hardcode secrets:**
1. Prefer environment variables
2. Use platform keychains when available (macOS Keychain, Windows Credential Manager)
3. Config files only for non-secret defaults
4. `.gitignore` any files with secrets

### Input Validation

Validate all inputs before processing:
- Image file size limits (prevent DoS)
- Image format validation (prevent malicious files)
- Prompt length limits (prevent excessive costs)
- Path traversal prevention for file operations

### HTTPS Only
All LLM API calls must use HTTPS:
- Prevents API key interception
- Ensures data integrity
- Provider SDKs handle this by default

### Error Messages
Don't leak sensitive information in errors:
- Log full details internally
- Show sanitized messages to users
- Don't expose API keys or internal paths

## Scalability Considerations

### Current Scope (MVP)
Single-user, single-request model:
- No concurrency needed
- No rate limiting
- No caching

### Future Scaling (API/UI)
When adding HTTP API or desktop UI:
- **Rate Limiting**: Prevent API abuse
- **Caching**: Cache identical requests (hash of image + prompt)
- **Queue System**: Background processing for long requests
- **Multiple Instances**: Stateless design enables horizontal scaling

## Testing Strategy

### Unit Tests
Focus on business logic in isolation:
- Mock external dependencies (`IChatClient`)
- Test prompt building logic
- Test response parsing
- Test error handling

### Integration Tests
Validate real integrations:
- Actual LLM provider calls (use test API keys)
- Image processing pipeline
- Configuration loading
- Serialization/deserialization

### End-to-End Tests
Test complete workflows:
- CLI with real screenshots
- Output validation
- Error scenarios (missing files, invalid API keys)

### Test Doubles Strategy
- **Mocks**: For verifying interactions (method calls)
- **Stubs**: For providing canned responses
- **Fakes**: In-memory implementations for complex dependencies

## Extensibility Points

### Future Provider Support
Add new LLM providers by:
1. Installing provider SDK package
2. Updating DI configuration
3. Adding configuration section
4. No core logic changes needed

### Future Output Formats
Currently: JSON to stdout  
Future options:
- Markdown reports
- HTML with styling
- PDF exports
- Structured logging

### Future Input Sources
Currently: Single image file  
Future options:
- Multiple images (sequence analysis)
- Video frames (extract key moments)
- Live screen capture
- Cloud storage (S3, Azure Blob)

## Deployment Models

### MVP: CLI Tool
Distributed as:
- Self-contained executable (no .NET runtime required)
- Platform-specific builds (macOS, Windows, Linux)
- Installed via package managers (Homebrew, Chocolatey)

### Future: HTTP API
Deployed as:
- Docker container
- Cloud service (Azure App Service, AWS Lambda)
- Kubernetes deployment

### Future: Desktop App
Distributed as:
- Platform-specific installers
- Auto-update support
- Code-signed for trust

## Failure Modes & Resilience

### Network Failures
- Retry with exponential backoff
- Clear error messages to user
- Graceful degradation (no partial results)

### Invalid Responses
- Schema validation for LLM responses
- Fallback to text-only if parsing fails
- Log malformed responses for debugging

### Rate Limits
- Respect provider rate limits
- Exponential backoff on 429 errors
- Clear messaging to user

### Image Processing Errors
- Validate format before processing
- Handle corrupted images gracefully
- Provide helpful error messages

## Future Architecture Evolution

### Phase 1: MVP (Current)
- Core library + CLI
- Single provider (Perplexity)
- Basic error handling

### Phase 2: Multi-Provider
- OpenAI and Anthropic support
- Provider comparison mode
- Cost tracking per provider

### Phase 3: HTTP API
- REST API with ASP.NET Core
- Authentication/authorization
- Rate limiting
- Result caching

### Phase 4: Desktop UI
- Avalonia cross-platform app
- Drag-and-drop screenshots
- History/favorites
- Provider preferences

### Phase 5: Advanced Features
- Local LLM support (Ollama)
- Plugin system for custom providers
- Batch processing
- Real-time game window monitoring

## Related Documentation

- **[MVP Goal](mvp_goal.md)**: Success criteria and scope definition
- **[Implementation Guide](implementation.md)**: Code structure, packages, and build instructions
- **[Agents](AGENTS.md)**: AI agent considerations (future)
