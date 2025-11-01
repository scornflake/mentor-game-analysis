# Technology Analysis for Warframe Assistant System

## Overview

Based on the requirements outlined in `ideas.md`, the Warframe assistant system needs to:

1. Store complex, interconnected game data (weapons, mods, frames, enemies, abilities)
2. Encode and execute game mechanics rules (status interactions, elemental weaknesses, synergies)
3. Reason about build optimization given contextual constraints
4. Answer user queries through planning and step-by-step execution
5. Maintain up-to-date game data through automated acquisition

This document analyzes applicable technologies for each requirement.

## Data Storage Technologies

### Relational Database (SQLite/PostgreSQL)

**Use Case**: Structured game data storage

**Strengths**:
- Well-defined schemas for entities (weapons, mods, warframes)
- Foreign key constraints enforce data integrity
- Efficient filtering and aggregation via SQL
- ACID transactions ensure consistency
- SQLite: zero-configuration, portable, suitable for desktop apps
- PostgreSQL: advanced features (JSONB, full-text search, custom types)

**Specific Applications**:
- Weapon stats tables with normalized damage types
- Mod compatibility matrices (weapon type → applicable mods)
- Enemy faction resistance lookups
- Version tracking for game updates

**Considerations**:
- Complex relationships (many-to-many synergies) require junction tables
- Deep nested queries (e.g., "find all builds for X that counter Y") can become verbose
- Limited semantic reasoning without additional layers

**Recommendation**: **SQLite** for v2 - aligns with desktop app nature, no server overhead, excellent .NET support via `Microsoft.Data.Sqlite`

---

### Graph Database (Neo4j)

**Use Case**: Relationship-heavy game mechanics modeling

**Strengths**:
- Natural representation of synergies: `(Mod)-[SYNERGIZES_WITH]->(Frame)`
- Path-finding queries: "what connects this weapon to optimal DPS?"
- Variable-depth traversals: "show all indirect effects of this mod choice"
- Schema flexibility for evolving game mechanics
- Cypher query language optimized for relationship patterns

**Specific Applications**:
```cypher
// Example: Find mods that synergize with Cedo Prime when using Mesa
MATCH (weapon:Weapon {name: 'Cedo Prime'})
      -[:SYNERGIZES_WITH]->(mod:Mod)
      -[:EFFECTIVE_ON]->(frame:Warframe {name: 'Mesa'})
WHERE mod.type IN ['Shotgun', 'Universal']
RETURN mod.name, mod.effect
```

**Considerations**:
- Adds infrastructure complexity (separate database server)
- .NET support via `Neo4j.Driver` is mature
- Overhead may not justify benefits if relationships are simple lookups
- Excellent if encoding complex conditional rules ("if Razor mod equipped, then status mods gain value")

**Recommendation**: Consider for future if rule complexity grows beyond simple lookups; SQLite with proper indexes may suffice initially

---

### Vector Database (Qdrant/Chroma/Milvus)

**Use Case**: Semantic search over game knowledge

**Strengths**:
- Store embeddings of wiki articles, build guides, patch notes
- Similarity search: "find builds similar to viral/slash Kuva Nukor"
- Retrieve relevant context for LLM reasoning
- Handle fuzzy queries: "what counters heavily armored enemies?"
- Integrates with existing LLM infrastructure

**Specific Applications**:
- Embed all weapon descriptions, enable "show me weapons like X"
- Store pre-analyzed build strategies, retrieve for context injection
- Semantic search over game mechanics documentation
- Find similar enemy vulnerabilities across factions

**Considerations**:
- Requires embedding generation via LLM provider (already available in Mentor.Core)
- Qdrant has excellent .NET client and can run embedded
- Chroma simpler but requires Python interop
- Not a replacement for structured data, but complementary

**Recommendation**: **Qdrant** embedded mode - integrates well with existing LLM tooling, enables semantic "find similar" features that pure SQL can't match

---

## Rules & Reasoning Systems

### Rule Engine (Drools/.NET Options)

**Use Case**: Encoding game mechanics as executable rules

