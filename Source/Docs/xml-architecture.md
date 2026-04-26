# RimPsychedelics — XML Architecture

## Purpose

This document defines the structural patterns, wiring conventions, and design rationale for all XML defs in RimPsychedelics. It is the canonical reference for how defs relate to each other and why they are organized the way they are.

**This is not a field reference.** For per-field documentation and modder-facing API, see `drug-profiles-final.md`. For per-drug parameter values, see `drug-profiles-final.md`. For how the C# classes consume these defs, see `cs-architecture.md`.

---

## 1. Governing Principle: Extension as Single Source of Truth

The `PsychedelicDrugExtension` on each drug's ThingDef is the **sole authority** for the hediff chain. All five hediff references (come-up, positive peak, negative peak, positive resolution, negative resolution) are defined there and nowhere else.

### What this means in practice

- `HediffCompProperties_TripResolution` on peak hediffs contains **only** `drugDef` — a back-reference to the drug ThingDef. It does NOT contain `resolutionHediffDef`. At runtime, the comp looks up `drugDef` → `PsychedelicDrugExtension` → selects the correct resolution hediff based on `tripValence`.
- The come-up hediff receives `nextHediffDef` as a **runtime field** set by the IngestionOutcomeDoer, which reads it from the extension. The come-up HediffDef in XML contains no reference to peak hediffs.
- Resolution hediff mappings are never duplicated. If you need to know which hediff follows which, you look at the extension. Period.

### Why

Earlier iterations stored resolution hediff refs on both the extension and the comp properties. This created a maintenance hazard — changing a resolution hediff required updating two locations per drug, and a mismatch would produce silent bugs (wrong post-trip hediff applied). The current design eliminates this class of error entirely.

It also enables configurations where both valences share a peak HediffDef (the peak is identical, only the resolution differs). With duplicated refs, each peak def would need its own resolution mapping. With valence passed explicitly through the chain, a single peak def works for both valences because the resolution comp reads valence at runtime, not from its XML properties.

### Cross-reference pattern

```
ThingDef (drug)
  └── PsychedelicDrugExtension
        ├── hediffDefComeUp ──────────────► ComeUp HediffDef
        ├── hediffDefPositiveTrip ────────► Good Peak HediffDef
        ├── hediffDefNegativeTrip ────────► Bad Peak HediffDef
        ├── hediffDefPositiveResolution ──► Good Resolution HediffDef
        └── hediffDefNegativeResolution ──► Bad Resolution HediffDef

ComeUp HediffDef
  └── (no outgoing def references in XML — peak hediff set at runtime)

Peak HediffDef (good or bad)
  └── HediffCompProperties_TripResolution
        └── drugDef ──────────────────────► ThingDef (drug) ← back-reference
```

The extension points outward to hediffs. Peak hediffs point back to the drug. No hediff points to another hediff in XML. All forward chain navigation happens at runtime.

---

## 2. The Chemical Foundation Pattern

Every RP drug requires a `ChemicalDef` with two associated hediffs: a tolerance hediff and a dummy addiction hediff. This is a vanilla requirement — the tolerance system needs a chemical to reference.

### ChemicalDef

```xml
<ChemicalDef>
  <defName>RP_Chemical{Name}</defName>
  <label>{name}</label>
  <addictionHediff>RP_{Name}AddictionDummy</addictionHediff>
  <toleranceHediff>RP_{Name}Tolerance</toleranceHediff>
  <onGeneratedAddictedToleranceChance>0.0</onGeneratedAddictedToleranceChance>
</ChemicalDef>
```

**Convention**: `onGeneratedAddictedToleranceChance` is always `0.0` for base RP drugs to prevent the pawn generator from invoking the IngestionOutcomeDoer during worldgen.

### Dummy Addiction HediffDef

Required by vanilla's chemical system even for non-addictive drugs. For base RP drugs, this clears instantly and should never be visible to the player. Drugs designed to be addictive would use a real addiction hediff here instead.

**Required properties (non-addictive pattern):**
- `hediffClass`: `Hediff_Addiction` directly — NOT via `ParentName="AddictionBase"`. Using `AddictionBase` pulls in withdrawal stages and need generation, which creates visible side effects for a hediff that should be invisible.
- `maxSeverity`: `0.01` — ensures instant clear.
- `severityPerDay`: `-1.0` — decays immediately.
- `isBad`: `false` — prevents alert/notification triggers.

