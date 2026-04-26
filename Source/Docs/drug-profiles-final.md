# RimPsychedelics — Drug Profiles (Revised)

## Purpose

Per-drug reference cards containing all finalized parameter values. This is the tuning and balance document — open it when you need to know a specific number, compare drugs, or identify what still needs deciding.

### Reading This Document

- Values marked **TBD** have no planned value yet.
- Values marked **TBD (starting point: X)** have a placeholder copied from another drug, flagged for independent tuning.
- Values marked **DEFERRED** are creative writing tasks to be completed manually in XML.
- All other values are locked design decisions finalized in the design session.

---

## Table of Contents

1. [LYS](#1-lys)
2. [Mindcap](#2-mindcap)
3. [Fluff](#3-fluff)
4. [Cross-Drug Comparison](#4-cross-drug-comparison)
5. [Shared Systems](#5-shared-systems)
6. [Open Questions](#6-open-questions)

---

## 1. LYS

### 1.1 ThingDef

| Property | Value |
|----------|-------|
| defName | `RP_LYS` |
| label | LYS |
| techLevel | Industrial |
| drugCategory | Hard |
| marketValue | 36 |
| workToMake | 600 |
| mass | 0.005 |
| joy | 1.0 |
| joyKind | Chemical |
| baseIngestTicks | 60 |

### 1.2 Production Chain

```
RP_Plant_Ergo (grow) → Ergo (harvest) + Neutroamine → LYS (drug lab)
```

| Stage | Details |
|-------|---------|
| Plant | `RP_Plant_Ergo` — growable crop. Grow days: 14. Fertility sensitivity: 100%. Yield: 13. Sow tags: Ground, Hydroponic. Min sow skill: 6. |
| Precursor | `Ergo` — raw ergot alkaloid. Stackable. Ingestible (`DesperateOnly`) — causes `RP_RawPrecursorSickness`. |
| Recipe | 100 Ergo + 1 Neutroamine → 10 LYS. Drug lab only. (~8 plants per batch.) |
| Research | Industrial tier, 4000 cost, prerequisite: DrugProduction. |
| Skill requirements | Crafting 8, Intellectual 12 |
| Bottleneck | Neutroamine (trade-gated, not craftable) |

### 1.3 Chemical Foundation

| Def | defName |
|-----|---------|
| ChemicalDef | `RP_ChemicalLYS` |
| Dummy Addiction | `RP_LYSAddictionDummy` |
| Tolerance Hediff | `RP_LYSTolerance` |

### 1.4 Tolerance

| Property | Value |
|----------|-------|
| Severity per dose | 1.0 |
| Decay rate | −0.10/day |
| Days below 50% | ~5 |
| Days to zero | ~10 |
| noTripToleranceThreshold | 0.5 |

### 1.5 Trip Timing

| Phase | Hediff | severityPerDay | Duration | Ritual (~2.5x) |
|-------|--------|---------------|----------|----------------|
| Come-Up | `RP_LYSComeUp` | +6.857 | ~3.5 hr | ~85 min |
| Good Peak | `RP_LYSTripGood` | −1.4545 | ~16.5 hr | — |
| Bad Peak | `RP_LYSTripBad` | −1.4545 | ~16.5 hr | — |
| Afterglow | `RP_LYSAfterglow` | −0.1429 | ~7 days | — |
| Disturbed | `RP_LYSDisturbed` | −0.3333 | ~3 days | — |

### 1.6 Come-Up Stages

Three stages across ~3.5 hours. Severity rises from 0 to 1.0 at +6.857/day.

**Stage 1 — "waiting"** (minSeverity 0.0, ~45 min)

| Effect | Value |
|--------|-------|
| Mood | +2 |
| (no other effects) | |

**Stage 2 — "unsettled"** (minSeverity 0.21, ~1.5 hr)

| Effect | Value |
|--------|-------|
| Mood | −5 |
| vomitMtbDays | 5 |
| hungerRateFactor | 0.5 |
| restFallFactor | 0.3 |

| Capacity | Value |
|----------|-------|
| Consciousness | −0.05 |
| Moving | −0.15 |
| BloodFiltration | −0.03 |
| BloodPumping | +0.10 |
| Breathing | +0.07 |
| Metabolism | +0.07 |

**Stage 3 — "ascending"** (minSeverity 0.64, ~1.25 hr)

| Effect | Value |
|--------|-------|
| Mood | −2 |
| vomitMtbDays | 8 |
| painFactor | 0.3 |
| hungerRateFactor | 0.3 |
| restFallFactor | 0.15 |
| socialFightChanceFactor | 0.5 |
| forgetMemoryThoughtMtbDays | 5 |
| pctConditionalThoughtsNullified | 25 |
| opinionOfOthersFactor | 1.5 |

| Capacity | Value |
|----------|-------|
| Consciousness | −0.12 |
| Moving | −0.25 |
| BloodFiltration | −0.07 |
| BloodPumping | +0.20 |
| Breathing | +0.13 |
| Metabolism | +0.13 |
| Sight | −0.30 |
| Manipulation | −0.15 |
| Talking | −0.10 |
| Hearing | −0.20 |

### 1.7 Peak Stages — Physical Effects (Shared by Good and Bad)

Four stages across ~16.5 hours. Severity decays from 1.0 to 0.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Peaking | 0.82 | ~3 hr |
| Plateau | 0.52 | ~5 hr |
| Easing | 0.24 | ~4.5 hr |
| Fading | 0.0 | ~4 hr |

**Physical effects by stage:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| vomitMtbDays | 10 | — | — | — |
| painFactor | 0.1 | 0.12 | 0.3 | 0.5 |
| hungerRateFactor | 0.1 | 0.1 | 0.1 | 0.5 |
| restFallFactor | 0.05 | 0.05 | 0.05 | 0.3 |
| forgetMemoryThoughtMtbDays | 3 | 4 | 6 | 10 |
| pctConditionalThoughtsNullified | 75 | 75 | 35 | 15 |

| Capacity | Peaking | Plateau | Easing | Fading |
|----------|---------|---------|--------|--------|
| Consciousness | −0.20, max 0.80 | −0.17, max 0.85 | −0.10 | −0.05 |
| Sight | −0.60 | −0.50 | −0.30 | −0.15 |
| Moving | −0.40, max 0.70 | −0.35, max 0.75 | −0.20 | −0.10 |
| Manipulation | −0.30 | −0.25 | −0.15 | −0.07 |
| Talking | −0.20 | −0.17 | −0.10 | −0.05 |
| Hearing | −0.40 | −0.35 | −0.20 | −0.10 |
| BloodFiltration | −0.10 | −0.08 | −0.05 | −0.03 |
| BloodPumping | +0.30 | +0.25 | +0.15 | +0.07 |
| Breathing | +0.20 | +0.17 | +0.10 | +0.05 |
| Metabolism | +0.20 | +0.17 | +0.10 | +0.05 |

### 1.8 Peak Stages — Valence-Specific (Mood & Social)

**Good Peak:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| Mood | — | +2 | +30 | +25 |
| socialFightChanceFactor | 0 | 0 | 0.5 | 0.8 |

**Bad Peak:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| Mood | — | −3 | −12 | −10 |
| socialFightChanceFactor | 0 | 0.5 | 1.0 | 1.5 |

**OpinionOfOthersFactor (diverges by valence at plateau):**

| Stage | Good | Bad |
|-------|------|-----|
| Peaking | 2.5 | 2.5 |
| Plateau | 3.0 | 2.0 |
| Easing | 2.0 | 0.8 |
| Fading | 1.5 | 0.6 |

### 1.9 Afterglow Stages

Three stages across ~7 days. Severity decays at −0.1429/day.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Glowing | 0.79 | ~1.5 days |
| Settling | 0.43 | ~2.5 days |
| Fading | 0.0 | ~3 days |

| Effect | Glowing | Settling | Fading |
|--------|---------|----------|--------|
| Mood | +12 | +8 | +5 |
| socialFightChanceFactor | 0.8 | 0.85 | 0.95 |
| opinionOfOthersFactor | 1.3 | 1.1 | 1.0 |

### 1.10 Disturbed Stages

Three stages across ~3 days. Severity decays at −0.3333/day.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Shaken | 0.83 | ~0.5 days |
| Unsettled | 0.50 | ~1 day |
| Fading | 0.0 | ~1.5 days |

| Effect | Shaken | Unsettled | Fading |
|--------|--------|-----------|--------|
| Mood | −8 | −5 | −1 |
| socialFightChanceFactor | 1.5 | 1.2 | 1.0 |
| opinionOfOthersFactor | 0.6 | 0.8 | 1.0 |

### 1.11 Valence Curves

**Normal pawn:**

| Parameter | Value |
|-----------|-------|
| goodTripChanceAtZeroMood | 0.10 |
| goodTripChanceUpperLimit | 0.90 |
| goodTripTransitionPoint | 0.50 |
| goodTripPlateauPoint | 0.80 |

**Psychonaut:**

| Parameter | Value |
|-----------|-------|
| psychonautGoodTripChanceAtZeroMood | 0.20 |
| psychonautGoodTripChanceUpperLimit | 0.95 |
| psychonautGoodTripTransitionPoint | 0.30 |
| psychonautGoodTripPlateauPoint | 0.55 |

**Ritual bonus:** 0.15

### 1.12 Inspiration

| Property | Value |
|----------|-------|
| canGoodTripInspire | true |
| canBadTripInspire | false |
| inspirationChance | 0.15 |

### 1.13 Psychonaut Progression

| Property | Value |
|----------|-------|
| psychonautExperienceGain | 0.1 |
| countsPsychonautExperience | true |

### 1.14 Trait Modification

| Property | Value |
|----------|-------|
| canModifyTraits | true |
| traitChangeChance | 0.10 |
| traitChangeChancePsychonaut | 0.01 |

### 1.15 Trait Pool — Spectrums

| TraitDef | minDegree | maxDegree | Weight | Good Bias | Bad Bias |
|----------|-----------|-----------|--------|-----------|----------|
| NaturalMood | −2 | 2 | 1.2 | 0.7 | −0.7 |
| Nerves | −1 | 2 | 1.0 | 0.6 | −0.7 |
| Neurotic | 0 | 2 | 0.6 | −0.7 | 0.5 |
| DrugDesire | −1 | 2 | 0.5 | 0.3 | −0.8 |
| PsychicSensitivity | −1 | 1 | 0.3 | 0.4 | 0.0 |

### 1.16 Trait Pool — Standalones

| TraitDef | Degree | Weight | Good Bias | Bad Bias |
|----------|--------|--------|-----------|----------|
| Kind | 0 | 0.8 | 1.0 | −0.6 |
| Abrasive | 0 | 0.5 | −0.8 | 1.0 |
| Nudist | 0 | 0.15 | 0.6 | −0.3 |
| Ascetic | 0 | 0.4 | 0.8 | 0.4 |
| BodyPurist | 0 | 0.3 | 0.7 | 0.6 |
| Transhumanist | 0 | 0.3 | 0.6 | 0.0 |

### 1.17 Trait Pool — Messages

**DEFERRED** — to be manually written in XML.

---

## 2. Mindcap

### 2.1 ThingDef

| Property | Value |
|----------|-------|
| defName | `RP_Mindcap` |
| label | mindcap |
| techLevel | Neolithic |
| drugCategory | Hard |
| marketValue | 12 |
| workToMake | 200 |
| mass | 0.02 |
| joy | 0.90 |
| joyKind | Chemical |
| baseIngestTicks | 120 |

### 2.2 Production Chain

```
RP_Plant_MindcapMushroom (grow) → RP_MindcapMushroom (harvest) → RP_Mindcap (dry)
```

| Stage | Details |
|-------|---------|
| Plant | `RP_Plant_MindcapMushroom` — growable mushroom. Grow days: 8. Fertility sensitivity: 30%. Yield: 2. Light requirement: Darkness. Sow tags: Ground, Hydroponic. Min sow skill: 4. |
| Harvest | `RP_MindcapMushroom` — raw mushroom. Rottable. Ingestible (`DesperateOnly`) — causes `RP_RawPrecursorSickness`. |
| Processing | 3 raw mushrooms → 1 mindcap. Campfire or drug lab. |
| Research | `RP_MindcapPreparation` — Neolithic tier, 600 cost, no prerequisites. |

### 2.3 Chemical Foundation

| Def | defName |
|-----|---------|
| ChemicalDef | `RP_ChemicalMindcap` |
| Dummy Addiction | `RP_MindcapAddictionDummy` |
| Tolerance Hediff | `RP_MindcapTolerance` |

### 2.4 Tolerance

| Property | Value |
|----------|-------|
| Severity per dose | 1.0 |
| Decay rate | −0.10/day |
| Days below 50% | ~5 |
| Days to zero | ~10 |
| noTripToleranceThreshold | 0.5 |

### 2.5 Trip Timing

| Phase | Hediff | severityPerDay | Duration | Ritual (~2.5x) |
|-------|--------|---------------|----------|----------------|
| Come-Up | `RP_MindcapComeUp` | +24.0 | ~1 hr | ~25 min |
| Good Peak | `RP_MindcapTripGood` | −3.4286 | ~7 hr | — |
| Bad Peak | `RP_MindcapTripBad` | −3.4286 | ~7 hr | — |
| Afterglow | `RP_MindcapAfterglow` | −0.3333 | ~3 days | — |
| Disturbed | `RP_MindcapDisturbed` | −0.5 | ~2 days | — |

### 2.6 Come-Up Stages

Two stages across ~1 hour. Severity rises from 0 to 1.0 at +24.0/day.

**Stage 1 — "waiting"** (minSeverity 0.0, ~20 min)

| Effect | Value |
|--------|-------|
| Mood | +2 |
| (no other effects) | |

**Stage 2 — "hitting"** (minSeverity 0.33, ~40 min)

| Effect | Value |
|--------|-------|
| Mood | −5 |
| vomitMtbDays | 0.25 |
| hungerRateFactor | 0.4 |
| restFallFactor | 0.2 |

| Capacity | Value |
|----------|-------|
| Consciousness | −0.08 |
| Moving | −0.20 |
| BloodFiltration | −0.05 |
| BloodPumping | +0.15 |
| Breathing | +0.10 |
| Metabolism | +0.10 |
| Sight | −0.15 |
| Manipulation | −0.08 |
| Talking | −0.05 |
| Hearing | −0.10 |

### 2.7 Peak Stages — Physical Effects (Shared by Good and Bad)

Four stages across ~7 hours. Severity decays from 1.0 to 0.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Peaking | 0.79 | ~1.5 hr |
| Plateau | 0.50 | ~2 hr |
| Easing | 0.21 | ~2 hr |
| Fading | 0.0 | ~1.5 hr |

**Physical effects by stage:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| vomitMtbDays | 0.5 | 1.0 | 3.0 | — |
| painFactor | 0.4 | 0.45 | 0.6 | 0.75 |
| hungerRateFactor | 0.1 | 0.1 | 0.5 | 0.75 |
| restFallFactor | 0.05 | 0.05 | 0.25 | 0.5 |
| forgetMemoryThoughtMtbDays | 3 | 4 | 6 | 10 |
| pctConditionalThoughtsNullified | 75 | 75 | 35 | 15 |

| Capacity | Peaking | Plateau | Easing | Fading |
|----------|---------|---------|--------|--------|
| Consciousness | −0.20, max 0.80 | −0.17, max 0.85 | −0.10 | −0.05 |
| Sight | −0.60 | −0.50 | −0.30 | −0.15 |
| Moving | −0.40, max 0.70 | −0.35, max 0.75 | −0.20 | −0.10 |
| Manipulation | −0.30 | −0.25 | −0.15 | −0.07 |
| Talking | −0.20 | −0.17 | −0.10 | −0.05 |
| Hearing | −0.40 | −0.35 | −0.20 | −0.10 |
| BloodFiltration | −0.10 | −0.08 | −0.05 | −0.03 |
| BloodPumping | +0.30 | +0.25 | +0.15 | +0.07 |
| Breathing | +0.20 | +0.17 | +0.10 | +0.05 |
| Metabolism | +0.20 | +0.17 | +0.10 | +0.05 |

### 2.8 Peak Stages — Valence-Specific (Mood & Social)

**Good Peak:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| Mood | — | +2 | +25 | +20 |
| socialFightChanceFactor | 0 | 0 | 0 | 0.3 |

**Bad Peak:**

| Effect | Peaking | Plateau | Easing | Fading |
|--------|---------|---------|--------|--------|
| Mood | — | −5 | −18 | −15 |
| socialFightChanceFactor | 0 | 0.7 | 1.3 | 1.8 |

**OpinionOfOthersFactor (diverges by valence at plateau):**

| Stage | Good | Bad |
|-------|------|-----|
| Peaking | 2.5 | 2.5 |
| Plateau | 3.0 | 1.5 |
| Easing | 2.0 | 0.5 |
| Fading | 1.5 | 0.5 |

### 2.9 Afterglow Stages

Three stages across ~3 days. Severity decays at −0.3333/day.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Glowing | 0.67 | ~1 day |
| Settling | 0.33 | ~1 day |
| Fading | 0.0 | ~1 day |

| Effect | Glowing | Settling | Fading |
|--------|---------|----------|--------|
| Mood | +10 | +8 | +5 |
| socialFightChanceFactor | 0.5 | 0.7 | 0.9 |
| opinionOfOthersFactor | 1.3 | 1.1 | 1.0 |

### 2.10 Disturbed Stages

Three stages across ~2 days. Severity decays at −0.5/day.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Shaken | 0.67 | ~0.67 days |
| Unsettled | 0.33 | ~0.67 days |
| Fading | 0.0 | ~0.67 days |

| Effect | Shaken | Unsettled | Fading |
|--------|--------|-----------|--------|
| Mood | −10 | −6 | −2 |
| socialFightChanceFactor | 1.8 | 1.4 | 1.1 |
| opinionOfOthersFactor | 0.5 | 0.7 | 1.0 |

### 2.11 Valence Curves

**Normal pawn:**

| Parameter | Value |
|-----------|-------|
| goodTripChanceAtZeroMood | 0.10 |
| goodTripChanceUpperLimit | 0.85 |
| goodTripTransitionPoint | 0.45 |
| goodTripPlateauPoint | 0.80 |

**Psychonaut:**

| Parameter | Value |
|-----------|-------|
| psychonautGoodTripChanceAtZeroMood | 0.20 |
| psychonautGoodTripChanceUpperLimit | 0.90 |
| psychonautGoodTripTransitionPoint | 0.20 |
| psychonautGoodTripPlateauPoint | 0.55 |

**Ritual bonus:** 0.15

### 2.12 Inspiration

| Property | Value |
|----------|-------|
| canGoodTripInspire | true |
| canBadTripInspire | true |
| inspirationChance | 0.15 |

### 2.13 Psychonaut Progression

| Property | Value |
|----------|-------|
| psychonautExperienceGain | 0.1 |
| countsPsychonautExperience | true |

### 2.14 Trait Modification

| Property | Value |
|----------|-------|
| canModifyTraits | true |
| traitChangeChance | 0.08 |
| traitChangeChancePsychonaut | 0.01 |

### 2.15 Trait Pool — Spectrums

| TraitDef | minDegree | maxDegree | Weight | Good Bias | Bad Bias |
|----------|-----------|-----------|--------|-----------|----------|
| NaturalMood | −1 | 1 | 1.2 | 0.7 | −0.7 |
| Nerves | −1 | 2 | 1.0 | 0.6 | −0.7 |
| Neurotic | 0 | 2 | 0.6 | −0.7 | 0.5 |
| DrugDesire | −1 | 2 | 0.5 | 0.3 | −0.8 |
| PsychicSensitivity | −1 | 1 | 0.3 | 0.4 | 0.0 |

### 2.16 Trait Pool — Standalones

| TraitDef | Degree | Weight | Good Bias | Bad Bias |
|----------|--------|--------|-----------|----------|
| Kind | 0 | 0.8 | 1.0 | −0.6 |
| Abrasive | 0 | 0.5 | −0.8 | 1.0 |
| Ascetic | 0 | 0.4 | 0.8 | 0.4 |

### 2.17 Trait Pool — Messages

**DEFERRED** — to be manually written in XML.

---

## 3. Fluff

### 3.1 ThingDef

| Property | Value |
|----------|-------|
| defName | `RP_Fluff` |
| label | fluff |
| techLevel | Industrial |
| drugCategory | Social |
| marketValue | 22 |
| workToMake | 400 |
| mass | 0.01 |
| joy | 1.0 |
| joyKind | Chemical |
| baseIngestTicks | 60 |

### 3.2 Production Chain

```
RP_Plant_TreeSassafras (grow/chop) → RP_ChoppedStumpSassafras (harvest) → RP_Safrole → Fluff (drug lab)
```

| Stage | Details |
|-------|---------|
| Plant | `RP_Plant_TreeSassafras` — extends `DeciduousTreeBase`. Normal tree growth rules. Spawns in 7 biomes via XPath patches. `choppedThingDef` → `RP_ChoppedStumpSassafras`. |
| Stump | `RP_ChoppedStumpSassafras` — extends `StumpChoppedBase`. Deterioration rate 50 (high). Harvestable for safrole. If smashed, yields wood not safrole. Yield: 12 safrole. |
| Precursor | `RP_Safrole` — safrole oil. Rottable (30 days). |
| Recipe | 2 Safrole → 1 Fluff. Drug lab only. (6 doses per tree.) |
| Skill requirements | Intellectual 4 |
| Research | Industrial tier, 1000 cost, prerequisite: DrugProduction. |

### 3.3 Chemical Foundation

| Def | defName |
|-----|---------|
| ChemicalDef | `RP_ChemicalFluff` |
| Dummy Addiction | `RP_FluffAddictionDummy` |
| Tolerance Hediff | `RP_FluffTolerance` |

### 3.4 Tolerance

| Property | Value |
|----------|-------|
| Severity per dose | 1.0 |
| Decay rate | −0.3333/day |
| Days below 50% | ~1.5 |
| Days to zero | ~3 |
| noTripToleranceThreshold | 0.5 |

### 3.5 Trip Timing

| Phase | Hediff | severityPerDay | Duration | Ritual (~2.5x) |
|-------|--------|---------------|----------|----------------|
| Come-Up | `RP_FluffComeUp` | +24.0 | ~1 hr | ~25 min |
| Good Peak | `RP_FluffTripGood` | −4.8 | ~5 hr | — |
| Bad Peak | `RP_FluffTripBad` | −4.8 | ~5 hr | — |
| Recovery | `RP_FluffRecovery` | −1.0 | ~1 day | — |

Note: both valences resolve to the same hediff (`RP_FluffRecovery`).

### 3.6 Come-Up Stages

Two stages across ~1 hour. Severity rises from 0 to 1.0 at +24.0/day.

**Stage 1 — "waiting"** (minSeverity 0.0, ~20 min)

No effects.

**Stage 2 — "rising"** (minSeverity 0.33, ~40 min)

| Effect | Value |
|--------|-------|
| Mood | −1 |
| vomitMtbDays | 8 |
| hungerRateFactor | 0.7 |
| restFallFactor | 0.5 |

| Capacity | Value |
|----------|-------|
| Consciousness | −0.03 |
| BloodPumping | +0.10 |
| Breathing | +0.05 |

### 3.7 Peak Stages — Physical Effects (Shared by Good and Bad)

Three stages across ~5 hours. Severity decays from 1.0 to 0.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Rolling | 0.70 | ~1.5 hr |
| Plateau | 0.30 | ~2 hr |
| Fading | 0.0 | ~1.5 hr |

**Physical effects by stage:**

| Effect | Rolling | Plateau | Fading |
|--------|---------|---------|--------|
| painFactor | 0.1 | 0.15 | 0.4 |
| hungerRateFactor | 0.1 | 0.1 | 0.5 |
| restFallFactor | 0.05 | 0.05 | 0.3 |

| Capacity | Rolling | Plateau | Fading |
|----------|---------|---------|--------|
| Consciousness | −0.05 | −0.04 | −0.02 |
| Sight | −0.10 | −0.08 | −0.04 |
| Moving | −0.05 | −0.04 | −0.02 |
| Manipulation | −0.08 | −0.06 | −0.03 |
| Talking | +0.15 | +0.12 | +0.05 |
| Hearing | −0.10 | −0.08 | −0.04 |
| BloodPumping | +0.25 | +0.20 | +0.10 |
| Breathing | +0.15 | +0.12 | +0.05 |
| Metabolism | +0.20 | +0.15 | +0.08 |

Note: No thought nullification, no forgetMemoryThought. Fluff overlays euphoria; it does not restructure consciousness.

### 3.8 Peak Stages — Valence-Specific (Mood & Social)

**Good Peak:**

| Effect | Rolling | Plateau | Fading |
|--------|---------|---------|--------|
| Mood | +35 | +28 | +15 |
| socialFightChanceFactor | 0 | 0 | 0 |
| opinionOffset (ThoughtWorker) | +40 | +40 | +20 |

Note: Good Fluff uses a custom `ThoughtWorker_RPSocialOffset` subclass to apply a universal positive opinion offset, rather than `opinionOfOthersFactor`. This creates a "love everyone" effect instead of amplifying existing relationships. See C# and XML Architecture documents for details.

**Bad Peak:**

| Effect | Rolling | Plateau | Fading |
|--------|---------|---------|--------|
| Mood | −6 | −10 | −8 |
| socialFightChanceFactor | 0 | 0.3 | 0.8 |
| opinionOfOthersFactor | 1.5 | 1.3 | 1.1 |
| opinionOffset (ThoughtWorker) | — | −5 | −3 |

Note: Bad Fluff uses both `opinionOfOthersFactor` on the hediff stage (amplifying intensity) and a second `ThoughtWorker_RPSocialOffset` for a mild negative opinion offset.

### 3.9 Recovery Stages

Two stages across ~1 day. Severity decays at −1.0/day. Both valences resolve here.

| Stage | minSeverity | Duration |
|-------|-------------|----------|
| Drained | 0.5 | ~12 hr |
| Fading | 0.0 | ~12 hr |

| Effect | Drained | Fading |
|--------|---------|--------|
| Mood | −5 | −2 |
| Consciousness | −0.08 | −0.04 |
| Talking | −0.10 | −0.05 |
| Moving | −0.08 | −0.03 |
| Manipulation | −0.05 | −0.02 |
| BloodFiltration | −0.05 | −0.02 |
| restFallFactor | 1.3 | 1.1 |
| hungerRateFactor | 1.3 | 1.1 |

Note: restFallFactor and hungerRateFactor above 1.0 represent rebound — the body demanding payback. Unique to Fluff recovery.

### 3.10 Valence Curves

**Normal pawn:**

| Parameter | Value |
|-----------|-------|
| goodTripChanceAtZeroMood | 0.40 |
| goodTripChanceUpperLimit | 0.95 |
| goodTripTransitionPoint | 0.23 |
| goodTripPlateauPoint | 0.35 |

**Psychonaut:**

| Parameter | Value |
|-----------|-------|
| psychonautGoodTripChanceAtZeroMood | 0.65 |
| psychonautGoodTripChanceUpperLimit | 0.98 |
| psychonautGoodTripTransitionPoint | 0.20 |
| psychonautGoodTripPlateauPoint | 0.35 |

**Ritual bonus:** 0.10

### 3.11 Inspiration

| Property | Value |
|----------|-------|
| canGoodTripInspire | false |
| canBadTripInspire | false |
| inspirationChance | 0.0 |

### 3.12 Trait Modification

| Property | Value |
|----------|-------|
| canModifyTraits | false |

### 3.13 Psychonaut Progression

| Property | Value |
|----------|-------|
| psychonautExperienceGain | 0 |
| countsPsychonautExperience | false |

---

## 4. Cross-Drug Comparison

### Trip Timing

| Drug | Come-Up | Peak | Post-Trip | Total Active |
|------|---------|------|-----------|-------------|
| LYS | ~3.5 hr | ~16.5 hr | 7 days (good) / 3 days (bad) | ~20 hr + days |
| Mindcap | ~1 hr | ~7 hr | 3 days (good) / 2 days (bad) | ~8 hr + days |
| Fluff | ~1 hr | ~5 hr | 1 day (both) | ~6 hr + 1 day |

### Tolerance Cooldown

| Drug | Decay Rate | Below 50% | To Zero |
|------|-----------|-----------|---------|
| LYS | −0.10/day | ~5 days | ~10 days |
| Mindcap | −0.10/day | ~5 days | ~10 days |
| Fluff | −0.3333/day | ~1.5 days | ~3 days |

### Valence Safety (Normal Pawn)

| Drug | Floor (0% mood) | Ceiling | Transition | Plateau |
|------|-----------------|---------|------------|---------|
| LYS | 10% | 90% | 0.50 | 0.80 |
| Mindcap | 10% | 85% | 0.45 | 0.80 |
| Fluff | 40% | 95% | 0.23 | 0.35 |

### Impairment Profile

| Drug | Peak Character |
|------|---------------|
| LYS | Heavy — Consciousness capped at 80%, Sight −60%, Moving capped at 70%. Pawn incapacitated. No nausea after peaking stage. |
| Mindcap | Heavy — identical capacity penalties to LYS. More nausea (persists through plateau). Less pain suppression. Shorter duration. |
| Fluff | Light — max Consciousness −5%, Sight −10%, Moving −5%. Talking **boosted** +15%. Pawn functional. No thought nullification. |

### Economic

| Drug | Market Value | Tech Level | Production Bottleneck |
|------|-------------|------------|----------------------|
| LYS | 36 | Industrial | Neutroamine (trade) |
| Mindcap | 12 | Neolithic | None |
| Fluff | 22 | Industrial | Sassafras trees (biome) |

### Feature Matrix

| Feature | LYS | Mindcap | Fluff |
|---------|-----|---------|-------|
| Trait modification | Yes | Yes | No |
| Good trip inspiration | Yes | Yes | No |
| Bad trip inspiration | No | Yes | No |
| Psychonaut experience gain | 0.1 | 0.1 | No |
| Psychonaut acquisition roll | Yes | Yes | No |
| Separate good/bad resolution | Yes | Yes | No (shared) |
| Drug category | Hard | Hard | Social |
| Opinion offset (ThoughtWorker_RPSocialOffset) | No | No | Yes (good + bad peak) |

---

## 5. Shared Systems

### Psychedelic Experience Hediff

| Property | Value |
|----------|-------|
| defName | `RP_PsychedelicExperience` |
| maxSeverity | 0.7 |
| Increment per trip | 0.1 |
| Decay | None |
| Visibility | Hidden (all stages `becomeVisible: false`) |

Stages (hidden, for internal tracking only):

| Label | minSeverity |
|-------|-------------|
| curious | 0.0 |
| experienced | 0.3 |
| seasoned | 0.5 |

### Psychonaut Trait

| Property | Value |
|----------|-------|
| defName | `Psychonaut` |
| commonality | 0.2 |
| Degrees | Single (0) — standalone, no spectrum |
| Stat offsets | None — interacts only with RP systems |
| label | psychonaut |

### RP_RecentTrip Thought

| Property | Value |
|----------|-------|
| Duration | 15 days |
| Mood effect | 0 (invisible) |
| stackLimit | 1 |
| Purpose | Flag for Ideology Essential precept |

---

## 6. Raw Precursor Sickness

Both raw Mindcap mushrooms (`RP_MindcapMushroom`) and Ergo (`Ergo`) are ingestible but cause severe sickness. This applies equally to humanlikes and animals via a standard vanilla `IngestionOutcomeDoer_GiveHediff`. No custom C# required.

### Precursor Ingestible Settings

Both precursors should have `preferability` set to `DesperateOnly` so pawns won't voluntarily eat them unless starving.

```xml
<outcomeDoers>
  <li Class="IngestionOutcomeDoer_GiveHediff">
    <hediffDef>RP_RawPrecursorSickness</hediffDef>
    <severity>1.0</severity>
  </li>
</outcomeDoers>
```

### RP_RawPrecursorSickness Hediff

| Property | Value |
|----------|-------|
| defName | `RP_RawPrecursorSickness` |
| hediffClass | HediffWithComps |
| label | precursor sickness |
| severityPerDay | −1.0 (~1 day total) |
| maxSeverity | 1.0 |
| isBad | true |

**Stage 1 — "delirious"** (minSeverity 0.5, ~12 hr)

| Effect | Value |
|--------|-------|
| vomitMtbDays | 0.08 |
| painOffset | 0.30 |
| Consciousness | −0.50 |
| Moving | −0.40, setMax 0.30 |
| Manipulation | −0.30 |
| Sight | −0.40 |
| Hearing | −0.30 |
| Talking | −0.30 |
| BloodFiltration | −0.15 |
| hungerRateFactor | 0.1 |
| restFallFactor | 0.1 |

**Stage 2 — "recovering"** (minSeverity 0.0, ~12 hr)

| Effect | Value |
|--------|-------|
| vomitMtbDays | 0.5 |
| painOffset | 0.10 |
| Consciousness | −0.20 |
| Moving | −0.15 |
| Manipulation | −0.10 |
| Sight | −0.15 |
| BloodFiltration | −0.08 |

**Design notes:**
- Delirious stage is incapacitating: −50% consciousness, movement capped at 30%, frequent vomiting. Pawn is completely out of action.
- Small animals may die from consciousness drop alone.
- Total duration ~1 day. Properly punishing — players learn quickly to keep raw precursors away from animals and colonists.
- No custom C# needed. Vanilla outcome doer applies to any pawn (humanlike or animal) that consumes the precursor.

---

## 7. Open Questions

Remaining items not resolved in this design session:

1. **Plant defs — detailed XML** — Ergo and Mindcap plant values are finalized but full PlantDef XML (textures, descriptions, etc.) needs authoring.
2. **DubsBadHygiene compatibility** — bathroom need suppression during trips, noted for future implementation.
3. **Trait pool messages** — all three drugs' trait change messages deferred to manual writing.
4. **Hediff stage mood descriptions** — all mood thought descriptions deferred to manual writing.
