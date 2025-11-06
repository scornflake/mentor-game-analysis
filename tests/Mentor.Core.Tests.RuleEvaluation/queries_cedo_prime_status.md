# Sample Queries: Finding Mods that Improve Status for Cedo Prime

These queries use the new graph model with Mod -> Effect -> Stat relationships.

## Basic Query: Find Mods that Increase Status Chance

```cypher
// Find all PRIMARY mods that increase Status Chance (for Cedo Prime)
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase" 
  AND e.value > 0
RETURN m.name AS modName, 
       e.value AS value, 
       e.unit AS unit,
       e.condition AS condition,
       e.originalText AS effectText
ORDER BY e.value DESC
```

## Query: Find Mods with Conditional Status Effects

```cypher
// Find PRIMARY mods that increase Status Chance with conditions (e.g., "On Kill", "On Status Effect")
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase" 
  AND e.condition IS NOT NULL 
  AND e.condition <> "none"
RETURN m.name AS modName,
       e.value AS value,
       e.unit AS unit,
       e.condition AS condition,
       e.duration AS duration,
       e.stacking AS stacking,
       e.maxStacks AS maxStacks,
       e.originalText AS effectText
ORDER BY e.value DESC
```

## Query: Find Mods with Stacking Status Effects

```cypher
// Find PRIMARY mods that have stacking status chance effects
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.stacking = true
RETURN m.name AS modName,
       e.value AS value,
       e.unit AS unit,
       e.condition AS condition,
       e.maxStacks AS maxStacks,
       e.stackType AS stackType,
       e.originalText AS effectText
ORDER BY e.maxStacks DESC, e.value DESC
```

## Query: Find Mods that Improve Status for Cedo Prime (Weapon-Specific)

```cypher
// Find PRIMARY mods that increase Status Chance for Cedo Prime
// Note: Cedo Prime is a PRIMARY weapon, so we filter for PRIMARY mods
MATCH (w:Weapon {name: "Cedo Prime"})
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase" 
  AND e.value > 0
RETURN m.name AS modName,
       m.type AS modType,
       e.value AS statusIncrease,
       e.unit AS unit,
       e.condition AS condition,
       e.originalText AS effectText
ORDER BY e.value DESC
```

## Query: Find All Status-Related Mods (Status Chance + Status Duration)

```cypher
// Find PRIMARY mods that improve any status-related stat
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat)
WHERE m.type = "PRIMARY"
  AND s.category = "status"
  AND e.type = "increase"
  AND e.value > 0
RETURN m.name AS modName,
       s.name AS statName,
       e.value AS value,
       e.unit AS unit,
       e.condition AS condition,
       e.originalText AS effectText
ORDER BY s.name, e.value DESC
```

## Query: Find Mods with Multiple Status Effects

```cypher
// Find PRIMARY mods that have multiple effects related to status
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat)
WHERE m.type = "PRIMARY"
  AND s.category = "status"
  AND e.type = "increase"
WITH m, collect(DISTINCT s.name) AS statusStats, count(DISTINCT e) AS effectCount
WHERE effectCount > 1
RETURN m.name AS modName,
       m.type AS modType,
       statusStats AS affectedStats,
       effectCount AS numberOfEffects
ORDER BY effectCount DESC
```

## Query: Find Best Status Mods by Rank

```cypher
// Find PRIMARY mods with highest status chance increase at max rank
MATCH (m:Mod)-[r:HAS_EFFECT {rank: m.fusionLimit}]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase" 
  AND e.value > 0
RETURN m.name AS modName,
       m.fusionLimit AS maxRank,
       e.value AS maxRankValue,
       e.unit AS unit,
       e.condition AS condition,
       e.originalText AS effectText
ORDER BY e.value DESC
LIMIT 20
```

## Query: Find Mods with Per-Unit Status Effects

```cypher
// Find PRIMARY mods that have per-unit modifiers (e.g., "per Status Type")
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase"
  AND e.perUnit IS NOT NULL
RETURN m.name AS modName,
       e.value AS value,
       e.unit AS unit,
       e.perUnit AS perUnit,
       e.condition AS condition,
       e.originalText AS effectText
ORDER BY e.value DESC
```

## Query: Find Mods with Duration-Based Status Effects

```cypher
// Find PRIMARY mods that have temporary status chance increases
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase"
  AND e.duration IS NOT NULL
RETURN m.name AS modName,
       e.value AS value,
       e.unit AS unit,
       e.condition AS condition,
       e.duration AS durationSeconds,
       e.stacking AS stacking,
       e.originalText AS effectText
ORDER BY e.duration DESC, e.value DESC
```

## Query: Comprehensive Status Mod Search for Cedo Prime

```cypher
// Comprehensive query: Find all status-related mods with full details
// Filtered to PRIMARY mods only (for Cedo Prime)
MATCH (m:Mod)-[r:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat)
WHERE m.type = "PRIMARY"
  AND s.category = "status"
  AND e.type = "increase"
  AND e.value > 0
WITH m, e, s, r, 
     CASE 
       WHEN e.condition = "none" THEN "Permanent"
       ELSE "Conditional: " + e.condition
     END AS effectType
RETURN m.name AS modName,
       m.type AS modType,
       m.rarity AS rarity,
       s.name AS statName,
       e.value AS value,
       e.unit AS unit,
       effectType AS effectType,
       e.duration AS durationSeconds,
       e.stacking AS stacking,
       e.maxStacks AS maxStacks,
       r.rank AS rank,
       e.originalText AS effectText
ORDER BY s.name, e.value DESC
```

## Query: Find Mods by Operation Type

```cypher
// Find PRIMARY mods that use multiplicative vs additive operations for status
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase"
RETURN m.name AS modName,
       e.operation AS operation,
       e.value AS value,
       e.unit AS unit,
       CASE 
         WHEN e.operation = "multiplicative" THEN "Multiplies base stat"
         ELSE "Adds to base stat"
       END AS operationDescription,
       e.originalText AS effectText
ORDER BY e.operation, e.value DESC
```

## Query: Find Mods with Complex Status Effects

```cypher
// Find PRIMARY mods with complex status effects (multiple conditions, stacking, duration)
MATCH (m:Mod)-[:HAS_EFFECT]->(e:Effect)-[:MODIFIES]->(s:Stat {name: "Status Chance"})
WHERE m.type = "PRIMARY"
  AND e.type = "increase"
  AND (
    (e.condition IS NOT NULL AND e.condition <> "none") OR
    e.stacking = true OR
    e.duration IS NOT NULL OR
    e.perUnit IS NOT NULL
  )
RETURN m.name AS modName,
       e.value AS value,
       e.unit AS unit,
       e.condition AS condition,
       e.duration AS duration,
       e.stacking AS stacking,
       e.maxStacks AS maxStacks,
       e.perUnit AS perUnit,
       e.originalText AS effectText
ORDER BY 
  CASE 
    WHEN e.stacking = true THEN 1
    WHEN e.condition IS NOT NULL AND e.condition <> "none" THEN 2
    ELSE 3
  END,
  e.value DESC
```
