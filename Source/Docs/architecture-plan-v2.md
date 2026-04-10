# RimPsychedelics — Complete Architecture Specification v2

## 1. MOD IDENTITY

- **Mod Name**: RimPsychedelics (RP)
- **Namespace Prefix**: `RP_`
- **Target Game Version**: 1.6
- **Required DLCs**: None (base mod)
- **Soft Dependencies**: Vanilla Social Interactions Expanded (VSIE)
- **Separate Expansion Mods**:
  - RimPsychedelics - Ideology (requires RP + Ideology DLC)
  - RimPsychedelics - DrugCategory (future optional, requires RP)

---

## 2. THE THREE DRUGS

### 2A. LYS (LSD Analog)
**Theme**: High-effort, high-reward. Long-acting commitment drug. The premium psychedelic.

| Stage | ThingDef | Details |
|-------|----------|---------|
| **Plant** | `RP_Plant_Ergo` | Growable crop. Moderate-to-long grow time. No soil quality requirement. |
| **Precursor** | `Ergo` | Raw fungal ergot alkaloid. Stackable, not ingestible. |
| **Processing** | Ergo + Neutroamine → LYS | Drug lab only. Requires research (Industrial, 4000 cost, prereq: DrugProduction). Crafting 8 + Intellectual 12. |
| **Product** | `LYS` | Ingestible tab. DrugCategory: Hard. Market value: 36. |

**Trip Profile**:
- Come-Up: ~3.5 hr
- Peak: ~16.5 hr
- Total impairment: ~20 hr
- Good trip afterglow: 7 days (fading positive mood + social bonuses)
- Bad trip disturbed: 3 days (fading negative mood + social penalties)
- Good trip decay (peak): -1.4545/day
- Afterglow decay: -0.1429/day | Disturbed decay: -0.3333/day

**Production Bottleneck**: Neutroamine (not craftable, must be traded). LYS is a luxury gated by trade network access. 100 Ergo + 1 Neutroamine → 10 LYS.

**Tolerance**: 1.0 severity per dose. Decay -0.10/day → 5 days to drop below 50%, 10 days to reach zero.

---

### 2B. Fluff (MDMA Analog)
**Theme**: The social drug. Empathogenic, warm, connective. Very low bad trip chance. Light impairment, big mood effects during peak. Universal hangover regardless of trip valence.

| Stage | ThingDef | Details |
|-------|----------|---------|
| **Plant** | `RP_Plant_TreeSassafras` | Extends `DeciduousTreeBase`. Normal tree growth rules. Spawns in 7 biomes via XPath patches. `choppedThingDef` → custom stump. |
| **Stump** | `RP_ChoppedStumpSassafras` | Extends `StumpBase`. High deterioration (50). Harvestable for safrole. Inherits vanilla damage — if smashed, yields wood not safrole. |
| **Precursor** | `RP_Safrole` | Safrole oil from stump roots. Rottable (30 days). |
| **Processing** | Safrole → Fluff | Drug lab. Requires research. |
| **Product** | `Fluff` | Ingestible pill/crystal. DrugCategory: Social. |

**Trip Profile**:
- Come-Up: ~1 hr
- Peak: ~5 hr
- Total impairment: ~6 hr
- Recovery: 1 day (mild debuffs, same regardless of good/bad trip)
- Peak character: LOW impairment, HIGH mood effects. Pawn remains functional.

**Tolerance**: 1.0 severity per dose. Decay -0.3333/day → 1.5 days to drop below 50%, 3 days to reach zero.

---

### 2C. Dried Mindcap (Psilocybin Analog)
**Theme**: Accessible, natural, volatile. Shortest and most intense trip. Easy to grow, hard to predict. The starter drug.

| Stage | ThingDef | Details |
|-------|----------|---------|
| **Plant** | `RP_Plant_MindcapMushroom` | Growable. Short grow time. Flexible soil. |
| **Harvest** | `RP_MindcapMushroom` | Raw harvested mushroom. Rottable. |
| **Processing** | Dry at campfire or drug lab | Simple drying process. |
| **Product** | `RP_DriedMindcap` | Ingestible mushroom. DrugCategory: Hard. Market value: 12. |

**Trip Profile**:
- Come-Up: ~1 hr
- Peak: ~7 hr
- Total impairment: ~8 hr
- Good trip afterglow: 3 days (fading positive mood)
- Bad trip disturbed: 3 days (fading negative mood + social penalties)
- Mindcap afterglow is shorter than LYS (3 days vs 7 days) — the upgrade incentive.

**Tolerance**: 1.0 severity per dose. Decay -0.10/day → 5 days to drop below 50%, 10 days to reach zero.

---

## 3. TOLERANCE SYSTEM

| Aspect | Behavior |
|--------|----------|
| Severity | 1.0 per dose for all three drugs. |
| Decay | Decreases over time. Rate per-drug, defined in XML via `HediffCompProperties_SeverityPerDay`. |
| Intensity cap | Tolerance severity scales down the `maxSeverityCap` on the come-up hediff, limiting how high peak severity can reach. |
| Duration cap | Afterglow/Disturbed/Recovery hediff applied at severity scaled by tolerance at ingestion. |
| Zero-tolerance gate | Trait changes and inspirations ONLY if tolerance was exactly zero at ingestion. |
| Body size | Explicitly ignored. Not factored into any RP drug calculation. Handled by using custom `PsychedelicIngestionOutcomeDoer` instead of patching vanilla. |
| Cross-tolerance | None. Each drug has independent tolerance. Pawns can take different drugs on consecutive days at full power. |

