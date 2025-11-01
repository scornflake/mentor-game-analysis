# Rule Augmentation Impact Report

**Generated:** 2025-11-01 11:57:02
**Provider:** Perplexity
**Total Comparisons:** 1

## Executive Summary

- **Comparisons with Improvement:** 1 (100%)
- **Comparisons with Degradation:** 0 (0%)
- **Neutral Comparisons:** 0 (0%)

**Overall Improvement Score:** 0.067 📈 Moderate Improvement

## Aggregate Metrics

*Subjective evaluations performed on 1 of 1 comparisons*

| Metric | Mean Delta | Median Delta | Interpretation |
|--------|------------|--------------|----------------|
| Specificity | 0.000 | 0.000 | Negligible impact |
| Terminology | 0.000 | 0.000 | Negligible impact |
| Actionability | 0.200 | 0.200 | Strong positive impact |
| **Subjective (0-10)** | **0.00** | **0.00** | **Negligible change** |

## Per-Screenshot Results

### acceltra prime rad build.png

**Overall Improvement:** 0.067 📈 Moderate Improvement

| Metric | Baseline | Rule-Augmented | Delta |
|--------|----------|----------------|-------|
| Specificity | 2.200 | 2.200 | 0.000 |
| Terminology | 1.000 | 1.000 | 0.000 |
| Actionability | 0.800 | 1.000 | **+0.200** ↑ |
| Confidence | 0.950 | 0.980 | +0.030 |
| Rec. Count | 5 | 5 | +0 |
| Duration | 26.9s | 38.4s | 11.5s |

#### Subjective Evaluation

**Baseline Score:** 8.5/10
**Rule-Augmented Score:** 8.5/10
**Delta:** 0.0 ⚪ Neutral

**Evaluation Details:**

1. **Should recommend corrosive or viral damage types for Grineer armor in Steel Path** (weight: 1)
   - **Baseline:** 10.0/10 - The recommendation explicitly and strongly advocates for Viral damage (Toxin + Cold) as the primary damage type for Grineer in Steel Path. It clearly states 'Swap elemental mods to Viral' and explains that 'Viral increases damage to health' which is optimal for armored Grineer. This is a high-priority recommendation with detailed mod suggestions (Infected Clip + Cryo Rounds).
   - **Rule-Aug:** 10.0/10 - The recommendation strongly advocates for Viral damage (explicitly stating 'Viral multiplies all damage by up to 325%') and mentions Corrosive as a comparison point. The primary recommendation is to replace Radiation with Viral + Heat, which directly addresses Grineer armor in Steel Path. Viral is correctly identified as the optimal damage type for this content.

2. **Should NOT recommend fire or blast damage as primary damage types** (weight: 0.8)
   - **Baseline:** 3.0/10 - The recommendation fails this criterion significantly. While it correctly identifies that Radiation is suboptimal, it actively recommends Heat as part of the elemental combo (Viral + Heat), stating 'add Heat for crowd control and armor stripping' and 'Heat strips armor over time.' Heat is recommended in the first high-priority suggestion alongside Viral, making it a core part of the build strategy rather than avoiding it.
   - **Rule-Aug:** 3.0/10 - The recommendation actually promotes Heat (fire) as a primary damage type alongside Viral, stating 'Heat strips armor while providing damage-over-time' and recommending it in multiple places (Recommendations 1 and 3). While Heat has utility for armor stripping, the criterion expects fire damage NOT to be recommended as a primary damage type. The recommendation treats Heat as a core component of the build strategy, which contradicts the expected negative stance.

3. **Should mention specific mod names (e.g., Hunter Munitions, Galvanized mods)** (weight: 1)
   - **Baseline:** 10.0/10 - The recommendation extensively mentions specific mod names throughout: Hunter Munitions, Serration, Point Strike, Vital Sense, Speed Trigger, Galvanized Chamber, Stormbringer, Wildfire, Hellfire, Infected Clip, Cryo Rounds, Primary Merciless, Vigilante Supplies, and Vigilante Offense. Each recommendation includes concrete mod names with clear reasoning for their inclusion or replacement.
   - **Rule-Aug:** 10.0/10 - The recommendation extensively mentions specific mod names including Hunter Munitions, Galvanized Chamber, Galvanized Aptitude, Point Strike, Vital Sense, Serration, Malignant Force, Infected Clip, Cryo Rounds, Hellfire, Speed Trigger, Rime Rounds, Vigilante Supplies, and Primed Rifle Ammo Mutation. This demonstrates thorough knowledge of the modding system and provides actionable, specific guidance.

