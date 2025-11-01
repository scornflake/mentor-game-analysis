# Automated Rule Discovery & Curation Pipeline

## Problem Statement

The Warframe assistant system requires encoding game mechanics as executable rules (e.g., "high status chance weapons benefit from status mods", "Grineer are vulnerable to corrosive damage"). However:

1. **Game complexity**: Hundreds of weapons, mods, frames with intricate interactions
2. **Continuous evolution**: Regular patches modify mechanics, add new items, rebalance existing ones
3. **Implicit knowledge**: Much expertise exists in community builds/discussions but isn't formally documented
4. **Manual curation doesn't scale**: Hand-authoring every rule is time-intensive and falls behind updates

**Solution**: Automated discovery of rules from multiple sources, combined with human-in-the-loop review before inclusion in the production knowledge base.

---

## Discovery Methods

### 1. Patch Notes Analysis

**Data Source**: Official Warframe update announcements, patch notes

**Extraction Approach**:
- Monitor official forums, Steam announcements, in-game news feed
- LLM analyzes patch note text for mechanical changes
- Pattern matching for common structures: "X increased from Y to Z", "Now scales with", "No longer affected by"

**Example**:
```
Patch Note: "Cedo Prime status chance increased from 80% to 100%"

Extracted Rule Candidate:
{
  "condition": "weapon.name == 'Cedo Prime' AND weapon.statusChance >= 1.0",
  "action": "recommend(status_mods, priority=HIGH)",
  "confidence": 0.95,
  "source": "patch_notes",
  "evidence": ["https://forums.warframe.com/topic/.../hotfix-3515"],
  "game_version": "35.15"
}
```

**Strengths**:
- High authority (official source)
- Timely (tracks game state accurately)
- Clear change detection

**Challenges**:
- Patch notes describe *changes*, not complete mechanics
- May not explain strategic implications
- Requires existing rule to update, not create new ones from scratch

**Confidence Scoring**: 0.9-1.0 (authoritative source)

---

### 2. Community Build Analysis

**Data Sources**:
- Overframe.gg (structured build database)
- Reddit r/Warframe build posts
- YouTube video descriptions
- Community Discord build channels

**Extraction Approach**:
- Scrape public builds for specific weapons/frames
- Aggregate mod usage statistics: "What % of Cedo Prime builds include Hunter Munitions?"
- Identify co-occurrence patterns: "Viral + Slash appears in 85% of high-level content builds"
- LLM analyzes associated commentary for reasoning

**Example**:
```
Data Collected:
- Cedo Prime builds analyzed: 247
- Hunter Munitions usage: 223/247 (90.3%)
- Viral damage usage: 231/247 (93.5%)
- Slash-focused builds: 215/247 (87.0%)

Generated Rule Candidate:
{
  "condition": "weapon.name == 'Cedo Prime' AND weapon.hasHighSlashWeight",
  "action": "recommend('Hunter Munitions', confidence=0.9)",
  "reasoning": "90% of community builds utilize slash proc synergy",
  "confidence": 0.85,
  "source": "community_builds",
  "evidence": ["overframe.gg/build/12345", "reddit.com/r/Warframe/..."],
  "sample_size": 247
}
```

**Strengths**:
- Discovers meta strategies in actual use
- Reflects collective player wisdom
- Identifies synergies that may not be documented