**Available .NET Options**:
- **NRules**: Forward-chaining, Rete algorithm, good for complex condition matching
- **RulesEngine** (Microsoft): JSON-based rules, simpler but less powerful
- Custom rule evaluator: Given existing tool architecture, may be simplest

**Strengths**:
- Declarative rule definition: "IF status_chance > 50 THEN recommend(status_mods)"
- Separation of rules from code (rules can be updated without recompilation)
- Conflict resolution when multiple rules fire
- Explanation capabilities (why was X recommended?)

**Example Rule Structure**:
```csharp
// Pseudocode for rule definition
Rule("High Status Weapons")
  .When(weapon => weapon.StatusChance > 50)
  .Then(context => context.Recommendations.Add(
    new ModCategory("Status", priority: Priority.High)))

Rule("Grineer Enemy Context")
  .When(context => context.EnemyFaction == Faction.Grineer)
  .Then(context => context.Recommendations.Add(
    new DamageType("Corrosive", multiplier: 1.5)))
```

**Considerations**:
- Rule interdependencies can become complex ("Razor mod makes status more valuable")
- Testing rule interactions requires careful setup
- May be overkill if rules are simple lookups

**Recommendation**: Start with **custom rule evaluator** as a Tool in Mentor.Core.Tools, graduate to NRules if complexity warrants

---

### Knowledge Graph + Ontology

**Use Case**: Formal representation of game domain knowledge

**Approach**:
- Define ontology: WeaponType, DamageType, StatusEffect, Synergy relationships
- Encode rules as graph structure rather than code
- Use OWL/RDF or simpler JSON-LD representation
- Reason over graph to infer implicit relationships

**Strengths**:
- Explicit domain modeling improves LLM reasoning
- Inference engines can derive new facts ("if A counters B and B is strong against C...")
- Documentation becomes the model

**Considerations**:
- Significant upfront modeling effort
- .NET libraries (dotNetRDF) available but less common
- May not add value over simpler approaches for this domain

**Recommendation**: Skip for v2 - graph database or simple relational model sufficient for explicit relationships; LLM handles implicit reasoning

---

### LLM-Based Planning (Current Architecture)

**Use Case**: Multi-step reasoning and query answering

**Current Capabilities**:
- `Mentor.Core` already has tool-based architecture
- LLM providers abstracted via `ILLMProviderFactory`
- Tools system in `Mentor.Core.Tools` (25 implementations)

**Specific Applications**:
```
User: "What's the best Cedo Prime build for Steel Path Grineer?"

Plan:
1. Query weapon stats for Cedo Prime (unique traits, base status chance)
2. Query enemy resistances for Steel Path Grineer factions
3. Query mods that boost status chance + corrosive damage
4. Apply rules: high status → stack status mods, Grineer → corrosive
5. Generate ranked recommendations
6. Explain reasoning
```

**New Tools Needed**:
- `QueryWeaponTool`: Retrieve weapon stats from database
- `QueryModsByTypeTool`: Filter mods by weapon compatibility and effect type
- `QueryEnemyResistancesTool`: Get faction-specific vulnerabilities
- `ApplyGameRulesTool`: Execute rule engine with context
- `RankBuildOptionsTool`: Score and order recommendations

**Considerations**:
- Planning approach scales well with complexity
- Each tool has single responsibility (SOLID compliance)
- LLM handles orchestration, tools provide reliable data access
- Explanation comes naturally from plan trace

**Recommendation**: Extend existing tool architecture - this is the core reasoning layer, other technologies support it

---

## Data Acquisition

### Web Scraping

**Use Case**: Extract data from Warframe wiki and community sites

**Technologies**:
- **AngleSharp** (.NET HTML parser): Parse wiki pages, extract tables/stats
- **Playwright-sharp**: For JavaScript-heavy sites requiring browser automation
- **Existing**: `SimpleHtmlToMarkdownConverter` in Mentor.Core can be extended

**Approach**:
```csharp
// Pseudocode
public class WikiScraper : IDataAcquisitionTool
{
    public async Task<WeaponData> ScrapeWeaponPage(string url)
    {
        var html = await _httpClient.GetStringAsync(url);
        var document = await _parser.ParseDocumentAsync(html);
        
        var statsTable = document.QuerySelector(".weapon-stats");
        return new WeaponData
        {
            Damage = ParseDamage(statsTable),
            StatusChance = ParseFloat(statsTable, "Status Chance"),
            FireRate = ParseFloat(statsTable, "Fire Rate"),
            // ...
        };
    }
}
```