4. **Should consider Steel Path enemy scaling and armor mechanics** (weight: 1)
   - **Baseline:** 10.0/10 - The recommendation demonstrates deep understanding of Steel Path armor mechanics. It explicitly discusses armor stripping, mentions that 'Grineer have high armor,' explains the importance of Slash procs for 'bypassing Grineer armor,' discusses health-bypassing damage, references 'tough Grineer eximus and heavy gunners,' and explains how Hunter Munitions works to bypass armor. The entire analysis is framed around Steel Path enemy scaling challenges.
   - **Rule-Aug:** 10.0/10 - The recommendation explicitly addresses Steel Path enemy scaling and armor mechanics throughout. It discusses armor stripping, slash procs bypassing armor, the importance of status effects for high-level Grineer, and specifically mentions 'early Steel Path Grineer' and 'high-level Grineer' multiple times. The analysis of why Viral and slash damage are effective directly relates to Steel Path armor scaling mechanics.


#### Baseline Analysis (No Rules)

**Summary:**

Your build maximizes crit and fire rate but lacks effective armor mitigation for Grineer in Steel Path. Switching elemental mods to Viral/Heat and considering Hunter Munitions for Slash procs will dramatically improve performance. Ammo efficiency and punch through can also be optimized.

**Analysis:**

This Acceltra Prime build is strongly focused on critical damage, multishot, and fire rate. You've chosen Radiation/Electricity as your elemental combo, and your mod selection includes all core base damage (Serration), critical chance (Point Strike), crit damage (Vital Sense), fire rate (Speed Trigger), multishot (Galvanized Chamber), and elemental mods (Stormbringer, Wildfire, Hellfire). This results in efficient critical output and high burst damage, but there are a few critical issues for early Steel Path Grineer:

- **Element Types**: Radiation is not optimal for Grineer in Steel Path. They have high armor, so Slash procs or a combo of Viral + Heat is strongly preferred for effective armor and health stripping.
- **Lack of Status/Slash**: Your build has 18% status, with no Hunter Munitions or way to proc Slash from crits. This limits health-bypassing damage against armored Grineer.
- **Ammo Economy**: Acceltra Prime remains ammo-hungry, and your high fire rate (Speed Trigger) may...

**Recommendations:**

1. **[HIGH]** Swap elemental mods to Viral (Toxin + Cold) and add Heat for crowd control and armor stripping (e.g., Infected Clip + Cryo Rounds + Hellfire).
   - **Reasoning:** Viral increases damage to health, and Heat strips armor over time. This is the meta combo for Grineer in Steel Path, increasing overall kill efficiency.
   - **Context:** Standard endgame and Steel Path Grineer builds rely on Viral/Heat due to the extreme durability of enemy armor and health scaling.[1][2][5][6][7]
   - **Reference:** https://steamcommunity.com/app/230410/discussions/0/601904634461704117/

2. **[HIGH]** Replace Speed Trigger or Wildfire with Hunter Munitions if you have it.
   - **Reasoning:** Hunter Munitions gives a chance to inflict Slash procs on crit, bypassing Grineer armor and melting health pools.
   - **Context:** Acceltra Prime has strong crit potential, making Slash procs via Hunter Munitions extremely effective on Steel Path.[2][5][7]
   - **Reference:** https://www.youtube.com/watch?v=rgV05ql7Mmo

3. **[MEDIUM]** Consider replacing Serration with Primary Merciless or another primary arcane for higher scalable damage.
   - **Reasoning:** With proper arcane setup, base damage mods can be replaced for longer endurance without redundancy.
   - **Context:** Weapon arcanes stack with mods like Galvanized Chamber and are optimal for Steel Path efficiency after ramp-up.[5]
   - **Reference:** https://www.youtube.com/watch?v=onKlsrRCTGg

4. **[MEDIUM]** If ammo is an issue, drop Speed Trigger for Vigilante Supplies (ammo mutation) or run ammo-support frames (e.g., Protea, Carrier).
   - **Reasoning:** Sustain is critical in Steel Path; high fire rate quickly depletes the small ammo pool of Acceltra Prime.[2][5][7]
   - **Context:** Multiple creators highlight ammo problems post-acceltra changes; consider support as needed.
   - **Reference:** https://www.youtube.com/watch?v=JLYHApAipP0

5. **[LOW]** Add Punch Through with a mod like Vigilante Offense if you feel crowded in missions or want more collateral hits.
   - **Reasoning:** Helps clear mobs faster on busy Steel Path nodes.
   - **Context:** Not always needed for open maps, but beneficial for tile-sets with narrow corridors.[7]
   - **Reference:** https://steamcommunity.com/app/230410/discussions/0/601904634461704117/


#### Rule-Augmented Analysis

**Summary:**