### Inherent Tolerance (Biotech Genes)
Biotech genes that grant inherent drug tolerance or modify metabolism (e.g., `DrugMetabolism` spectrum, `AddictionResistance`) may prevent a pawn from ever reaching zero tolerance. Such pawns can trip with reduced intensity but will **never** experience trait changes or inspirations. Full gene audit needed during implementation.

### Chemical/Addiction Def Pattern
Each drug uses a `ChemicalDef` with a dummy `AddictionHediff` that instantly clears (-1.0 severity/day, maxSeverity 0.01). This is required by vanilla's tolerance system — tolerance hediffs need a chemical to reference. Addiction is functionally impossible (`addictiveness: 0.0`, `minToleranceToAddict: 9999.0`). All ChemicalDefs set `onGeneratedAddictedToleranceChance` to `0.0` to prevent the pawn generator from calling `PsychedelicIngestionOutcomeDoer` during worldgen. The dummy addiction hediff uses `<hediffClass>Hediff_Addiction</hediffClass>` directly rather than inheriting from `AddictionBase`, to avoid pulling in withdrawal stages and need generation.

---

## 4. HEDIFF SYSTEM

### Architecture
Each drug uses a sequential hediff chain plus post-trip effects. LYS and Mindcap use 5 hediff defs each. Fluff uses 4 (single recovery hediff regardless of valence).

```
        ┌─── max% (100% at zero tolerance, capped by tolerance)
       / \
      /   \
     /     \
    /       \
   /         \
  0%          0%  → [Resolution: apply afterglow/disturbed/recovery, traits, inspiration]

  |--ComeUp--|----Peak----|---Post-Trip---|
   hediff #1    hediff #2    hediff #3
   (severity    (severity    (severity
    rises)       decays)      decays over days)
```

The entire hediff chain is defined in the `PsychedelicDrugExtension` on the drug's ThingDef. The extension is the single source of truth — resolution hediff mappings are NOT duplicated on comp properties. Valence is passed explicitly through the chain to support shared peak HediffDefs.

### Per Drug: Hediff Defs

**LYS (5 hediffs):**

| HediffDef | Severity | Duration | Role |
|-----------|----------|----------|------|
| `LYSComeUp` | Rises 0 → max | ~3.5 hr | Shared pre-valence. Transitions to good or bad peak. |
| `LYSTripGood` | Starts at max, decays → 0 | ~16.5 hr | Good trip active impairment. |
| `LYSTripBad` | Starts at max, decays → 0 | ~16.5 hr | Bad trip active impairment. |
| `LYSAfterglow` | Starts at max, decays → 0 | ~7 days | Fading positive mood + social bonuses. |
| `LYSDisturbed` | Starts at max, decays → 0 | ~3 days | Fading negative mood + social penalties. |

**Mindcap (5 hediffs):**

| HediffDef | Severity | Duration | Role |
|-----------|----------|----------|------|
| `MindcapComeUp` | Rises 0 → max | ~1 hr | Shared pre-valence. |
| `MindcapTripGood` | Starts at max, decays → 0 | ~7 hr | Good trip active impairment. |
| `MindcapTripBad` | Starts at max, decays → 0 | ~7 hr | Bad trip active impairment. |
| `MindcapAfterglow` | Starts at max, decays → 0 | ~3 days | Fading positive mood. |
| `MindcapDisturbed` | Starts at max, decays → 0 | ~3 days | Fading negative mood + social penalties. |

**Fluff (4 hediffs):**

| HediffDef | Severity | Duration | Role |
|-----------|----------|----------|------|
| `FluffComeUp` | Rises 0 → max | ~1 hr | Shared pre-valence. |
| `FluffTripGood` | Starts at max, decays → 0 | ~5 hr | Good trip. Low impairment, high mood. |
| `FluffTripBad` | Starts at max, decays → 0 | ~5 hr | Bad trip. Low impairment, negative mood. |
| `FluffRecovery` | Starts at max, decays → 0 | ~1 day | Universal hangover regardless of valence. |

### Hediff Chain Data Flow

The come-up hediff is the only custom class (`Hediff_PsychedelicComeUp`). It needs a custom class for two reasons that can't be cleanly handled by comps: dynamic severity capping (clamping severity rise to `maxSeverityCap` derived from tolerance) and runtime rate scaling (`comeUpRateMultiplier` for rituals). Peak hediffs use standard `HediffWithComps` with a `HediffComp_TripResolution` comp.

```
Hediff_PsychedelicComeUp (custom class)
  Runtime fields (set by IngestionOutcomeDoer, saved via ExposeData):
    float maxSeverityCap        — tolerance-derived ceiling
    bool wasZeroTolerance       — gates trait/inspiration rolls
    HediffDef nextHediffDef     — pre-determined peak hediff (good or bad)
    float comeUpRateMultiplier  — 1.0 normal, ~2.5 ritual
    TripValence tripValence     — Good or Bad, determined at ingestion
  Behavior:
    Severity rises via SeverityPerDay * comeUpRateMultiplier.
    On reaching maxSeverityCap: removes self, applies nextHediffDef
    at same severity, passes wasZeroTolerance + tripValence to
    HediffComp_TripResolution on the new hediff.

HediffComp_TripResolution (comp on peak hediffs)
  Comp properties:
    ThingDef drugDef            — for extension lookup (ONLY field)
  Runtime fields (set by come-up transition, saved via ExposeData):
    bool wasZeroTolerance
    TripValence tripValence
  Behavior:
    On parent hediff removal (severity 0): looks up drugDef →
    PsychedelicDrugExtension. Uses tripValence to select
    hediffDefPositiveResolution or hediffDefNegativeResolution.
    Fires resolution systems (post-trip hediff, Psychonaut progression,
    inspiration, trait modification, tale recording, recent trip thought).
```