**Considerations**:
- Wiki structure changes break scrapers
- Rate limiting and respectful crawling
- Data validation (sanity checks on parsed values)
- Caching to avoid repeated requests

---

### API Integration (warframe-items)

**Use Case**: Automated game data updates

**Source**: https://github.com/WFCD/warframe-items
- JSON API with comprehensive item data
- Includes images, drop rates, patch logs, riven dispositions
- Community-maintained, updated regularly
- RESTful access via `https://api.warframestat.us/items`

**Implementation**:
```csharp
public class WarframeItemsApiClient
{
    private readonly HttpClient _client;
    
    public async Task<List<Weapon>> FetchAllWeapons()
    {
        var response = await _client.GetFromJsonAsync<ItemsResponse>(
            "https://api.warframestat.us/items");
        
        return response.Items
            .Where(i => i.Category == "Primary" || i.Category == "Secondary")
            .Select(MapToWeapon)
            .ToList();
    }
}
```

**Update Strategy**:
- Scheduled background task (daily/weekly)
- Compare local DB version with API version
- Incremental updates (only changed items)
- Notification on breaking changes

**Considerations**:
- API rate limits
- Handle API downtime gracefully
- Local data must remain functional offline
- .NET's `System.Net.Http.Json` simplifies integration

---

### Hybrid Approach

**Recommendation**:
1. **Primary source**: warframe-items API (reliable, structured)
2. **Fallback/enrichment**: Wiki scraping for data not in API (specific synergies, advanced mechanics)
3. **Manual curation**: Rule definitions (too nuanced for automated extraction)
4. **Update cadence**: API check weekly, wiki scrape on major patches, rules updated as game evolves

---

## Architecture Recommendations

### Integration with Mentor.Core

**Existing Architecture Strengths**:
- Tool-based abstraction (`Mentor.Core.Tools`)
- Provider factory pattern (`ILLMProviderFactory`)
- Configuration repository (`IConfigurationRepository`)
- Export services (`IAnalysisExportService`)
- Strong serialization support (`JsonSerializerContext`)

**Proposed Extensions**:

#### 1. Data Layer
```
Mentor.Core.Data/
  ├── GameDatabase/
  │   ├── IGameDataRepository.cs
  │   ├── SqliteGameDataRepository.cs
  │   ├── Entities/
  │   │   ├── WeaponEntity.cs
  │   │   ├── ModEntity.cs
  │   │   └── WarframeEntity.cs
  │   └── Migrations/
  └── VectorStore/
      ├── ISemanticSearchService.cs
      └── QdrantSemanticSearchService.cs
```

#### 2. Rules Engine
```
Mentor.Core.Rules/
  ├── IRuleEngine.cs
  ├── SimpleRuleEngine.cs
  ├── Models/
  │   ├── Rule.cs
  │   ├── RuleContext.cs
  │   └── RuleResult.cs
  └── Definitions/
      ├── WeaponRules.json
      └── SynergyRules.json
```

#### 3. New Tools
```
Mentor.Core.Tools/
  ├── Game/
  │   ├── QueryWeaponTool.cs
  │   ├── QueryModTool.cs
  │   ├── QueryEnemyTool.cs
  │   ├── ApplyGameRulesTool.cs
  │   └── RankBuildTool.cs
  └── DataAcquisition/
      ├── WarframeItemsApiTool.cs
      └── WikiScraperTool.cs
```

#### 4. Service Layer
```
Mentor.Core.Services/
  ├── BuildOptimizationService.cs
  ├── GameDataUpdateService.cs
  └── BuildExportService.cs (extends IAnalysisExportService pattern)
```

---

### Tool-Based Approach Alignment

**Current Pattern** (from existing tools):
```csharp
public abstract class BaseTool : IDisposable
{
    protected readonly ILogger Logger;
    public abstract string Name { get; }
    public abstract Task<string> ExecuteAsync(string input);
}
```