### Tolerance HediffDef

Standard `HediffWithComps` with `SeverityPerDay` comp. Decay rate controls the effective cooldown between trips.

**Design constraint**: Tolerance severity is read by the IngestionOutcomeDoer at ingestion time. The `noTripToleranceThreshold` (defined on the extension, not the hediff) determines when tolerance blocks a trip entirely. The hediff itself is a vanilla-standard severity-over-time tracker.

**Convention**: `maxSeverity` is always `1.0`. Each dose applies `1.0` severity via a standard `IngestionOutcomeDoer_GiveHediff` on the drug ThingDef.

---

## 3. The Hediff Chain Pattern

Each drug defines a chain of hediffs representing trip phases. The chain is always:

```
ComeUp → Peak (good or bad) → Resolution (positive or negative)
```

### 3.1 ComeUp HediffDef

**hediffClass**: `RimPsychedelics.Hediff_PsychedelicComeUp` (custom class, required).

**XML-defined behavior:**
- `severityPerDay`: Positive value (severity rises). This is the *base* rate — at runtime, it is multiplied by `comeUpRateMultiplier` (1.0 normally, ~2.5 during rituals).
- `maxSeverity`: Always `1.0` in XML. The actual cap is `maxSeverityCap`, a runtime field set by the IngestionOutcomeDoer based on tolerance. The XML value is the absolute ceiling.
- `stages`: Onset effects. Number and content of stages are drug-specific — design these to match the character of each drug's onset.

**XML does NOT define:**
- Which peak hediff follows (set at runtime from extension)
- The severity cap for this instance (set at runtime from tolerance)
- The rate multiplier (set at runtime, 1.0 default)
- Valence (set at runtime from valence roll)

### 3.2 Peak HediffDefs

**hediffClass**: `HediffWithComps` (standard vanilla).

Each drug defines two peak HediffDefs: one for good trips, one for bad trips. They may have identical capacity penalties (LYS, Mindcap) or different mood/social effects (Fluff).