### Supported Peak/Resolution Configurations

Passing valence explicitly supports all four combinations:

| Positive Peak | Negative Peak | Positive Resolution | Negative Resolution | Example |
|---|---|---|---|---|
| Different | Different | Different | Different | LYS, Mindcap |
| Different | Different | Same | Same | Fluff (universal recovery) |
| Same | Same | Different | Different | Shared peak, valence only matters at resolution |
| Same | Same | Same | Same | Minimal drug, valence is cosmetic |

### Fluff Peak Difference

Fluff peaks have LOW capacity penalties compared to LYS/Mindcap. Pawns remain functional.

| Capacity | LYS/Mindcap Peak | Fluff Peak |
|----------|-------------------|------------|
| Consciousness | -0.20 (setMax 0.8) | -0.05 |
| Sight | -0.60 | -0.10 |
| Moving | -0.40 (setMax 0.7) | -0.10 |
| Manipulation | -0.30 | -0.10 |
| Talking | -0.20 | +0.10 (talkative) |
| Hearing | -0.40 | -0.15 |

### Ingestion Flow

```
1.  PsychedelicIngestionOutcomeDoer fires
2.  Check tolerance severity
3.  If > noTripToleranceThreshold: send "no effect" letter, consume dose, RETURN
4.  Calculate maxSeverityCap from tolerance (1.0 at zero, scaled down)
5.  Store wasZeroTolerance flag
6.  Roll valence using sigmoid curve:
    → Input: pawn mood at ingestion, Psychonaut status, ritual context
    → Select normal or Psychonaut curve parameters
    → Evaluate sigmoid, add ritual bonus if applicable, clamp 0-1
    → Roll against result → Good or Bad
7.  Select nextHediffDef based on valence result
    → Good: hediffDefPositiveTrip (from extension)
    → Bad: hediffDefNegativeTrip (from extension)
8.  Apply hediffDefComeUp (from extension) at severity 0
    → Configure instance fields: maxSeverityCap, wasZeroTolerance,
       nextHediffDef, tripValence, comeUpRateMultiplier (1.0 default, ~2.5 ritual)
9.  Come-up severity rises over time
10. At maxSeverityCap: come-up removes self
11. Apply nextHediffDef at severity = maxSeverityCap
    → Pass wasZeroTolerance + tripValence to HediffComp_TripResolution
12. Peak severity decays through stages
13. At severity 0: hediff removed, HediffComp_TripResolution fires
    → RESOLUTION (see Section 7D)
```

---

## 5. VALENCE SYSTEM

### Overview
Trip valence (Good or Bad) is determined at ingestion via a sigmoid curve based on pawn mood. The roll happens in `PsychedelicIngestionOutcomeDoer` before the come-up hediff is applied.

### Inputs
- **Pawn mood at ingestion** (0–1 float)
- **Psychonaut trait** (bool — selects which curve to use)
- **Ritual context** (bool — adds flat bonus after curve evaluation)

### Sigmoid Curve
Standard logistic function:
```
f(mood) = L + (U - L) / (1 + exp(-k * (mood - m)))
```

Where L (lower asymptote) and k (steepness) are derived from the 4 modder-specified parameters:
```
k = 2.944 / (plateauPoint - transitionPoint)
S0 = 1 / (1 + exp(k * transitionPoint))
L = (chanceAtZeroMood - upperLimit * S0) / (1 - S0)
```

The value 2.944 = logit(0.95), meaning the plateau point is where the curve reaches 95% of its floor-to-ceiling range.

### XML Parameters (per drug, in PsychedelicDrugExtension)

4 parameters per curve × 2 curves (normal + Psychonaut) + 1 ritual bonus = **9 parameters per drug**.

#### Normal Pawn Curve

| Parameter | Description |
|-----------|-------------|
| `goodTripChanceAtZeroMood` | Exact good trip probability when pawn mood is 0%. |
| `goodTripChanceUpperLimit` | Maximum possible good trip probability (ceiling). |
| `goodTripTransitionPoint` | Mood at which the curve is steepest (inflection point). |
| `goodTripPlateauPoint` | Mood at which good trip probability reaches 95% of its ceiling. |

#### Psychonaut Pawn Curve

Same four parameters prefixed with `psychonaut`: `psychonautGoodTripChanceAtZeroMood`, `psychonautGoodTripChanceUpperLimit`, `psychonautGoodTripTransitionPoint`, `psychonautGoodTripPlateauPoint`.

#### Ritual Bonus

`ritualGoodTripChanceBonus` — flat additive bonus applied after curve evaluation, clamped 0–1. Only applied during Ideology rituals (Vision Quest, Group Ritual).

### Per-Drug Valence Defaults

**LYS** — Wide, steep curve. Low floor, high ceiling. Big difference between normal and Psychonaut.

**Mindcap** — Similar shape to LYS, slightly more generous floor. Mindcap's punishment is in outcomes (shorter afterglow, same cooldown) not in odds.

**Fluff** — Sharp transition concentrated in the break-risk zone. Above 35% mood, odds are essentially at ceiling. Psychonaut barely matters because the drug is already safe.

---

## 6. INSPIRATION SYSTEM

Inspirations are a zero-tolerance-only bonus that fires near the end of the peak hediff (entering the comedown region).

### XML Parameters (per drug, in PsychedelicDrugExtension)
```xml
<canGoodTripInspire>true</canGoodTripInspire>
<canBadTripInspire>false</canBadTripInspire>
<inspirationChance>0.15</inspirationChance>
```