**Challenges**:
- Correlation ≠ causation (popularity doesn't prove effectiveness)
- Meta bias (temporary trends, not timeless mechanics)
- Skill-level context (Steel Path builds differ from normal content)
- Copycat effect (one popular guide can skew statistics)

**Confidence Scoring**: 0.6-0.85 (depends on sample size, consistency, and recency)

---

### 3. Wiki Content Extraction

**Data Sources**:
- Warframe Wiki (fandom.com)
- Community-maintained strategy guides
- Weapon/mod/frame detail pages

**Extraction Approach**:
- Parse structured sections: "Recommended Builds", "Synergies", "Strengths/Weaknesses"
- LLM reads prose descriptions and extracts logical rules
- Cross-reference with stat tables for validation

**Example**:
```
Wiki Text: "Cedo Prime's guaranteed status on alt-fire makes it exceptional 
for status-focused builds. The high base status chance allows stacking 
multiple status types effectively. Particularly effective against armored 
targets when built for corrosive/viral."

Extracted Rule Candidates:
1. {
     "condition": "weapon.name == 'Cedo Prime'",
     "action": "recommend(status_mods, reason='guaranteed alt-fire status')",
     "confidence": 0.8,
     "source": "wiki",
     "evidence": ["https://warframe.fandom.com/wiki/Cedo_Prime"]
   }

2. {
     "condition": "weapon.name == 'Cedo Prime' AND enemy.faction == 'Grineer'",
     "action": "recommend(['corrosive', 'viral'], priority=HIGH)",
     "confidence": 0.75,
     "source": "wiki"
   }
```

**Strengths**:
- Often explains *why* behind mechanics (not just *what*)
- Community-reviewed (popular wikis have editor oversight)
- Comprehensive coverage of items

**Challenges**:
- May be outdated (patches outpace wiki updates)
- Mix of verified mechanics and community opinion
- Inconsistent quality across pages
- Ambiguous language requiring LLM interpretation

**Confidence Scoring**: 0.7-0.9 (higher for recently edited pages, lower for stubs)

---

### 4. Simulation & Empirical Testing

**Data Source**: Game data files (if accessible via data mining), or controlled testing

**Extraction Approach**:
- Access damage calculation formulas from game files
- Simulate all mod combinations for a weapon
- Rank configurations by DPS, survivability, status application rate
- Generate rules from optimal configurations

**Example**:
```
Simulation Results for Cedo Prime vs. Level 175 Corrupted Heavy Gunner:
- Config A (4x Status + Viral + Slash): 15,240 DPS, 95% status/shot
- Config B (4x Crit + Hunter Munitions): 12,180 DPS, 45% status/shot
- Config C (Hybrid): 13,890 DPS, 72% status/shot

Generated Rule:
{
  "condition": "weapon.name == 'Cedo Prime' AND enemy.type == 'Heavy' AND enemy.level > 150",
  "action": "recommend(status_stacking, priority=HIGH)",
  "reasoning": "Simulation shows 25% higher DPS vs. crit build at this level",
  "confidence": 0.98,
  "source": "simulation",
  "test_parameters": {...}
}
```

**Strengths**:
- Ground truth from actual game math
- Comprehensive testing impossible for humans
- Deterministic results (reproducible)
- Can explore edge cases systematically

**Challenges**:
- Requires reverse-engineering game mechanics or data file access
- Complex to model all interactions (parkour, enemy AI, squad synergies)
- Simulation validity depends on accuracy of formula understanding
- May not capture "feel" factors humans value

**Confidence Scoring**: 0.95-0.99 (if formulas are verified; lower if assumptions required)

---

## Draft Mode & Review Workflow

### Draft Rule Data Model

```csharp
public class DraftRule
{
    public Guid RuleId { get; set; }
    public string RuleText { get; set; } // Human-readable description
    public string ConditionExpression { get; set; } // Executable condition
    public string ActionExpression { get; set; } // Executable action
    public RuleSource Source { get; set; } // PatchNotes, CommunityBuild, Wiki, Simulation
    public double ConfidenceScore { get; set; } // 0.0 - 1.0
    public List<EvidenceItem> Evidence { get; set; } // URLs, quotes, data supporting rule
    public RuleStatus Status { get; set; } // Draft, UnderReview, Approved, Rejected
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerUserId { get; set; }
    public string? ReviewerNotes { get; set; }
    public string GameVersion { get; set; } // Version when rule was discovered
    public int SampleSize { get; set; } // For community build rules
    public List<string> ConflictsWith { get; set; } // IDs of contradictory rules
}

public enum RuleSource
{
    PatchNotes,
    CommunityBuild,
    Wiki,
    Simulation,
    ManualEntry
}

public enum RuleStatus
{
    Draft,          // Newly discovered, awaiting review
    UnderReview,    // Reviewer is examining
    Approved,       // Promoted to production knowledge base
    Rejected,       // Determined to be incorrect/low-quality
    Expired         // Obsolete due to game updates
}

public class EvidenceItem
{
    public string SourceUrl { get; set; }
    public string QuoteText { get; set; }
    public DateTime AccessedAt { get; set; }
}
```

---

### Review Workflow

**Step 1: Automated Quality Gates**

```csharp
public class RuleQualityGate
{
    public async Task<RuleStatus> EvaluateNewRule(DraftRule rule)
    {
        // Automatic rejection
        if (rule.ConfidenceScore < 0.3)
            return RuleStatus.Rejected;
        
        // Check for conflicts with approved rules
        var conflicts = await _repository.FindConflictingRulesAsync(rule);
        if (conflicts.Any())
        {
            rule.ConflictsWith = conflicts.Select(c => c.RuleId.ToString()).ToList();
            return RuleStatus.UnderReview; // Force human decision
        }
        
        // Automatic approval (high confidence + authoritative source)
        if (rule.ConfidenceScore > 0.9 && rule.Source == RuleSource.PatchNotes)
            return RuleStatus.Approved;
        
        // Everything else requires human review
        return RuleStatus.UnderReview;
    }
}
```

**Step 2: Reviewer Dashboard**

UI displays pending rules sorted by:
- Priority: Conflicts > High Confidence > Older drafts
- Confidence score (descending)
- Game version (recent patches first)

**For each rule, reviewer sees**:
1. **Rule Description**: Natural language explanation
2. **Formal Logic**: Condition and action expressions
3. **Evidence Panel**:
   - Clickable links to sources
   - Quoted text supporting the rule
   - Sample size (for community data)
4. **Confidence Score**: With breakdown by factor
5. **Similar Existing Rules**: To catch redundancy
6. **Conflict Warnings**: If contradicts approved rules
7. **Discovery Context**: When discovered, which agent, game version

**Step 3: Review Actions**

```csharp
public interface IRuleReviewService
{
    Task ApproveRuleAsync(Guid ruleId, string reviewerNotes = null);
    Task RejectRuleAsync(Guid ruleId, string reason);
    Task EditAndApproveAsync(Guid ruleId, DraftRule updatedRule, string notes);
    Task RequestMoreEvidenceAsync(Guid ruleId, string specificRequest);
    Task FlagForCommunityVoteAsync(Guid ruleId); // Delegate to community
}
```

**Actions**:
- ✅ **Approve**: Promote to production, index in RAG, mark as reviewed
- ❌ **Reject**: Archive with reason, use to train confidence models
- ✏️ **Edit & Approve**: Modify rule logic/wording, then approve
- 🔍 **Request More Evidence**: Trigger re-scraping or simulation
- 👥 **Community Vote**: For borderline cases, open to trusted users

**Step 4: Promotion to Production**

```csharp
public class RulePromotionService
{
    public async Task PromoteToProductionAsync(DraftRule draftRule)
    {
        // 1. Convert to production rule format
        var productionRule = new ProductionRule
        {
            RuleId = draftRule.RuleId,
            Condition = ParseCondition(draftRule.ConditionExpression),
            Action = ParseAction(draftRule.ActionExpression),
            Provenance = new RuleProvenance
            {
                Source = draftRule.Source,
                DiscoveredAt = draftRule.CreatedAt,
                ApprovedAt = DateTime.UtcNow,
                Evidence = draftRule.Evidence
            }
        };
        
        // 2. Add to rule engine
        await _ruleEngine.AddRuleAsync(productionRule);
        
        // 3. Generate embedding and index in vector DB
        var embedding = await _embeddingService.GenerateAsync(
            $"{draftRule.RuleText} {draftRule.ConditionExpression}"
        );
        await _vectorDb.IndexAsync(new RuleEmbedding
        {
            RuleId = productionRule.RuleId.ToString(),
            Vector = embedding,
            Metadata = new Dictionary<string, object>
            {
                ["status"] = "approved",
                ["confidence"] = draftRule.ConfidenceScore,
                ["source"] = draftRule.Source.ToString(),
                ["game_version"] = draftRule.GameVersion
            }
        });
        
        // 4. Update draft status
        draftRule.Status = RuleStatus.Approved;
        await _draftRepository.UpdateAsync(draftRule);
    }
}
```

---

## RAG Integration Strategy

### Two-Tier Knowledge Base

**Tier 1: Production Rules** (High Trust)
- Only approved rules included
- Full trust for automated recommendations
- Highest retrieval priority in vector search
- Include provenance metadata for transparency

**Tier 2: Draft Rules** (Experimental Context)
- Indexed separately with lower retrieval priority
- Surfaced with "UNVERIFIED" or "EXPERIMENTAL" prefix
- Used only when:
  - User explicitly enables "experimental mode"
  - Production rules insufficient to answer query
  - Presented as suggestions, not recommendations

### Vector Database Schema

```csharp
public class RuleEmbedding
{
    public string RuleId { get; set; }
    public float[] Vector { get; set; } // 1536-dim for OpenAI, varies by model
    public Dictionary<string, object> Metadata { get; set; }
}

// Metadata fields for filtering
public class RuleMetadata
{
    public string Status { get; set; } // "approved" | "draft"
    public double Confidence { get; set; }
    public string Source { get; set; }
    public string GameVersion { get; set; }
    public DateTime IndexedAt { get; set; }
    public string[] Tags { get; set; } // ["weapon", "status", "grineer"]
}
```

### Retrieval Logic

```csharp
public class HybridRuleRetrieval
{
    public async Task<List<Rule>> RetrieveRelevantRulesAsync(
        string userQuery,
        bool includeExperimental = false)
    {
        // Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateAsync(userQuery);
        
        // Search production rules (always)
        var productionResults = await _vectorDb.SearchAsync(
            vector: queryEmbedding,
            filter: new { status = "approved" },
            limit: 10
        );
        
        // Optionally include draft rules
        List<RuleEmbedding> draftResults = null;
        if (includeExperimental)
        {
            draftResults = await _vectorDb.SearchAsync(
                vector: queryEmbedding,
                filter: new { status = "draft", confidence = new { $gte = 0.7 } },
                limit: 5
            );
        }
        
        return FormatResults(productionResults, draftResults);
    }
    
    private List<Rule> FormatResults(
        List<RuleEmbedding> production,
        List<RuleEmbedding>? drafts)
    {
        var results = production.Select(r => new Rule
        {
            Id = r.RuleId,
            Text = r.Metadata["text"].ToString(),
            Trust = TrustLevel.Verified,
            Source = r.Metadata["source"].ToString()
        }).ToList();
        
        if (drafts != null)
        {
            results.AddRange(drafts.Select(r => new Rule
            {
                Id = r.RuleId,
                Text = $"⚠️ EXPERIMENTAL: {r.Metadata["text"]}",
                Trust = TrustLevel.Unverified,
                Source = r.Metadata["source"].ToString(),
                Confidence = (double)r.Metadata["confidence"]
            }));
        }
        
        return results;
    }
}
```

### LLM Tool Integration

```csharp
public class ApplyGameRulesTool : BaseTool
{
    public override string Name => "apply_game_rules";
    
    public override async Task<string> ExecuteAsync(string context)
    {
        var parsedContext = JsonSerializer.Deserialize<GameContext>(context);
        
        // Retrieve relevant rules
        var rules = await _retrieval.RetrieveRelevantRulesAsync(
            userQuery: parsedContext.Query,
            includeExperimental: parsedContext.ExperimentalMode
        );
        
        // Execute production rules
        var recommendations = new List<Recommendation>();
        foreach (var rule in rules.Where(r => r.Trust == TrustLevel.Verified))
        {
            if (rule.Condition.Evaluate(parsedContext))
            {
                recommendations.Add(rule.Action.Execute(parsedContext));
            }
        }
        
        // Format experimental insights separately
        var experimentalInsights = rules
            .Where(r => r.Trust == TrustLevel.Unverified)
            .Select(r => $"💡 {r.Text} (confidence: {r.Confidence:P0})")
            .ToList();
        
        return JsonSerializer.Serialize(new
        {
            Recommendations = recommendations,
            ExperimentalInsights = experimentalInsights
        });
    }
}
```

---

## Continuous Learning Loop

### Feedback Collection

**Implicit Signals**:
- User accepts/rejects generated recommendations
- Time spent viewing a recommendation
- Click-through to evidence links
- User edits recommended builds (what changed?)

**Explicit Signals**:
- Upvote/downvote on individual recommendations
- "Report incorrect rule" button
- User-submitted corrections

**Effectiveness Tracking**:
```csharp
public class RuleFeedbackService
{
    public async Task TrackRuleApplicationAsync(RuleApplicationEvent evt)
    {
        await _repository.SaveAsync(new RuleFeedback
        {
            RuleId = evt.RuleId,
            RecommendationId = evt.RecommendationId,
            UserAction = evt.Action, // Accepted, Rejected, Modified, Ignored
            ContextSnapshot = evt.Context, // What query/situation
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task<RuleEffectivenessReport> AnalyzeRulePerformanceAsync(Guid ruleId)
    {
        var feedback = await _repository.GetFeedbackForRuleAsync(ruleId);
        
        return new RuleEffectivenessReport
        {
            RuleId = ruleId,
            TimesApplied = feedback.Count,
            AcceptanceRate = feedback.Count(f => f.UserAction == UserAction.Accepted) / (double)feedback.Count,
            RejectionRate = feedback.Count(f => f.UserAction == UserAction.Rejected) / (double)feedback.Count,
            AverageConfidence = feedback.Average(f => f.ConfidenceAtTime),
            RecommendedAction = DetermineAction(feedback)
        };
    }
    
    private RuleAction DetermineAction(List<RuleFeedback> feedback)
    {
        var rejectionRate = feedback.Count(f => f.UserAction == UserAction.Rejected) / (double)feedback.Count;
        
        if (rejectionRate > 0.5)
            return RuleAction.FlagForReview; // High rejection, may be wrong
        
        if (rejectionRate > 0.3)
            return RuleAction.LowerConfidence; // Somewhat unreliable
        
        return RuleAction.NoChange;
    }
}
```

### Automated Re-evaluation

**Trigger Conditions**:
1. **Game update detected**: All rules tagged with older version → queue for verification
2. **Low acceptance rate**: Rules with <40% acceptance after 20+ applications → flag for review
3. **Age-based**: Rules >6 months old without feedback → re-scrape evidence sources
4. **Conflict detection**: New rule approved that contradicts existing → both flagged

**Re-evaluation Process**:
```csharp
public class RuleRevalidationService
{
    public async Task RevalidateRuleAsync(Guid ruleId)
    {
        var rule = await _repository.GetRuleAsync(ruleId);
        
        // Re-scrape original evidence sources
        var currentEvidence = await _discoveryService.RecheckEvidenceAsync(rule.Evidence);
        
        // Check if evidence still supports rule
        var validationResult = await _llmService.ValidateRuleAgainstEvidenceAsync(
            rule, currentEvidence
        );
        
        if (validationResult.Confidence < 0.5)
        {
            // Evidence no longer supports rule
            rule.Status = RuleStatus.Expired;
            await _repository.UpdateAsync(rule);
            
            // Remove from production RAG
            await _vectorDb.DeleteAsync(rule.RuleId.ToString());
            
            // Notify reviewer
            await _notificationService.NotifyRuleExpiredAsync(rule);
        }
        else if (validationResult.Confidence < rule.OriginalConfidence - 0.2)
        {
            // Confidence dropped significantly
            rule.ConfidenceScore = validationResult.Confidence;
            rule.Status = RuleStatus.UnderReview;
            await _repository.UpdateAsync(rule);
        }
    }
}
```

### Learning from Rejections

**Pattern Mining**:
- Analyze rejected draft rules for common characteristics
- Update confidence models: "Rules from Source X with Confidence > Y have 80% approval rate"
- Adjust quality gates based on historical performance

**Example**:
```
Analysis after 1000 reviewed rules:
- PatchNotes source: 95% approval rate
- Wiki source (edited <30 days ago): 85% approval rate
- Wiki source (edited >180 days ago): 45% approval rate
- CommunityBuild (sample < 50): 40% approval rate
- CommunityBuild (sample > 200): 75% approval rate

Action: Adjust confidence scoring:
- Wiki rules: apply 0.5x multiplier if last edit >180 days
- Community rules: require sample_size > 100 for confidence > 0.7
```

---

## Practical Concerns

### Advantages

✅ **Scalability**: System keeps pace with game updates automatically
✅ **Human Oversight**: Review workflow prevents hallucinated rules
✅ **Transparency**: Provenance tracking enables trust and debugging
✅ **Continuous Improvement**: Feedback loop refines discovery quality over time
✅ **Multi-Source Validation**: Confidence increases when sources agree
✅ **Temporal Awareness**: Rules tracked against game versions, expire when outdated

### Challenges

⚠️ **Extraction Quality**: LLM interpretation of ambiguous text may be incorrect
⚠️ **Reviewer Availability**: Requires active moderation (backlog risk)
⚠️ **Source Reliability**: Community data may reflect meta trends, not mechanics
⚠️ **Cultural Bias**: English-only sources miss international community insights
⚠️ **Temporal Validity**: Rules from 6 months ago may be obsolete but not flagged
⚠️ **Complexity Cascade**: More rules increase conflict risk and review overhead

### Mitigations

**Multi-Source Validation**:
- Require 2+ independent sources for high confidence
- Cross-reference patch notes + wiki + community builds
- Flag rules supported by single source only

**Community-Driven Review**:
- Allow trusted users to vote on draft rules (Wikipedia model)
- Reputation system: users who consistently identify good/bad rules earn higher weight
- Distribute review load across community, not single admin

**Expiration Policies**:
- Draft rules older than 30 days without review → auto-archive
- Approved rules tagged with game version → revalidate on major patches
- Unused rules (never applied in recommendations for 90 days) → flag for relevance check

**Adversarial Validation**:
- LLM generates counter-examples: "Does this rule still hold if status_chance = 0.1?"
- Simulation testing for rules that claim optimization
- A/B testing in recommendations (show rule-based vs. baseline, track acceptance)

**Conflict Resolution**:
- When new rule conflicts with existing, surface both to reviewer
- Reviewer sees diff: "Old rule says X, new rule says Y, evidence shows..."
- Option to deprecate old rule, merge rules, or reject new rule

**Version Tracking**:
```csharp
public class GameVersionTracker
{
    public async Task OnNewPatchDetectedAsync(string newVersion)
    {
        // Get all rules created before this patch
        var potentiallyAffectedRules = await _repository.GetRulesAsync(
            filter: r => r.GameVersion != newVersion && r.Status == RuleStatus.Approved
        );
        
        // Queue for revalidation
        foreach (var rule in potentiallyAffectedRules)
        {
            await _revalidationQueue.EnqueueAsync(rule.RuleId);
        }
        
        // Notify reviewers
        await _notificationService.NotifyPatchDetectedAsync(
            newVersion,
            affectedRuleCount: potentiallyAffectedRules.Count
        );
    }
}
```

---

## Implementation Architecture

### New Components

```
Mentor.Core.RuleDiscovery/
  ├── IDiscoveryAgent.cs
  ├── Agents/
  │   ├── PatchNotesDiscoveryAgent.cs
  │   ├── CommunityBuildDiscoveryAgent.cs
  │   ├── WikiContentDiscoveryAgent.cs
  │   └── SimulationDiscoveryAgent.cs
  ├── RuleCurationService.cs
  ├── RuleQualityGate.cs
  ├── RulePromotionService.cs
  └── RuleRevalidationService.cs

Mentor.Core.Data/
  ├── DraftRulesRepository.cs
  ├── RuleFeedbackRepository.cs
  └── Entities/
      ├── DraftRuleEntity.cs
      └── RuleFeedbackEntity.cs

Mentor.Core.Services/
  ├── RuleReviewService.cs
  └── RuleFeedbackService.cs

Mentor.Uno/ (UI Components)
  ├── Views/
  │   ├── RuleReviewPage.xaml
  │   └── RuleFeedbackWidget.xaml
  └── ViewModels/
      ├── RuleReviewViewModel.cs
      └── RuleFeedbackViewModel.cs
```

### Discovery Agent Interface

```csharp
public interface IDiscoveryAgent
{
    string AgentName { get; }
    RuleSource SourceType { get; }
    
    Task<List<DraftRule>> DiscoverRulesAsync(DiscoveryContext context);
    Task<bool> ValidateEvidenceAsync(EvidenceItem evidence);
}

public class PatchNotesDiscoveryAgent : IDiscoveryAgent
{
    private readonly ILLMClient _llmClient;
    private readonly IWebSearchTool _searchTool;
    
    public string AgentName => "Patch Notes Analyzer";
    public RuleSource SourceType => RuleSource.PatchNotes;
    
    public async Task<List<DraftRule>> DiscoverRulesAsync(DiscoveryContext context)
    {
        // 1. Fetch recent patch notes
        var patchNotes = await FetchRecentPatchNotesAsync();
        
        // 2. Extract changes via LLM
        var extractionPrompt = BuildExtractionPrompt(patchNotes);
        var response = await _llmClient.GenerateAsync(extractionPrompt);
        
        // 3. Parse LLM response into structured rules
        var rules = ParseExtractedRules(response);
        
        // 4. Set confidence and metadata
        foreach (var rule in rules)
        {
            rule.Source = SourceType;
            rule.ConfidenceScore = CalculateConfidence(rule);
            rule.Evidence = BuildEvidenceList(patchNotes);
            rule.GameVersion = context.CurrentGameVersion;
        }
        
        return rules;
    }
    
    private double CalculateConfidence(DraftRule rule)
    {
        // Patch notes are authoritative, high confidence
        return 0.95;
    }
}
```

### Review Service Interface

```csharp
public interface IRuleReviewService
{
    Task<List<DraftRule>> GetPendingReviewsAsync(ReviewFilter filter);
    Task<DraftRule> GetRuleDetailsAsync(Guid ruleId);
    Task<List<ProductionRule>> FindSimilarRulesAsync(DraftRule draftRule);
    Task<List<DraftRule>> FindConflictingRulesAsync(DraftRule draftRule);
    
    Task ApproveRuleAsync(Guid ruleId, string reviewerUserId, string notes = null);
    Task RejectRuleAsync(Guid ruleId, string reviewerUserId, string reason);
    Task EditAndApproveAsync(Guid ruleId, DraftRule updatedRule, string reviewerUserId);
    Task RequestMoreEvidenceAsync(Guid ruleId, string specificRequest);
    Task FlagForCommunityVoteAsync(Guid ruleId);
}
```

### Tool for Accessing Draft Rules

```csharp
public class DraftRuleLookupTool : BaseTool
{
    private readonly IDraftRulesRepository _repository;
    
    public override string Name => "lookup_experimental_rules";
    
    public override async Task<string> ExecuteAsync(string input)
    {
        var context = JsonSerializer.Deserialize<GameContext>(input);
        
        // Only retrieve high-confidence drafts
        var drafts = await _repository.GetDraftRulesAsync(
            filter: r => r.ConfidenceScore > 0.7 && r.Status == RuleStatus.Draft,
            orderBy: r => r.ConfidenceScore,
            descending: true,
            limit: 5
        );
        
        if (!drafts.Any())
            return "No experimental rules found for this context.";
        
        var formatted = new StringBuilder();
        formatted.AppendLine("⚠️ EXPERIMENTAL INSIGHTS (unverified by reviewers):");
        formatted.AppendLine();
        
        foreach (var draft in drafts)
        {
            formatted.AppendLine($"💡 {draft.RuleText}");
            formatted.AppendLine($"   Confidence: {draft.ConfidenceScore:P0}");
            formatted.AppendLine($"   Source: {draft.Source}");
            formatted.AppendLine($"   Evidence: {draft.Evidence.First().SourceUrl}");
            formatted.AppendLine();
        }
        
        formatted.AppendLine("Note: These are community-derived insights, not officially reviewed.");
        
        return formatted.ToString();
    }
}
```

---

## Development Phasing

### Phase 1: Foundation (Weeks 1-2)
- Define `DraftRule` data model and database schema
- Implement `DraftRulesRepository`
- Create basic `RuleQualityGate` with threshold checks
- Build simple CLI review tool (approve/reject from terminal)

### Phase 2: Discovery Agents (Weeks 3-4)
- Implement `PatchNotesDiscoveryAgent` (highest ROI)
- Test extraction quality on historical patch notes
- Implement `WikiContentDiscoveryAgent`
- Set up scheduled tasks to run agents daily/weekly

### Phase 3: Review Workflow (Weeks 5-6)
- Build Mentor.Uno UI for rule review
- Implement conflict detection
- Add "similar rules" comparison
- Create bulk approval/rejection features

### Phase 4: RAG Integration (Weeks 7-8)
- Index draft rules in separate Qdrant collection
- Implement metadata filtering (approved vs. draft)
- Create `DraftRuleLookupTool`
- Test hybrid retrieval (production + experimental mode)

### Phase 5: Feedback Loop (Weeks 9-10)
- Implement `RuleFeedbackService`
- Add user feedback UI (upvote/downvote)
- Build effectiveness analytics dashboard
- Implement automated revalidation triggers

### Phase 6: Advanced Discovery (Weeks 11-12)
- Implement `CommunityBuildDiscoveryAgent` (requires scraping infrastructure)
- Add `SimulationDiscoveryAgent` (if feasible)
- Multi-source validation logic
- Confidence model tuning based on historical approval rates

---

## Success Metrics

**Discovery Quality**:
- % of draft rules that pass review (target: >60% for patch notes, >40% for community)
- Time from game update to rule discovery (target: <24 hours for patch notes)
- Duplicate detection rate (avoid re-discovering existing rules)

**Review Efficiency**:
- Average time to review a rule (target: <5 minutes)
- Backlog size (target: <50 pending rules)
- Inter-reviewer agreement (if multiple reviewers)

**Production Impact**:
- % of recommendations backed by at least one rule (target: >80%)
- Rule application rate (rules used in recommendations vs. never used)
- User acceptance rate of rule-based recommendations (target: >70%)

**Freshness**:
- % of approved rules validated within last 90 days
- Time lag between patch release and rule updates (target: <7 days)
- % of rules with evidence sources still accessible (target: >95%)

---

## Conclusion

Automated rule discovery with human-in-the-loop curation transforms the Warframe assistant from a **static knowledge system** into a **living, evolving knowledge base** that:

1. **Scales beyond manual authoring** through multi-source discovery
2. **Maintains quality** via review workflow and confidence scoring
3. **Stays current** with automated revalidation and version tracking
4. **Learns continuously** from user feedback and effectiveness metrics
5. **Builds trust** through transparency (provenance, evidence, experimental flags)

The draft mode is crucial: it enables aggressive discovery (high recall) while maintaining production quality (high precision) through the review gate. Users benefit from both verified recommendations and optional experimental insights.

**Recommended Priority**: Implement in Phase 3-4 of v2 development, after core data infrastructure and basic rule engine are functional. Start with patch notes discovery (highest signal-to-noise), then expand to other sources as the system matures.