**Warframe Tool Implementation**:
```csharp
public class QueryWeaponTool : BaseTool
{
    private readonly IGameDataRepository _repository;
    
    public override string Name => "query_weapon";
    
    public override async Task<string> ExecuteAsync(string weaponName)
    {
        var weapon = await _repository.GetWeaponByNameAsync(weaponName);
        if (weapon == null)
            return $"Weapon '{weaponName}' not found.";
            
        return JsonSerializer.Serialize(new
        {
            weapon.Name,
            weapon.Type,
            weapon.Damage,
            weapon.StatusChance,
            weapon.CriticalChance,
            weapon.UniqueTraits
        });
    }
}
```

**Benefits**:
- LLM can discover and use tools via `ToolFactory`
- Each tool testable in isolation
- Tools composable in multi-step plans
- Consistent error handling and logging

---

### Practical Implementation Considerations

#### Technology Stack Summary
| Component | Technology | Justification |
|-----------|-----------|---------------|
| Primary DB | SQLite | Portable, fast, .NET native support |
| Vector Store | Qdrant (embedded) | Semantic search, embeds in process |
| Rule Engine | Custom → NRules | Start simple, graduate if needed |
| Data Source | warframe-items API | Authoritative, maintained |
| Scraping | AngleSharp | Pure .NET, sufficient for wikis |
| LLM Orchestration | Existing Mentor.Core | Already proven architecture |

#### Development Phasing

**Phase 1: Data Foundation**
- Set up SQLite schema
- Implement warframe-items API client
- Create basic GameDataRepository
- Write migration/update service

**Phase 2: Core Tools**
- Implement QueryWeaponTool, QueryModTool, QueryEnemyTool
- Register with ToolFactory
- Test tool invocation via LLM

**Phase 3: Rules & Reasoning**
- Define initial rule set (JSON format)
- Implement SimpleRuleEngine
- Create ApplyGameRulesTool
- Test conditional recommendation logic

**Phase 4: Semantic Layer**
- Integrate Qdrant for embeddings
- Generate embeddings for all items/descriptions
- Implement similarity search tool
- Enable "find similar builds" queries

**Phase 5: Polish**
- Export service for build sharing
- UI integration (likely Mentor.Uno extensions)
- Comprehensive testing
- Documentation

#### Testing Strategy (TDD Compliance)

```csharp
// Example test-first approach
[Fact]
public async Task QueryWeaponTool_ReturnsCorrectStatusChance()
{
    // Arrange
    var mockRepo = new Mock<IGameDataRepository>();
    mockRepo.Setup(r => r.GetWeaponByNameAsync("Cedo Prime"))
        .ReturnsAsync(new WeaponEntity { StatusChance = 1.0 });
    var tool = new QueryWeaponTool(mockRepo.Object);
    
    // Act
    var result = await tool.ExecuteAsync("Cedo Prime");
    var weapon = JsonSerializer.Deserialize<WeaponData>(result);
    
    // Assert
    Assert.Equal(1.0, weapon.StatusChance);
}
```

#### .NET Version Compatibility
- Mentor.Core uses .NET 8 (per workspace rules)
- All proposed libraries compatible with .NET 8
- SQLite: `Microsoft.Data.Sqlite` (official)
- Qdrant: `Qdrant.Client` (community, active)
- AngleSharp: `AngleSharp` (mature, .NET Standard 2.0+)
- NRules: `NRules` (targets .NET 6+)

---

## Conclusion

The Warframe assistant system maps well onto the existing Mentor architecture with targeted extensions:

1. **SQLite** for structured game data (weapons, mods, frames)
2. **Qdrant** for semantic "similarity" queries
3. **Custom rule engine** starting simple, evolving as needed
4. **warframe-items API** as primary data source
5. **Tool-based approach** extending Mentor.Core.Tools pattern
6. **LLM planning** orchestrating multi-step reasoning

This approach:
- Leverages existing architectural patterns
- Minimizes new infrastructure
- Scales incrementally
- Maintains SOLID principles
- Supports TDD methodology
- Aligns with .NET 8 ecosystem

The system can start with basic lookup tools (Phase 1-2) and progressively add reasoning capabilities (Phase 3-4) as requirements clarify through user feedback.