### Logic
```
1. wasZeroTolerance? NO → skip
2. Trip valence?
   GOOD → check canGoodTripInspire → false: skip
   BAD  → check canBadTripInspire  → false: skip
3. Roll against inspirationChance → FAIL → skip
4. Grant random inspiration:
   If VSIE installed → use VSIE inspiration system
   Else → vanilla InspirationHandler.TryStartInspiration()
```

### Per-Drug Inspiration Defaults

| Property | LYS | Fluff | Mindcap |
|----------|-----|-------|---------|
| canGoodTripInspire | true | true | true |
| canBadTripInspire | false | false | true |
| inspirationChance | TBD | TBD | TBD |

Mindcap's `canBadTripInspire: true` reflects psilocybin's reputation for difficult-but-meaningful experiences.

---

## 7. TRAIT SYSTEM

### Overview
Two distinct mechanics:
1. **Psychonaut** — custom mod trait, earned through repeated use, modifies trip behavior via independent valence curve
2. **Vanilla trait manipulation** — trips can add/remove existing vanilla traits (zero tolerance only)

### 7A. Psychonaut Trait

**Acquisition**: Fires at **resolution** (end of peak hediff), not ingestion. Only drugs with `countsPsychonautExperience: true` contribute to the hidden `PsychedelicExperience` hediff.

**Eligibility**: Trip must be qualifying — tolerance ≤ `noTripToleranceThreshold` at ingestion.

**Progression**: Each qualifying trip adds 0.1 severity to `PsychedelicExperience` (cap: 0.7). Starting at severity 0.3, an increasing probability roll fires.

| Dose # | Severity | Chance |
|--------|----------|--------|
| 1st | 0.1 | 0% |
| 2nd | 0.2 | 0% |
| 3rd | 0.3 | 10% |
| 4th | 0.4 | 30% |
| 5th | 0.5 | 50% |
| 6th | 0.6 | 70% |
| 7th+ | 0.7 (cap) | 90% |

**Roll formula**: `Rand.Value < ((severity * 2) - 0.5)`

**On acquisition**: Pawn gains Psychonaut trait, PsychedelicExperience hediff removed. Tale `RP_GainedPsychonaut` recorded.

**Cross-drug accumulation**: Any drug with `countsPsychonautExperience: true` contributes to the same shared hediff.

### 7B. Vanilla Trait Manipulation

Two entry types: **spectrums** (degree range on a single TraitDef) and **standalones** (single trait add/remove). Direction determined by bias values (-1.0 to 1.0). Pre-filtered candidate pool with weighted random selection. Each drug maintains its own independent trait pool.

#### Spectrum Entry Fields

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `traitDef` | Yes | — | The TraitDef to modify. |
| `minDegree` | Yes | — | Lowest degree this drug can reach. |
| `maxDegree` | Yes | — | Highest degree this drug can reach. |
| `selectionWeight` | No | 1.0 | Relative weight in the trait pool. |
| `goodTripBias` | No | 0.0 | Direction bias on good trips. -1.0 = always lower, 1.0 = always higher. |
| `badTripBias` | No | 0.0 | Direction bias on bad trips. |
| `higherMessage` | No | auto | Notification when degree increases. |
| `lowerMessage` | No | auto | Notification when degree decreases. |

#### Standalone Entry Fields

| Field | Required | Default | Description |
|-------|----------|---------|-------------|
| `traitDef` | Yes | — | The TraitDef to add/remove. |
| `degree` | No | 0 | Specific degree. |
| `selectionWeight` | No | 1.0 | Relative weight in the trait pool. |
| `goodTripBias` | No | 0.0 | Add/remove bias on good trips. |
| `badTripBias` | No | 0.0 | Add/remove bias on bad trips. |
| `addedMessage` | No | auto | Notification when trait is added. |
| `removedMessage` | No | auto | Notification when trait is removed. |

### 7C. Locked Trait Values for Base Drugs

**LYS — Spectrums:**

| TraitDef | Min | Max | Weight | Good Bias | Bad Bias |
|----------|-----|-----|--------|-----------|----------|
| NaturalMood | -2 | 2 | 1.2 | 0.7 | -0.7 |
| Nerves | -1 | 2 | 1.0 | 0.6 | -0.7 |
| NervousnessTraitDef | 0 | 2 | 0.6 | -0.7 | 0.5 |
| DrugDesire | -1 | 2 | 0.5 | 0.3 | -0.8 |
| PsychicSensitivity | -1 | 1 | 0.3 | 0.4 | 0.0 |

**LYS — Standalones:**

| TraitDef | Degree | Weight | Good Bias | Bad Bias |
|----------|--------|--------|-----------|----------|
| Kind | 0 | 0.8 | 1.0 | -0.6 |
| Abrasive | 0 | 0.5 | -0.8 | 1.0 |
| Nudist | 0 | 0.15 | 0.6 | -0.3 |
| Ascetic | 0 | 0.4 | 0.8 | 0.4 |
| BodyPurist | 0 | 0.3 | 0.7 | 0.6 |
| BodyModder | 0 | 0.3 | 0.6 | 0.0 |

**Mindcap**: Same pool entries and same biases as LYS. Weights subject to independent tuning during balance pass.

**Fluff**: `<canModifyTraits>false</canModifyTraits>`

### 7D. Resolution Logic

Resolution fires when a peak hediff's severity reaches 0, triggered by `HediffComp_TripResolution`. The comp reads `tripValence` (passed from come-up) and looks up `drugDef` → `PsychedelicDrugExtension` to determine which resolution hediff to apply. Four independent systems evaluate in sequence. All can fire on the same trip.