**Required comps:**
1. `HediffCompProperties_SeverityPerDay` — Negative value (severity decays). Controls peak duration.
2. `RimPsychedelics.HediffCompProperties_TripResolution` — Contains **only** `drugDef`. See [Section 1](#1-governing-principle-extension-as-single-source-of-truth).

**Stage design:**

Stage count, labels, severity thresholds, and effects are entirely drug-specific. Design them to match the character of each substance.

### 3.3 Resolution HediffDefs

**hediffClass**: `HediffWithComps` (standard vanilla).

Simple decaying hediffs representing post-trip states. No custom comps — these are endpoint hediffs with no onward chain.

**Required comp:**
1. `HediffCompProperties_SeverityPerDay` — Negative value. Controls post-trip duration (typically days).

Stage count, labels, severity thresholds, and effects (mood, social fight chance, capacity mods) are drug-specific. Design them to match the intended post-trip experience.

**Shared resolution pattern**: Both `hediffDefPositiveResolution` and `hediffDefNegativeResolution` on the extension may point to the same HediffDef. This is how Fluff implements its universal hangover. The resolution HediffDef itself doesn't know or care about valence.

**Shared peak pattern**: Similarly, `hediffDefPositiveTrip` and `hediffDefNegativeTrip` may point to the same HediffDef. In this configuration, good and bad trips have identical capacity penalties and stage progression — valence only matters at resolution time, when the comp reads `tripValence` to select the correct resolution hediff. No base RP drugs use this pattern, but the architecture supports it fully.

---

## 4. Drug ThingDef Structure

The drug ThingDef wires together the chemical system, ingestion behavior, and the psychedelic extension.

### Outcome Doers (order matters)

```xml
<outcomeDoers>
  <!-- RP custom doer: reads extension, handles tolerance check, 
       valence roll, and come-up hediff application -->
  <li Class="RimPsychedelics.PsychedelicIngestionOutcomeDoer">
    <toleranceChemical>RP_Chemical{Name}</toleranceChemical>
  </li>
  <!-- Vanilla doer: applies tolerance. RP checks tolerance but 
       does not apply it — that's vanilla's job -->
  <li Class="IngestionOutcomeDoer_GiveHediff">
    <hediffDef>RP_{Name}Tolerance</hediffDef>
    <severity>1.0</severity>
  </li>
</outcomeDoers>
```

**Why two doers**: The RP doer reads the current tolerance severity to make trip decisions, then the vanilla doer applies the new tolerance. If the vanilla doer fired first, the tolerance check would always read the post-dose value (too high). Order is preserved by list position in XML.

### CompProperties_Drug

```xml
<li Class="CompProperties_Drug">
  <chemical>RP_Chemical{Name}</chemical>
  <addictiveness>0.0</addictiveness>
  <minToleranceToAddict>9999.0</minToleranceToAddict>
  <existingAddictionSeverityOffset>0</existingAddictionSeverityOffset>
  <needLevelOffset>0</needLevelOffset>
  <listOrder>{unique int}</listOrder>
</li>
```

**Convention**: `addictiveness` is `0.0` and `minToleranceToAddict` is `9999.0` for all base RP drugs. This makes addiction functionally impossible while still satisfying vanilla's drug comp requirements.

**Addiction is possible but unsupported**: The framework does not prevent creating addictive RP drugs — vanilla's addiction system will function normally if `addictiveness` is set above 0. However, RP's tolerance mechanics (long cooldown periods, wasted doses above threshold) interact with addiction in ways that haven't been tested or balanced. If creating an addictive RP drug, carefully consider how the tolerance cooldown interacts with the addiction need cycle.

### PsychedelicDrugExtension Placement

The extension lives in `modExtensions` on the drug ThingDef. This is the single attachment point — it is never placed on hediff defs, chemical defs, or anywhere else.

```xml
<modExtensions>
  <li Class="RimPsychedelics.PsychedelicDrugExtension">
    <!-- All per-drug configuration here -->
  </li>
</modExtensions>
```

---

## 5. Extension Internal Structure

The extension groups its fields into functional sections. The order below is the canonical XML ordering convention for all RP drugs.

### 5.1 Hediff References (5 fields)

```xml
<hediffDefComeUp>RP_{Name}ComeUp</hediffDefComeUp>
<hediffDefPositiveTrip>RP_{Name}TripGood</hediffDefPositiveTrip>
<hediffDefNegativeTrip>RP_{Name}TripBad</hediffDefNegativeTrip>
<hediffDefPositiveResolution>RP_{Name}Afterglow</hediffDefPositiveResolution>
<hediffDefNegativeResolution>RP_{Name}Disturbed</hediffDefNegativeResolution>
```

All five are `HediffDef` references. Both resolution fields may point to the same def.

### 5.2 Tolerance (2 fields)

```xml
<toleranceChemical>RP_Chemical{Name}</toleranceChemical>
<noTripToleranceThreshold>0.5</noTripToleranceThreshold>
```

`toleranceChemical` is the same ChemicalDef referenced by the outcome doer and CompProperties_Drug. This triple-reference to the same ChemicalDef is unavoidable — each consumer reads it independently.

`noTripToleranceThreshold` is the severity above which a dose is wasted. Per-drug tunable. Typical range: 0.3 (strict) to 0.7 (forgiving).

### 5.3 Psychonaut Progression (2 fields)

```xml
<psychonautExperienceGain>0.1</psychonautExperienceGain>
<countsPsychonautExperience>true</countsPsychonautExperience>
```

`psychonautExperienceGain` controls how much severity is added to the shared `RP_PsychedelicExperience` hediff per qualifying trip. The hediff has a hardcoded cap of 0.7 — values are clamped on application. Set to 0 (or omit) for drugs that should not contribute experience at all.

`countsPsychonautExperience` controls whether the Psychonaut trait acquisition roll fires at resolution. A drug can contribute experience (`psychonautExperienceGain` > 0) without triggering the roll (`countsPsychonautExperience: false`). The accumulated experience is still available when a drug that does roll is taken.

### 5.4 Valence Curves (4 required + up to 5 optional)

Normal pawn curve (4, required), Psychonaut curve (4, optional), ritual bonus (1, optional). See Core Systems doc for sigmoid math.

The normal curve parameters are required for every drug. Psychonaut parameters fall back to their normal counterparts when omitted — a modder can override all four, some, or none. Omitting all Psychonaut parameters means the Psychonaut trait has no effect on valence for that drug.

```xml
<!-- Normal curve (required) -->
<goodTripChanceAtZeroMood>...</goodTripChanceAtZeroMood>
<goodTripChanceUpperLimit>...</goodTripChanceUpperLimit>
<goodTripTransitionPoint>...</goodTripTransitionPoint>
<goodTripPlateauPoint>...</goodTripPlateauPoint>

<!-- Psychonaut curve (optional — each field falls back to normal if omitted) -->
<psychonautGoodTripChanceAtZeroMood>...</psychonautGoodTripChanceAtZeroMood>
<psychonautGoodTripChanceUpperLimit>...</psychonautGoodTripChanceUpperLimit>
<psychonautGoodTripTransitionPoint>...</psychonautGoodTripTransitionPoint>
<psychonautGoodTripPlateauPoint>...</psychonautGoodTripPlateauPoint>

<!-- Ritual bonus (optional, defaults to 0.0) -->
<ritualGoodTripChanceBonus>...</ritualGoodTripChanceBonus>
```

### 5.5 Inspiration (3 fields)

```xml
<canGoodTripInspire>true</canGoodTripInspire>
<canBadTripInspire>false</canBadTripInspire>
<inspirationChance>0.15</inspirationChance>
```

### 5.6 Trait Modification (2 scalar fields + 2 list fields)

```xml
<canModifyTraits>true</canModifyTraits>
<traitChangeChance>0.10</traitChangeChance>
<traitChangeChancePsychonaut>0.02</traitChangeChancePsychonaut>

<traitSpectrums>
  <li>...</li>
</traitSpectrums>

<traitStandalones>
  <li>...</li>
</traitStandalones>
```

When `canModifyTraits` is `false`, the trait lists may be omitted entirely. The C# skips all trait logic before reaching the lists.

### 5.7 Tale Overrides (3 fields, optional)

```xml
<goodTripTale>RP_HadGoodTrip_Fluff</goodTripTale>
<badTripTale>RP_HadBadTrip_Fluff</badTripTale>
<sharedTripTale>RP_SharedTrip_Fluff</sharedTripTale>
```

Each field references a `TaleDef` and is optional. When omitted (null), the C# falls back to the default tales in `Source/RimPsychedelics/Core/RP_TaleDefOf.cs` — `RP_HadGoodTrip`, `RP_HadBadTrip`, and `RP_SharedTrip` respectively. Those defaults are written with hallucinogenic imagery (visual distortion, perceptual unmaking) and fit "classic psychedelic" experiences.

Override these when a drug's experience profile doesn't fit the default imagery — e.g., empathogens (fluff) where the trip is emotional/social rather than visual, or a hypothetical dissociative where it's depersonalized. The custom tales should live in the drug's own XML file, not in `Defs/Core/Tales_Psychedelics.xml`.

The override mechanism applies only to the three per-trip tales. `RP_FirstTrip`, `RP_GainedPsychonaut`, and the four trait-change tales remain shared across drugs and cannot be overridden — they describe pawn-life events or personality shifts, not the trip's qualitative texture.

---

## 6. Trait Entry XML Patterns

### 6.1 Spectrum Entries

```xml
<li>
  <traitDef>NaturalMood</traitDef>
  <minDegree>-2</minDegree>
  <maxDegree>2</maxDegree>
  <selectionWeight>1.2</selectionWeight>
  <goodTripBias>0.7</goodTripBias>
  <badTripBias>-0.7</badTripBias>
  <higherMessage>{PAWN}'s outlook brightened after the {DRUG} trip.</higherMessage>
  <lowerMessage>{PAWN} came back seeing the world darker.</lowerMessage>
</li>
```

**Degree range constraints**: `minDegree` and `maxDegree` must correspond to actual `degreeDatas` entries on the vanilla TraitDef. Degree 0 (no trait) is always implicitly included in the step sequence if the range spans across it. If the range does not include 0, the "no trait" state is not part of the spectrum — the pawn must already have the trait at a degree within range for this entry to be eligible.

**Bias direction**: Follows vanilla degree numbering. "Higher" means toward `maxDegree`, "lower" means toward `minDegree`. For traits where higher degree = worse outcome (e.g., NervousnessTraitDef where +2 = Very Neurotic), a good trip that *reduces* neuroticism needs a **negative** `goodTripBias`.

### 6.2 Standalone Entries

```xml
<li>
  <traitDef>Kind</traitDef>
  <degree>0</degree>
  <selectionWeight>0.8</selectionWeight>
  <goodTripBias>1.0</goodTripBias>
  <badTripBias>-0.6</badTripBias>
  <addedMessage>Something shifted in {PAWN}. A tenderness that wasn't there before.</addedMessage>
  <removedMessage>{PAWN} pulled inward, less generous than before.</removedMessage>
</li>
```

**Bias direction**: Positive = favors adding the trait. Negative = favors removing it.

### 6.3 Defaults and Omission

All fields except `traitDef` (and `minDegree`/`maxDegree` for spectrums) have defaults:
- `selectionWeight`: 1.0
- `goodTripBias` / `badTripBias`: 0.0 (coin flip)
- `degree` (standalone only): 0
- Messages: auto-generated by C# if omitted

A minimal spectrum entry needs only `traitDef`, `minDegree`, `maxDegree`. A minimal standalone entry needs only `traitDef`.

---

### 7. Social Opinion ThoughtDefs

Some drugs apply a universal opinion offset to all other pawns during the trip, rather than using `opinionOfOthersFactor` on hediff stages. This is implemented via `ThoughtWorker_RPSocialOffset` subclass ThoughtDefs, entirely XML-driven.

#### When to Use

Use `opinionOfOthersFactor` on hediff stages when the drug should **amplify existing relationships** (pawns the tripper already likes are liked more, pawns they dislike are disliked more).

Use a social opinion ThoughtDef when the drug should apply a **flat offset to all relationships equally** — e.g., a "love everyone" empathogenic effect or a "resent everyone" paranoid effect. These are additive with all other social thoughts and do not multiply existing opinion.

#### Structure

```xml
<ThoughtDef>
  <defName>RP_{DrugName}{Valence}TripSocial</defName>
  <workerClass>RimPsychedelics.ThoughtWorker_RPSocialOffset</workerClass>
  <modExtensions>
    <li Class="RimPsychedelics.SocialOffsetExtension">
      <hediffDef>RP_{DrugName}Trip{Good|Bad}</hediffDef>
      <severityThresholds>
        <!-- Descending order. Each value maps to a stage index. -->
        <!-- Severity above first threshold  → stage 0 -->
        <!-- Severity above second threshold → stage 1 -->
        <!-- ... -->
        <!-- Below all thresholds            → last stage -->
        <li>0.70</li>
        <li>0.30</li>
      </severityThresholds>
    </li>
  </modExtensions>
  <stages>
    <!-- One stage per threshold, plus one final stage for "below all" -->
    <li>
      <label>stage label</label>
      <baseOpinionOffset>40</baseOpinionOffset>
    </li>
    <li>
      <label>stage label</label>
      <baseOpinionOffset>40</baseOpinionOffset>
    </li>
    <li>
      <label>stage label</label>
      <baseOpinionOffset>20</baseOpinionOffset>
    </li>
  </stages>
</ThoughtDef>
```

#### Wiring Rules

- **Stage count = threshold count + 1.** Two thresholds → three stages. Three thresholds → four stages.
- **Thresholds must be in descending order.** The worker iterates top-down and returns the first match.
- **Thresholds should align with the hediff's stage boundaries** for consistency, but they are independent — the ThoughtDef can subdivide the hediff's severity range differently if desired.
- **`hediffDef` must reference a peak hediff** (or any hediff the pawn carries during the relevant period). The worker checks `pawn.health.hediffSet` for this hediff.
- **No back-reference required.** Unlike peak hediffs (which back-reference the drug ThingDef via `drugDef`), social ThoughtDefs are standalone. They don't need to know about the drug or the extension — they only watch a hediff.

#### Cross-Reference Pattern

```
ThoughtDef (social opinion)
  └── SocialOffsetExtension
        └── hediffDef ──────► Peak HediffDef (read-only, no back-reference)
```

The ThoughtDef reads hediff state. The hediff is unaware of the ThoughtDef. This means social opinion ThoughtDefs can be added to (or removed from) a drug without modifying any existing hediff XML.

#### Naming Convention

| Def Type | Pattern | Example |
|----------|---------|---------|
| Good trip social thought | `RP_{Name}{Valence}TripSocial` | `RP_FluffGoodTripSocial` |
| Bad trip social thought | `RP_{Name}{Valence}TripSocial` | `RP_FluffBadTripSocial` |

---

### 7.5 Mood ThoughtDefs (ThoughtWorker_Hediff)

Mood effects are delivered via vanilla `ThoughtWorker_Hediff` ThoughtDefs rather than `baseMoodEffect` on hediff stages. This gives each mood stage its own label and description visible in the mood tooltip.

#### Structure

Each hediff with mood effects has a paired ThoughtDef placed directly below it in the drug XML file. The ThoughtDef uses vanilla `ThoughtWorker_Hediff` and references the hediff via the `<hediff>` field. Thought stages map 1:1 to hediff stages — same count, same order (ascending by minSeverity).

#### Naming Convention

| Def Type | Pattern | Example |
|----------|---------|---------|
| Mood ThoughtDef | `RP_{Drug}{Phase}Mood` | `RP_LYSComeUpMood` |

#### Wiring Rules

- Stage count must exactly match the hediff's stage count.
- Stage ordering must match (ascending minSeverity).
- Each thought stage carries `baseMoodEffect` and has its own `label` and `description`.
- Stages where the hediff has no mood impact use `baseMoodEffect` of 0 (the thought still appears with a narrative label).
- The mood ThoughtDef coexists with any social ThoughtDefs on the same hediff — they are independent systems.

#### Cross-Reference Pattern

```
HediffDef
  ↓ (paired, placed directly below in XML)
ThoughtDef (mood)
  └── hediff → HediffDef (read-only reference)
```

The hediff is unaware of the ThoughtDef. Mood ThoughtDefs can be added or removed without modifying hediff XML.

---

## 8. Shared Defs (Cross-Drug)

### PsychedelicExperience Hediff

```xml
<defName>RP_PsychedelicExperience</defName>
```

Hidden tracker hediff. No `SeverityPerDay` comp — severity is only incremented by resolution logic (0.1 per qualifying trip). `maxSeverity` is `0.7`. Removed entirely when Psychonaut trait is granted.

All stages use `<becomeVisible>false</becomeVisible>`. The hediff exists purely as a data store.

### RP_RecentTrip Thought

An invisible 15-day memory thought applied at resolution. Zero mood effect, `stackLimit` 1. Exists solely as a flag for the Ideology Essential precept ("haven't tripped recently" debuff checks for the absence of this thought).

---

## 9. File Organization

### Canonical Structure

```
1.6/Defs/
├── Drugs/
│   ├── LYS.xml
│   ├── fluff.xml
│   └── mindcap.xml
├── Core/
│   ├── hediffs.xml
│   ├── traits.xml
│   ├── Tales_Psychedelics.xml
│   ├── thoughts.xml
│   ├── Plants_Ergo.xml
│   ├── Plants_MindcapMushroom.xml
│   ├── Plants_SassafrasTree.xml
│   ├── Resources_Precursors.xml
│   ├── Recipes_DrugProcessing.xml
│   └── Research_Psychedelics.xml
└── Patches/
    ├── Sassafras_Patches.xml      (vanilla biome wildPlants)
    ├── Patches_AlphaBiomes.xml    (gated by PatchOperationConditional on AB_IdyllicMeadows)
    └── Patches_DubsBadHygiene.xml (gated by PatchOperationConditional on ThirstRateMultiplier)
```

### Per-Drug File Contents

Each file in `Defs/Drugs/` contains **everything** for that drug in this order:

1. ChemicalDef
2. Dummy Addiction HediffDef
3. Tolerance HediffDef
4. ComeUp HediffDef
   4a. ComeUp Mood ThoughtDef
5. Good Peak HediffDef
   5a. Good Peak Mood ThoughtDef
6. Bad Peak HediffDef
   6a. Bad Peak Mood ThoughtDef
7. Positive Resolution HediffDef
   7a. Positive Resolution Mood ThoughtDef
8. Negative Resolution HediffDef (omit if same as positive)
   8a. Negative Resolution Mood ThoughtDef
9. Drug ThingDef with PsychedelicDrugExtension
10. Social ThoughtDefs (if relevant)
11. Drug-specific TaleDefs (if the drug overrides the default tales — see Section 5.7)

**Rationale**: Everything needed to understand, debug, or tune a single drug is in one file. The ordering mirrors the lifecycle: chemical foundation → hediff chain (in temporal order) → drug def that wires it all together.

### Core Files

`1.6/Defs/Core/` contains defs shared across all drugs:
- `hediffs.xml` — the hidden progression tracker
- `traits.xml` — the Psychonaut TraitDef
- `Tales_Psychedelics.xml` — all TaleDefs (trip tales, shared trip tales, trait change tales)
- `thoughts.xml` — RP_RecentTrip and any other shared thought defs

### File Organization Does Not Affect Loading

RimWorld loads all XML defs from all files in the `Defs/` tree into a flat database. File boundaries are purely organizational. XPath patches operate on the loaded def database by `defName`, not by file path. Splitting or merging files has zero gameplay impact.

---

## 10. Naming Conventions

| Def Type | Pattern | Example |
|----------|---------|---------|
| ChemicalDef | `RP_Chemical{Name}` | `RP_ChemicalLYS` |
| Dummy Addiction | `RP_{Name}AddictionDummy` | `RP_LYSAddictionDummy` |
| Tolerance Hediff | `RP_{Name}Tolerance` | `RP_LYSTolerance` |
| ComeUp Hediff | `RP_{Name}ComeUp` | `RP_LYSComeUp` |
| Good Peak Hediff | `RP_{Name}TripGood` | `RP_LYSTripGood` |
| Bad Peak Hediff | `RP_{Name}TripBad` | `RP_LYSTripBad` |
| Positive Resolution | `RP_{Name}Afterglow` (or drug-specific) | `RP_LYSAfterglow` |
| Negative Resolution | `RP_{Name}Disturbed` (or drug-specific) | `RP_LYSDisturbed` |
| Drug ThingDef | `RP_{Name}` | `RP_LYS` |
| Mood ThoughtDef | `RP_{Name}{Phase}Mood` | `RP_LYSComeUpMood` |
| Shared Hediff | `RP_PsychedelicExperience` | — |
| Thought | `RP_RecentTrip` | — |

Resolution hediff names are flexible — Fluff uses `RP_FluffRecovery` for both valences. The pattern above is a starting point, not a constraint.

---

## 11. Validation Checklist

When adding or modifying a drug, verify:

1. **Extension references resolve**: All five `hediffDef*` fields on the extension point to hediff defs that exist.
2. **Back-reference matches**: Every peak hediff's `HediffCompProperties_TripResolution.drugDef` points back to the correct drug ThingDef (the one with the extension).
3. **Chemical consistency**: The same ChemicalDef defName appears in: the ChemicalDef itself, the drug's `CompProperties_Drug.chemical`, the drug's `PsychedelicIngestionOutcomeDoer.toleranceChemical`, and the extension's `toleranceChemical`.
4. **Tolerance hediff matches**: The ChemicalDef's `toleranceHediff` matches the hediffDef applied by the vanilla `IngestionOutcomeDoer_GiveHediff` in the drug's outcome doers.
5. **Outcome doer order**: `PsychedelicIngestionOutcomeDoer` is listed before `IngestionOutcomeDoer_GiveHediff` so tolerance is read before it's applied.
6. **Come-up maxSeverity**: Always `1.0` in XML.
7. **Degree range validity**: All spectrum entry `minDegree`/`maxDegree` values correspond to actual `degreeDatas` entries on the referenced vanilla TraitDef.
8. **No orphaned hediffs**: Every hediff def defined in the drug file is referenced by either the extension or the ChemicalDef.
9. **Social ThoughtDef stage count**: If the drug has social opinion ThoughtDefs, verify that `stages` count = `severityThresholds` count + 1.
10. **Social ThoughtDef hediff reference**: Each social ThoughtDef's `SocialOffsetExtension.hediffDef` must point to a hediff def that actually exists (typically the drug's good or bad peak hediff).
11. **Mood ThoughtDef stage count**: Each mood ThoughtDef must have exactly as many stages as its paired HediffDef.
12. **Mood ThoughtDef hediff reference**: Each mood ThoughtDef's `<hediff>` field must reference the correct HediffDef.