This Acceltra Prime build is heavily focused on Radiation and raw crit, which is suboptimal for Steel Path Grineer. Swapping to Viral+Heat, adding Hunter Munitions, and addressing ammo concerns will provide dramatically better results for this content.

**Analysis:**

Your current Acceltra Prime build focuses on maximizing critical stats (Crit Chance/Crit Damage via Point Strike + Vital Sense), multishot (Galvanized Chamber), and base damage (Serration), with added electric elementals (Stormbringer, Wildfire, Hellfire) forming Radiation. Deadhead arcane is appropriate for extra headshot damage. However, for early Steel Path Grineer, several aspects can be significantly optimized:

- **Corrosive/Radiation vs Viral/Heat**: Corrosive and Radiation are less optimal than Viral + Heat or Viral + Slash for fast Grineer kills in Steel Path; Viral heavily boosts all damage, while Heat not only strips armor but also inflicts powerful DoT. Radiation is niche for Alloy-armored targets and is not as versatile.
- **Status Output**: Your current status chance (18%) is low for reliably inflicting procs with non-beam rifles—this reduces the effectiveness of viral/heat or slash/viral strategies, especially since slash procs bypass armor.
- **Hunter Munitions...

**Recommendations:**

1. **[HIGH]** Replace elemental mods to build Viral + Heat instead of Radiation. Use Malignant Force/Infected Clip (Toxin) + Cryo Rounds (Cold) + Hellfire (Heat).
   - **Reasoning:** Viral multiplies all damage by up to 325% and Heat strips armor while providing damage-over-time, both exceptionally effective versus Grineer armor and health in Steel Path.
   - **Context:** Steel Path Grineer have massive armor and health pools, making armor strip and damage multiplication superior to Radiation.
   - **Reference:** [7]

2. **[HIGH]** Replace Speed Trigger with Hunter Munitions.
   - **Reasoning:** With 85% Crit Chance, you are primed for frequent slash procs from Hunter Munitions. Slash damage ignores armor and is vital for high-level Grineer.
   - **Context:** Steel Path Grineer rely on their armor for durability; Slash dots from Hunter Munitions directly bypass this defense.
   - **Reference:** [2][7]

3. **[MEDIUM]** Consider replacing Wildfire with a 60/60 elemental mod (Rime Rounds or Malignant Force) for status chance and elemental coverage.
   - **Reasoning:** Low status chance means unreliable procs; 60/60 mods improve chance to proc Viral and Heat, as well as provide some CC. Wildfire is less effective for raw DPS.
   - **Context:** Reliable status application is key for Viral stacks and Heat procs on Steel Path enemies.
   - **Reference:** [2][6][7]

4. **[MEDIUM]** If ammo is an issue, replace a non-core mod (like Speed Trigger or Wildfire) with Vigilante Supplies or Ammo Mutation.
   - **Reasoning:** Acceltra Prime is still limited by ammo. Ammo mutation ensures consistent DPS output in long Steel Path engagements.
   - **Context:** Sustained DPS is essential for Steel Path; running out of ammo can quickly become fatal.
   - **Reference:** [2][3][7]

5. **[LOW]** If you have Galvanized Aptitude, consider slotting it for increased damage per status effect on target.
   - **Reasoning:** Galvanized Aptitude multiplies damage per unique status effect, synergizing with Viral + Heat and Hunter Munitions slash procs for maximal output.
   - **Context:** Extra scaling is especially noticeable in Steel Path where enemies survive longer and take more statuses.
   - **Reference:** [3][7]


---

## Methodology

### Objective Metrics

**Specificity Score:** Unique game entities mentioned / recommendation count
- Measures how concrete and specific recommendations are
- Higher = mentions specific weapons, mods, frames by name
- Range: 0-5+ (unbounded)

**Terminology Score:** Game-specific mechanic terms used (normalized to 0-1)
- Measures technical depth and game knowledge
- Higher = uses terms like 'status chance', 'corrosive', 'slash proc'
- Range: 0-1

**Actionability Score:** Recommendations with clear actions (0-1)
- Measures how actionable and concrete recommendations are
- Higher = specific verbs (equip, replace) + reasoning
- Range: 0-1

**Overall Improvement:** Average of the three objective metric deltas
- Positive = rules improved recommendations
- Negative = rules degraded recommendations

### Subjective Evaluation

**Subjective Score:** LLM-based evaluation against custom criteria (0-10 scale)
- Uses a separate evaluator LLM to score recommendations against predefined expectations
- Each criterion can specify positive expectations (should include X) or negative expectations (should avoid Y)
- Criteria are weighted and averaged to produce an overall subjective score
- Score of 10 = perfectly meets criterion, 0 = completely fails criterion
- More sensitive than objective metrics to domain-specific quality nuances