```
RESOLUTION (HediffComp_TripResolution)

═══ 0. SAFETY CHECK (always, first) ═══

Before any resolution logic, check pawn state:
  → pawn == null? → abort
  → pawn.Dead? → abort
  → !pawn.Spawned? → abort (guest left map, caravan, etc.)
If aborted, no resolution systems fire. The peak hediff is still
removed normally by vanilla severity decay — only the RP resolution
logic is skipped. This prevents errors from tale recording,
thought application, or trait manipulation on invalid pawns.

═══ A. POST-TRIP HEDIFF (always) ═══

1. Look up drugDef → PsychedelicDrugExtension
2. Determine resolution hediff from tripValence:
   → Good → hediffDefPositiveResolution
   → Bad  → hediffDefNegativeResolution
3. Apply hediff at severity scaled by tolerance at ingestion

═══ B. PSYCHONAUT PROGRESSION (independent) ═══

1. countsPsychonautExperience on this drug? → NO: skip
2. Was trip qualifying (tolerance ≤ noTripToleranceThreshold at ingestion)? → NO: skip
3. Increment PsychedelicExperience hediff by 0.1 (cap 0.7)
   → If pawn doesn't have hediff, create it
4. Severity ≥ 0.3? → NO: skip roll
5. Roll: Rand.Value < ((severity * 2) - 0.5) → FAIL: skip
6. Grant Psychonaut trait
7. Remove PsychedelicExperience hediff
8. Record tale: RP_GainedPsychonaut

═══ C. INSPIRATION (independent) ═══

1. wasZeroTolerance? → NO: skip
2. Trip valence:
   GOOD → check canGoodTripInspire → false: skip
   BAD  → check canBadTripInspire  → false: skip
3. Roll against inspirationChance → FAIL: skip
4. Grant random inspiration:
   → VSIE installed → VSIE inspiration system
   → Else → vanilla InspirationHandler.TryStartInspiration()

═══ D. TRAIT MODIFICATION (independent) ═══

1.  wasZeroTolerance? → NO: skip
2.  canModifyTraits on this drug? → NO: skip
3.  Mod settings trait toggle ON? → NO: skip
4.  Is pawn Psychonaut?
    → YES: roll against traitChangeChancePsychonaut
    → NO:  roll against traitChangeChance
    → FAIL: skip

5.  Build pre-filtered candidate pool:

    FOR EACH spectrum entry:
      a. Pawn has this TraitDef at degree OUTSIDE [minDegree, maxDegree]?
         → exclude
      b. Build step sequence from degree range
      c. Find pawn's current step
      d. Select bias: good trip → goodTripBias, bad trip → badTripBias
      e. Roll direction: Rand.Value < (bias + 1) / 2 → HIGHER, else → LOWER
      f. Target step = current ± 1
      g. Target out of bounds? → exclude
      h. Would adding target trait conflict with pawn's other traits? → exclude
      i. Survives → add to pool with selectionWeight

    FOR EACH standalone entry:
      a. Select bias: good trip → goodTripBias, bad trip → badTripBias
      b. Roll action: Rand.Value < (bias + 1) / 2 → ADD, else → REMOVE
      c. ADD + pawn already has trait → exclude
      d. REMOVE + pawn doesn't have trait → exclude
      e. ADD + trait conflicts with existing traits → exclude
      f. Survives → add to pool with selectionWeight

6.  Pool empty? → skip
7.  Weighted random pick → selected entry
8.  Execute stored modification
9.  Apply trait change
10. Fire letter notification
11. Record tale: RP_TraitChangedByTrip

═══ E. TRIP RECORDING (always) ═══

1. Record tale via TaleRecorder.RecordTale()
   → TaleDef selected by valence: RP_HadGoodTrip or RP_HadBadTrip
   → Pawn + drug ThingDef passed as tale parameters
2. Apply/refresh RP_RecentTrip memory thought (15-day duration)
   → Zero mood, invisible, stackLimit 1
   → Consumed by Ideology Essential precept only
```

---

## 8. IDEOLOGY INTEGRATION (Separate Mod)

### Mod Structure
- **Mod**: RimPsychedelics - Ideology
- **Dependencies**: RimPsychedelics (base), Ideology DLC
- **Soft dependencies**: VSIE (for enhanced ritual social outcomes)

### Precepts

| Precept | Type | Effect |
|---------|------|--------|
| Psychedelic Use: Approved | Drug use | No guilt/mood penalty. Social approval. |
| Psychedelic Use: Disapproved | Drug use | −6 mood on use. Social disapproval. |
| Psychedelic Use: Prohibited | Drug use | −12 mood on use. Major social penalty. |
| Psychedelic Use: Essential | Drug use | +4 mood on use. −4 mood if `RP_RecentTrip` thought absent (>15 days since last trip). |
| Psychonaut: Exalted | Special | Psychonaut pawns get +opinion from believers. Role bonus. |

### Ritual Tolerance Failure
Tolerance is NOT a precondition — rituals proceed regardless. If any ingesting participant has >50% tolerance at the ingestion phase:
- Ritual immediately fails with negative outcome
- Message: "[Pawn] had dosed too recently to join the experience, and it made everyone uncomfortable."
- Effects: All participants/spectators receive mood debuff. No trips applied. Drug dose consumed.
- Exception: Psychedelic Party does NOT have this check (casual, not ceremonial).

### Ritual-Modified Trip Mechanics
Come-up hediff receives `comeUpRateMultiplier` (~2.5x) during Vision Quest or Group Ritual. Ritual bonus is applied as flat additive to valence roll.

| Drug | Come-Up (normal) | Come-Up (ritual ~2.5x) | Ritual Bonus |
|------|-----------------|----------------------|--------------|
| Mindcap | ~1 hr | ~25 min | +0.15 |
| Fluff | ~1 hr | ~25 min | +0.10 |
| LYS | ~3.5 hr | ~85 min | +0.15 |

### Rituals

**Vision Quest** — Single pawn, optional leader + spectators. ~3–5 hours. Seeker ingests, leader departs, seeker wanders outdoors solo, returns for sharing phase.

**Group Ritual** — Multi-pawn, optional leader (does NOT ingest) + spectators. ~3–4 hours. All participants ingest with ritual-modified come-up.

**Psychedelic Party** — Social gathering. ~1–2 hours. No tolerance failure check. No ritual-modified come-up. No ritual valence bonus. Standard party buffs.

---

## 9. VSIE INTEGRATION (Soft Dependency)

### Implementation
- All VSIE code in `Compat/VSIE_Compat.cs`
- Runtime check: `ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded")`
- `[MayRequireMod("vanillaexpanded.vsie")]` attributes on relevant classes

### Base Mod — VSIE Memories

| Memory | Trigger | Valence | Notes |
|--------|---------|---------|-------|
| **Shared a trip** | Two pawns both have active trip hediffs, nearby | Per-pawn valence-dependent | Asymmetric outcomes possible. Also records `RP_SharedTrip` tale. |
| **Talked about a good trip** | Initiator has recent good trip thought | Mild positive for listener | Spreads the afterglow. |
| **Talked about a bad trip** | Initiator has recent bad trip thought | Mild negative for listener, small positive for initiator | Processing the experience. |
| **Bonded on Fluff** | Both pawns on Fluff, nearby | Strong positive for both | Fluff's empathogenic nature. |

### Ideology Mod — VSIE Memories

| Memory | Trigger | Valence | Notes |
|--------|---------|---------|-------|
| **Group ritual together** | Both participated in Group Ritual | Always positive | Ceremonial context. |
| **Psychedelic party together** | Both attended Psychedelic Party | Per-pawn valence-dependent | Casual. |

### Inspiration Bridge
When granting inspiration: if VSIE installed → use VSIE's inspiration system. Else → vanilla `InspirationHandler.TryStartInspiration()`.

---

## 10. ART INTEGRATION (Tales)

Art descriptions use vanilla's Tale system. Tales are recorded at resolution and picked up by the art generator when pawns create sculptures, engravings, etc.

### TaleDefs

| TaleDef | taleClass | Type | baseInterest | Trigger |
|---------|-----------|------|-------------|---------|
| `RP_HadGoodTrip` | Tale_SinglePawnAndDef | Volatile | 15 | Good peak resolution |
| `RP_HadBadTrip` | Tale_SinglePawnAndDef | Volatile | 15 | Bad peak resolution |
| `RP_GainedPsychonaut` | Tale_SinglePawn | PermanentHistorical | 25 | Psychonaut trait granted |
| `RP_TraitChangedByTrip` | Tale_SinglePawnAndDef | Volatile | 20 | Trait modification fires |
| `RP_SharedTrip` | Tale_DoublePawn | Volatile | 12 | Two pawns with active trip hediffs nearby |

`Tale_SinglePawnAndDef` stores pawn + ThingDef (drug). Art grammar references `[def_label]` for drug name.

Each TaleDef has a `rulePack` with `rulesStrings` covering: `tale_noun` (title), `image` (scene descriptions), `desc_sentence` (detail), `circumstance_phrase` (atmospheric color). Full grammar text is Phase 9 work.

### C# Integration

Tales recorded via `TaleRecorder.RecordTale()` at resolution time. Referenced via `RP_TaleDefOf` class with `[DefOf]` attribute.

---

## 11. CONFIRMED DESIGN DECISIONS

1. **No addiction**: None of the three drugs have addiction mechanics.
2. **Tolerance**: All three drugs build to 100% per dose. LYS and Mindcap share 10-day cooldown. Fluff has 3-day cooldown. Trip cutoff threshold is per-drug via `noTripToleranceThreshold`. Zero tolerance required for inspiration and trait modification.
3. **Processing**: Vanilla drug lab for LYS/Fluff. Drug lab or crafting spot for Mindcap.
4. **No trip visuals**: No screen overlays or shaders. Effects communicated via hediffs, mood, and thoughts.
5. **Mod settings**: Single toggle — enable/disable trait changes (default: on). All other tuning in XML.
6. **Body size**: Ignored. Custom IngestionOutcomeDoer never factors body size.
7. **Drug categories**: LYS and Mindcap = Hard. Fluff = Social.
8. **Binary valence**: Trips are Good or Bad. No neutral.
9. **Sequential hediffs**: Come-up (rising) → Peak positive/negative (decaying) → Resolution positive/negative (decaying over days). All five hediff references defined in the PsychedelicDrugExtension — the extension is the single source of truth for the hediff chain.
10. **5 hediffs per drug** for LYS/Mindcap. **4 hediffs for Fluff** (shared recovery).
11. **Equal impairment duration**: Good and bad trips have identical peak duration and capacity penalties per drug. Valence affects mood/social/mental states, not physical impairment.
12. **Fluff is different**: Low impairment during peak, high mood effects, universal hangover regardless of valence.
13. **Valence roll at ingestion**: Determined by sigmoid curve before come-up hediff is applied. Based on mood, Psychonaut status, and ritual context only.
14. **Independent tolerance**: No cross-tolerance between drugs.
15. **Sassafras**: Fully implemented in XML via vanilla `choppedThingDef` + `StumpBase`. No custom C#.
16. **Ideology**: Separate dependent mod.
17. **DrugCategory extension**: Future optional module, not in base mod.
18. **Resolution systems**: Independent. Psychonaut progression, inspiration, and trait modification all evaluate independently. A single trip can trigger all three.
19. **Psychonaut progression**: Fires at resolution, not ingestion. Cross-drug accumulation via shared PsychedelicExperience hediff.
20. **Trait modification types**: Spectrums and standalones with bias-driven direction and weighted random selection.
21. **Trait fields in XML**: Sensible defaults. Auto-generated notification messages when not specified.
22. **No trip tracker**: Trip history is not stored as a GameComponent. Art descriptions use vanilla TaleDefs recorded at resolution. Ideology Essential precept uses an invisible 15-day memory thought (`RP_RecentTrip`). All other systems use hediffs and thoughts.
23. **Pawn generation safety**: All ChemicalDefs set `onGeneratedAddictedToleranceChance` to `0.0`. Dummy addiction hediffs use `hediffClass` directly, not `ParentName="AddictionBase"`.
24. **Folder structure**: Defs organized by domain (Drugs/, Core/, Plants/, Production/) not by def type. Each drug's complete definition lives in a single file. Documented in README.md.
25. **Extension is single source of truth**: The `PsychedelicDrugExtension` defines the complete hediff chain. Resolution hediff mappings are NOT duplicated on comp properties. Valence is passed explicitly through the chain (come-up → resolution comp) to support shared peak HediffDefs.
26. **Resolution safety check**: Resolution logic aborts silently if the pawn is null, dead, or despawned. The peak hediff is still removed by vanilla severity decay — only RP resolution systems (post-trip hediff, Psychonaut progression, inspiration, trait modification, tales, thoughts) are skipped. This prevents errors from pawns dying mid-trip, guests leaving the map, or caravanning out.
27. **No custom mental states**: Trips do not impose mental states. Pawn behavior during trips is communicated through hediff stages (capacity penalties, labels) and mood effects. Pawns remain draftable, can respond to emergencies, eat, use the bathroom, and participate in social interactions while tripping. This avoids long lockouts (16+ hour LYS peaks), preserves compatibility with DBH/Hospitality/VSIE, and prevents redundancy with the hediff stage system.

---

## 12. C# CLASS STRUCTURE

```
Source/RimPsychedelics/
├── Core/
│   ├── RimPsychedelicsMod.cs                   — Mod entry, Harmony init, mod settings (trait toggle)
│   ├── TripValence.cs                          — Enum: Good, Bad
│   ├── PsychedelicDrugExtension.cs             — DefModExtension: per-drug XML parameters
│   │                                              (hediff refs [5 fields, single source of truth],
│   │                                              valence curves [9 params],
│   │                                              tolerance config,
│   │                                              countsPsychonautExperience,
│   │                                              inspiration settings,
│   │                                              trait modification pools)
│   ├── ValenceCurveEvaluator.cs                — Sigmoid math: derive L and k from XML params,
│   │                                              evaluate curve, apply ritual bonus, clamp
│   ├── RP_TaleDefOf.cs                         — [DefOf] class: references to custom TaleDefs
│   ├── RP_ThoughtDefOf.cs                      — [DefOf] class: RP_RecentTrip reference
│   ├── TraitSpectrumEntry.cs                   — Data class: spectrum trait pool entry
│   └── TraitStandaloneEntry.cs                 — Data class: standalone trait pool entry
├── Hediffs/
│   ├── Hediff_PsychedelicComeUp.cs             — Custom hediff: rising severity, dynamic cap,
│   │                                              rate multiplier, transition to peak,
│   │                                              passes wasZeroTolerance + tripValence.
│   │                                              ExposeData() saves all runtime fields.
│   ├── HediffCompProperties_TripResolution.cs  — Comp properties: drugDef only (single field)
│   └── HediffComp_TripResolution.cs            — On peak hediff removal: looks up extension
│                                                  via drugDef, selects resolution hediff by
│                                                  tripValence, fires all resolution systems,
│                                                  records tales, refreshes RP_RecentTrip thought.
├── Ingestion/
│   └── PsychedelicIngestionOutcomeDoer.cs      — Entry point: tolerance check, valence roll,
│                                                  apply come-up with configured instance fields.
├── Thoughts/
│   └── ThoughtWorker_OnTrip.cs                 — Situational mood (if needed beyond ThoughtWorker_Hediff)
├── Traits/
│   ├── TraitLogic.cs                           — Utility: safe trait add/remove, conflict checks
│   └── TraitNotification.cs                    — Utility: token substitution for notifications
├── Harmony/
│   ├── HarmonyPatches.cs                       — Patch registration
│   ├── Patch_TraitNotification.cs              — Letter when pawn gains/loses trait from trip
│   └── Patch_TraitConflicts.cs                 — Prevent conflicting trait combinations
└── Compat/
    └── VSIE_Compat.cs                          — Soft dependency: social interactions (TBD)
```

```
Source/RimPsychedelics_Ideology/
├── Rituals/
│   ├── RitualBehaviorWorker_VisionQuest.cs
│   ├── RitualBehaviorWorker_GroupTrip.cs
│   ├── RitualOutcomeEffectWorker_VisionQuest.cs
│   ├── RitualOutcomeEffectWorker_GroupTrip.cs
│   └── RitualOutcomeEffectWorker_PsychedelicParty.cs
└── Precepts/
    └── PreceptComp_PsychedelicUse.cs           — Essential precept checks for absence of RP_RecentTrip
```

---

## 13. FOLDER STRUCTURE

### Base Mod
```
RimPsychedelics/
├── About/
│   ├── About.xml
│   ├── Preview.png
│   └── Manifest.xml
├── Defs/
│   ├── Drugs/
│   │   ├── Drug_LYS.xml              (ChemicalDef, dummy addiction, tolerance, 5 hediffs, ThingDef + extension)
│   │   ├── Drug_Fluff.xml            (ChemicalDef, dummy addiction, tolerance, 4 hediffs, ThingDef + extension)
│   │   └── Drug_Mindcap.xml          (ChemicalDef, dummy addiction, tolerance, 5 hediffs, ThingDef + extension)
│   ├── Core/
│   │   ├── Hediffs_PsychedelicExperience.xml
│   │   ├── Traits_Psychonaut.xml
│   │   ├── Tales_Psychedelics.xml
│   │   └── Thoughts_Trips.xml         (includes RP_RecentTrip + trip-related thoughts)
│   ├── Plants/
│   │   ├── Plants_Ergo.xml
│   │   ├── Plants_SassafrasTree.xml
│   │   └── Plants_MindcapMushroom.xml
│   └── Production/
│       ├── Resources_Precursors.xml
│       ├── Recipes_DrugProcessing.xml
│       └── Research_Psychedelics.xml
├── Patches/
│   ├── Patches_Biomes.xml
│   └── Patches_VSIE.xml
├── Textures/
│   ├── Things/
│   │   ├── Plant/
│   │   ├── Items/
│   │   └── Drug/
│   └── UI/
├── Assemblies/
│   └── RimPsychedelics.dll
├── README.md
└── Source/
    └── RimPsychedelics/
```

### Folder Structure Rationale (documented in README.md)

- **`Defs/Drugs/`** — One file per drug. Contains the complete definition: ChemicalDef, dummy addiction hediff, tolerance hediff, full hediff chain, and drug ThingDef with extension. Everything needed to understand or tune a single drug is in one place.
- **`Defs/Core/`** — Cross-cutting defs shared across all drugs: PsychedelicExperience hediff, Psychonaut trait, TaleDefs, shared thoughts.
- **`Defs/Plants/`** — Source plant defs.
- **`Defs/Production/`** — Precursor resources, processing recipes, research projects.

XPath patches are unaffected by file organization — they operate on the loaded def database by defName, not file path.

### Ideology Expansion
```
RimPsychedelics_Ideology/
├── About/
│   ├── About.xml                      (dependency: RimPsychedelics + Ideology DLC)
│   └── Manifest.xml
├── Defs/
│   ├── PreceptDefs/
│   │   └── Precepts_Psychedelics.xml
│   ├── RitualDefs/
│   │   ├── Ritual_VisionQuest.xml
│   │   ├── Ritual_GroupTrip.xml
│   │   └── Ritual_PsychedelicParty.xml
│   └── ThoughtDefs/
│       └── Thoughts_Rituals.xml
├── Assemblies/
│   └── RimPsychedelics_Ideology.dll
└── Source/
    └── RimPsychedelics_Ideology/
```

---

## 14. BUILD ORDER

| Phase | Focus | Deliverable |
|-------|-------|-------------|
| **Phase 1** | Mindcap pipeline | XML: mushroom plant, harvested mindcap, dried mindcap. Vanilla-style placeholder drug effects. Verify grow → harvest → dry → ingest loop. |
| **Phase 2** | LYS pipeline | XML: Ergo plant, ergo resource, LYS drug + recipe (neutroamine). Research project. |
| **Phase 3** | Sassafras + Fluff | XML for tree/stump. Add Fluff drug + recipe. Biome patches. |
| **Phase 4** | Trip system core | C#: `Hediff_PsychedelicComeUp`, `PsychedelicIngestionOutcomeDoer`, `ValenceCurveEvaluator`, `HediffComp_TripResolution` (drugDef only), `PsychedelicDrugExtension`. Placeholder TaleDefs with minimal grammar. `RP_TaleDefOf`, `RP_ThoughtDefOf`. `RP_RecentTrip` thought def. Replace placeholder effects with sequential hediff trips. |
| **Phase 5** | Trait system | C#: Psychonaut progression, `TraitLogic`, vanilla trait manipulation, Harmony patches for notifications. |
| **Phase 6** | Tolerance refinement | Wire tolerance cap into come-up hediff, post-trip duration scaling, >50% cutoff. |
| **Phase 7** | Ideology mod | Precepts (Essential checks for `RP_RecentTrip` absence), ritual defs, `RitualBehaviorWorker`s, `RitualOutcomeEffectWorker`s. Ritual tolerance failure. Ritual valence bonus. |
| **Phase 8** | VSIE integration | Soft dependency, social interactions, inspiration system bridge. `RP_SharedTrip` tale recording. |
| **Phase 9** | Polish & balance | Tale grammar writing (full `rulesStrings` for all TaleDefs — pure XML, no code changes). Weighted trait list tuning. Balance pass. Mod settings UI. Steam Workshop prep. |

---

## 15. OPEN QUESTIONS

1. **Psychonaut trait XML definition** — stat offsets, spectrum, description
2. **Ergo and Mindcap plant defs** — grow days, fertility, yield (sassafras is done)
3. **Animal interactions** — can animals eat Mindcap mushrooms or Ergo?
4. **Inspiration chance values** — per-drug TBD
5. **Fluff recipe** — Safrole → Fluff processing details, research cost
6. **Mindcap research** — trivial or none?
7. **Vanilla TraitDef degree verification** — confirm actual defName and degree values for: NaturalMood, Nerves, NervousnessTraitDef, DrugDesire, PsychicSensitivity, Kind, Abrasive, Nudist, Ascetic, BodyPurist, BodyModder
8. **Psychonaut progression: should Fluff contribute?** — (`countsPsychonautExperience` currently true for all three)
