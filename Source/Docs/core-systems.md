# RimPsychedelics — Core Systems

## Purpose

This document defines how the mechanical systems of RimPsychedelics work: tolerance, the hediff chain lifecycle, valence determination, inspiration, Psychonaut progression, and trait modification. It describes the algorithms, decision logic, and data flow between systems.

**This is not an implementation reference.** For C# class structure and responsibilities, see C# Architecture. For XML def structure and wiring, see XML Architecture. For per-drug parameter values, see Drug Profiles.

---

## Table of Contents

1. [Tolerance](#1-tolerance)
2. [Hediff Chain Lifecycle](#2-hediff-chain-lifecycle)
3. [Valence System](#3-valence-system)
4. [Resolution](#4-resolution)
5. [Psychonaut Progression](#5-psychonaut-progression)
6. [Inspiration](#6-inspiration)
7. [Trait Modification](#7-trait-modification)

---

## 1. Tolerance

### Behavior

Each RP drug has its own independent tolerance tracked via a standard hediff. There is no cross-tolerance between drugs — taking Mindcap does not affect LYS tolerance.

Every dose applies 1.0 tolerance severity regardless of existing tolerance level. Tolerance decays over time at a per-drug rate defined in XML.

### Effects of Tolerance

Tolerance affects the trip in three ways:

**Trip gating**: Each drug defines a `noTripToleranceThreshold` (typically 0.5). If the pawn's tolerance severity exceeds this threshold at ingestion, the dose is consumed and wasted — no trip occurs. A "no effect" letter is sent to the player.

**Intensity scaling**: Tolerance severity at ingestion determines the `maxSeverityCap` on the come-up hediff. At zero tolerance, the cap is 1.0 (full intensity). At higher tolerance (but still below the threshold), the cap is scaled down, resulting in a weaker peak and shorter effective duration.

**Zero-tolerance gating**: Inspiration rolls and trait modification rolls only fire if tolerance was exactly zero at ingestion. This is tracked as a boolean (`wasZeroTolerance`) passed through the hediff chain. Pawns who dose before tolerance fully clears can still trip, but miss the chance for lasting effects.

### Body Size

Explicitly ignored. The custom IngestionOutcomeDoer bypasses vanilla's body-size-based drug calculations entirely. A thrumbo and a child on the same drug at the same tolerance get the same trip.

### Inherent Tolerance (Biotech Genes)

Biotech genes that grant inherent drug tolerance or modify metabolism (e.g., `DrugMetabolism` spectrum, `AddictionResistance`) may prevent a pawn from ever reaching zero tolerance. Such pawns can trip with reduced intensity but will never experience trait changes or inspirations. Full gene interaction audit is an open item.

---

## 2. Hediff Chain Lifecycle

### Overview

Every RP trip follows the same three-phase hediff chain:

```
ComeUp (severity rises) → Peak (severity decays) → Resolution (fires at peak removal)
                                                      → Post-Trip hediff (severity decays over days)
```

The come-up hediff is the only custom class. Peak and post-trip hediffs are standard `HediffWithComps`. Resolution is a comp on the peak hediff that fires when the peak is removed (severity reaches 0).

### Phase 1: Ingestion

The `PsychedelicIngestionOutcomeDoer` is the entry point. It runs the following sequence:

```
1. Read pawn's current tolerance severity
2. Tolerance > noTripToleranceThreshold?
   → YES: send "no effect" letter, RETURN (dose consumed, no trip)
3. Calculate maxSeverityCap from tolerance
   → 1.0 at zero tolerance, scaled down at higher tolerance
4. Store wasZeroTolerance = (tolerance severity == 0)
5. Roll valence (see Section 3)
   → Result: Good or Bad
6. Look up PsychedelicDrugExtension on the drug ThingDef
7. Select peak hediff from extension:
   → Good: hediffDefPositiveTrip
   → Bad: hediffDefNegativeTrip
8. Apply hediffDefComeUp at severity 0
9. Configure come-up instance fields:
   → maxSeverityCap (from step 3)
   → wasZeroTolerance (from step 4)
   → nextHediffDef (from step 7)
   → tripValence (from step 5)
   → comeUpRateMultiplier (1.0 default; ~2.5 if ritual context)
```

All decisions are made at ingestion. The rest of the chain executes what was determined here.

### Phase 2: Come-Up

The come-up hediff (`Hediff_PsychedelicComeUp`) is a custom class that manages rising severity with a dynamic cap.

**Tick behavior:**
- Severity increases each tick by `SeverityPerDay * comeUpRateMultiplier` (converted to per-tick).
- Severity is clamped to `maxSeverityCap` each tick.
- When severity reaches `maxSeverityCap`: the come-up removes itself and applies the peak hediff.

**Transition to peak:**
1. Safety check: pawn null or dead → abort transition (come-up hediff removed, no peak applied).
2. Remove come-up hediff from pawn.
3. Apply `nextHediffDef` (the pre-selected peak hediff) at severity = `maxSeverityCap`.
4. Find `HediffComp_TripResolution` on the new peak hediff.
5. Pass `wasZeroTolerance` and `tripValence` to the comp's runtime fields.

### Phase 3: Peak

The peak hediff is a standard `HediffWithComps` with decaying severity. It progresses through its stages as severity drops (pawn experiences highest stage first, lowest stage last).

The peak hediff carries `HediffComp_TripResolution`, which holds `wasZeroTolerance` and `tripValence` as saved runtime fields, plus `drugDef` from its comp properties.

When severity reaches 0, vanilla removes the hediff, triggering the comp's `CompPostPostRemoved` — which fires the resolution sequence (see Section 4).

### Data Flow Summary

```
IngestionOutcomeDoer
  │  reads: tolerance, mood, Psychonaut status, ritual context
  │  decides: maxSeverityCap, wasZeroTolerance, valence, nextHediffDef
  │
  ▼
Hediff_PsychedelicComeUp (instance fields set by doer)
  │  carries: maxSeverityCap, wasZeroTolerance, nextHediffDef,
  │           tripValence, comeUpRateMultiplier
  │
  ▼  (at maxSeverityCap: remove self, apply peak)
  │
HediffComp_TripResolution (on peak hediff, fields set by come-up)
  │  carries: wasZeroTolerance, tripValence
  │  reads from XML: drugDef → PsychedelicDrugExtension
  │
  ▼  (at severity 0: resolution fires)
  │
Resolution sequence (Section 4)
```

All runtime fields are saved via `ExposeData()` to survive save/load.

---

## 3. Valence System

### Overview

Trip valence (Good or Bad) is determined at ingestion via a sigmoid curve evaluated against the pawn's current mood. The result is a probability of a good trip — a random roll against that probability determines the outcome.

### Inputs

| Input | Source | Effect |
|-------|--------|--------|
| Pawn mood | `pawn.needs.mood.CurLevelPercentage` (0.0–1.0) | Primary input to the sigmoid curve |
| Psychonaut trait | `pawn.story.traits` check | Selects which curve parameters to use |
| Ritual context | Passed by Ideology ritual workers | Adds flat bonus after curve evaluation |

### The Sigmoid Curve

Standard logistic function parameterized by four modder-friendly values:

```
f(mood) = L + (U - L) / (1 + exp(-k * (mood - m)))
```

**Derived constants:**

```
k = 2.944 / (plateauPoint - transitionPoint)
S0 = 1 / (1 + exp(k * transitionPoint))
L = (chanceAtZeroMood - upperLimit * S0) / (1 - S0)
U = upperLimit
m = transitionPoint
```

The value `2.944 = logit(0.95)`, meaning the plateau point is where the curve reaches 95% of its floor-to-ceiling range.

### Parameters (per drug, up to 9)

Each drug defines a normal pawn curve (required) and optionally a Psychonaut curve. If Psychonaut parameters are omitted, the normal curve is used for both. A ritual bonus is also optional.

**Normal pawn curve (4 parameters, required):**

| Parameter | Meaning |
|-----------|---------|
| `goodTripChanceAtZeroMood` | Exact good trip probability at 0% mood (floor) |
| `goodTripChanceUpperLimit` | Maximum good trip probability (ceiling) |
| `goodTripTransitionPoint` | Mood where the curve is steepest (inflection) |
| `goodTripPlateauPoint` | Mood where probability effectively reaches ceiling |

**Psychonaut curve (4 parameters, optional):** Same structure, prefixed with `psychonaut`. If any are omitted, the corresponding normal curve parameter is used as the fallback. This means a modder can override just one or two Psychonaut parameters, or omit all four to have Psychonaut status not affect valence for their drug.

**Ritual bonus (1 parameter, optional):** `ritualGoodTripChanceBonus` — flat additive after curve evaluation, clamped 0.0–1.0. Defaults to 0.0 if omitted.

### Evaluation Flow

```
1. Is pawn a Psychonaut AND does this drug define psychonaut curve parameters?
   → YES: use psychonaut values (falling back to normal for any omitted parameter)
   → NO:  use normal curve parameters
2. Evaluate sigmoid: goodTripChance = f(mood)
3. Is this a ritual context?
   → YES: goodTripChance += ritualGoodTripChanceBonus
4. Clamp goodTripChance to [0.0, 1.0]
5. Roll: Rand.Value < goodTripChance
   → YES: valence = Good
   → NO:  valence = Bad
```

### Curve Design Intuition

- **`chanceAtZeroMood`**: How forgiving is this drug to miserable pawns? Higher = safer.
- **`upperLimit`**: Best possible odds. Below 1.0 means even happy pawns have some bad trip risk.
- **`transitionPoint`**: Where does mood start to matter? Low values concentrate the curve's work in the low-mood range. High values mean mood doesn't matter much until the pawn is already content.
- **`plateauPoint`**: Where does mood stop mattering? The gap between transition and plateau controls steepness — narrow gap = sharp transition, wide gap = gradual.

### Design Archetypes

**Safe drug** (high floor, low transition): Good odds for almost everyone. Mood barely matters above break-risk range.

**Mood-dependent drug** (low floor, centered transition): Every point of mood matters. Depressed pawns face real danger, happy pawns are rewarded.

**Dangerous drug** (very low floor, moderate ceiling, high transition): Bad trips are common even at decent mood. Only very happy pawns get good odds.

---

## 4. Resolution

Resolution fires when a peak hediff's severity reaches 0, triggered by `HediffComp_TripResolution.CompPostPostRemoved`. It orchestrates all end-of-trip systems.

### Tiered Safety Check (always first)

Resolution uses a tiered check that allows most operations to fire on despawned pawns (caravan, guests who left, etc.) while protecting against null/dead states:

**Tier 1 — pawn null or dead**: Abort everything. No resolution systems fire. The peak hediff is still removed normally by vanilla severity decay.

**Tier 2 — pawn alive but despawned**: Post-trip hediff, Psychonaut progression, trait modification, and RP_RecentTrip thought all fire normally — these operate on the pawn object directly and don't require a map or position. Inspiration and tale recording are skipped (inspiration is a map-level system; tales can reference map position).

**Tier 3 — pawn alive and spawned**: Everything fires normally.

### Resolution Sequence

Five independent systems evaluate in order. All can fire on the same trip (subject to tier eligibility).

```
A. Post-Trip Hediff ────── Tier 2+ (pawn alive)
B. Psychonaut Progression ─ Tier 2+ (pawn alive), see Section 5
C. Inspiration ──────────── Tier 3 only (pawn spawned), see Section 6
D. Trait Modification ───── Tier 2+ (pawn alive), see Section 7
E. Trip Recording ──────── Tier 3 only (pawn spawned)
```

#### A. Post-Trip Hediff (Tier 2+)

1. Look up `drugDef` → `PsychedelicDrugExtension`.
2. Select resolution hediff based on `tripValence`:
   - Good → `hediffDefPositiveResolution`
   - Bad → `hediffDefNegativeResolution`
3. Apply hediff at severity scaled by tolerance at ingestion.

#### E. Trip Recording (Tier 3 only)

1. Record tale via `TaleRecorder.RecordTale()`:
   - Good trip → `RP_HadGoodTrip` (pawn + drug ThingDef)
   - Bad trip → `RP_HadBadTrip` (pawn + drug ThingDef)
2. Apply/refresh `RP_RecentTrip` memory thought (15-day duration, zero mood, invisible, stackLimit 1). This thought exists solely as a flag for the Ideology Essential precept.

Note: `RP_RecentTrip` thought is applied at Tier 2+ (pawn alive), even though tale recording requires Tier 3. A caravanning pawn still gets their recent trip thought refreshed.

---

## 5. Psychonaut Progression

### Overview

Psychonaut is a custom mod trait earned through repeated psychedelic use. It provides an independent, more favorable valence curve and a reduced trait modification chance.

Progression is tracked by a hidden hediff (`RP_PsychedelicExperience`) that accumulates severity across qualifying trips on any contributing drug.

### Eligibility

Psychonaut progression has two independent parts — experience accumulation and the acquisition roll — with separate eligibility:

**Experience accumulation** fires if:
1. The drug's `psychonautExperienceGain` is greater than 0.
2. The trip was qualifying — tolerance was at or below `noTripToleranceThreshold` at ingestion.
3. The pawn does not already have the Psychonaut trait.

**Acquisition roll** fires if all of the above are true AND:
4. The drug's `countsPsychonautExperience` is `true`.

### Accumulation

Each qualifying trip adds severity to `RP_PsychedelicExperience` based on the drug's `psychonautExperienceGain` field (per-drug, configurable in XML). The hediff has a hardcoded maximum severity of 0.7 — values are clamped on application. The hediff has no `SeverityPerDay` — it does not decay. It is only modified by resolution logic and removed when Psychonaut is granted.

Cross-drug accumulation: any drug with `psychonautExperienceGain` > 0 contributes to the same shared hediff. Three Mindcap trips and two Fluff trips all add to the same counter, even if some of those drugs don't trigger the acquisition roll.

### Acquisition Roll

Starting at severity 0.3, a probability roll fires at each resolution — but only if the drug's `countsPsychonautExperience` is `true`. A drug can contribute experience without ever triggering the roll itself; the accumulated experience is still available when a qualifying drug fires the check.

| Trips | Severity | Chance |
|-------|----------|--------|
| 1 | 0.1 | 0% |
| 2 | 0.2 | 0% |
| 3 | 0.3 | 10% |
| 4 | 0.4 | 30% |
| 5 | 0.5 | 50% |
| 6 | 0.6 | 70% |
| 7+ | 0.7 (cap) | 90% |

**Formula**: `Rand.Value < ((severity * 2) - 0.5)`

At severity < 0.3, the formula yields a negative threshold — the roll automatically fails without being evaluated.

### On Acquisition

1. Grant Psychonaut trait to pawn.
2. Remove `RP_PsychedelicExperience` hediff entirely.
3. Record tale: `RP_GainedPsychonaut`.

### Effects of Psychonaut Trait

- Valence curve: the drug's Psychonaut curve parameters are used instead of the normal curve. Typically higher floor, higher ceiling, lower transition point — better odds across the board.
- Trait modification: `traitChangeChancePsychonaut` is used instead of `traitChangeChance`. Typically much lower — the pawn's personality has stabilized.

---

## 6. Inspiration

### Overview

Qualifying trips can grant a random vanilla inspiration at resolution time. This is a zero-tolerance-only bonus.

### Eligibility

Inspiration fires if all of the following are true:
1. `wasZeroTolerance` is true (tolerance was exactly 0 at ingestion).
2. The drug allows inspiration for this valence:
   - Good trip → `canGoodTripInspire` must be true
   - Bad trip → `canBadTripInspire` must be true
3. Random roll succeeds against `inspirationChance`.

### Granting

If eligible, a random inspiration is granted:
- If VSIE is installed → use VSIE's inspiration system.
- Otherwise → vanilla `InspirationHandler.TryStartInspiration()`.

### Per-Drug Variation

Most drugs only allow good trip inspiration (`canBadTripInspire: false`). Mindcap is an exception — it allows bad trip inspiration, reflecting psilocybin's reputation for difficult-but-meaningful experiences.

---

## 7. Trait Modification

### Overview

Psychedelic trips can permanently modify a pawn's vanilla traits. The system supports two entry types: **spectrums** (moving along a degree range within a single TraitDef) and **standalones** (adding or removing a single trait).

Each drug defines its own independent trait pool. Trait modification is entirely optional — drugs can disable it by setting `canModifyTraits` to `false`.

### Eligibility

Trait modification fires if all of the following are true:
1. `wasZeroTolerance` is true.
2. The drug's `canModifyTraits` is true.
3. The mod settings trait toggle is on (player-controlled, default on).
4. Random roll succeeds:
   - Psychonaut pawn → roll against `traitChangeChancePsychonaut`
   - Non-Psychonaut → roll against `traitChangeChance`

### The Bias System

Both spectrum and standalone entries use a bias value from -1.0 to 1.0 to determine direction. The bias used depends on trip valence: `goodTripBias` for good trips, `badTripBias` for bad trips.

**Conversion to probability:**

```
Chance of "higher" (spectrum) or "add" (standalone) = (bias + 1) / 2
```

| Bias | Higher/Add | Lower/Remove |
|------|-----------|-------------|
| 1.0 | 100% | 0% |
| 0.5 | 75% | 25% |
| 0.0 | 50% | 50% |
| -0.5 | 25% | 75% |
| -1.0 | 0% | 100% |

**Bias follows vanilla degree numbering, not intuitive "good/bad" labels.** For traits where higher degree means a worse outcome (e.g., NervousnessTraitDef where +2 = Very Neurotic), a good trip that *reduces* neuroticism needs a **negative** `goodTripBias`.

### Candidate Pool Construction

After the eligibility roll succeeds, the system builds a pre-filtered pool of valid modifications. Each entry is evaluated independently — the direction/action is rolled per-entry using the bias, then checked for validity.

**For each spectrum entry:**

1. Does the pawn have this TraitDef at a degree outside `[minDegree, maxDegree]`? → Exclude (treated as conflict — the drug doesn't interact with degrees it doesn't define).
2. Build the step sequence from the degree range. Degree 0 (no trait) is implicitly included if the range spans across it.
3. Find the pawn's current position in the step sequence.
4. Roll direction using bias → HIGHER or LOWER.
5. Calculate target: current position ± 1 step.
6. Target out of bounds (beyond min or max of the step sequence)? → Exclude.
7. Would adding the target degree's trait conflict with the pawn's other traits? → Exclude.
8. Survives all checks → add to pool with `selectionWeight`.

**For each standalone entry:**

1. Roll action using bias → ADD or REMOVE.
2. ADD + pawn already has the trait → Exclude.
3. REMOVE + pawn doesn't have the trait → Exclude.
4. ADD + trait conflicts with pawn's existing traits → Exclude.
5. Survives all checks → add to pool with `selectionWeight`.

### Selection and Application

1. Pool empty? → No trait change occurs (not an error — edge case when all entries are filtered out).
2. Weighted random selection from pool.
3. Apply the pre-determined modification (the direction/action was already decided during pool construction).
4. Send notification letter to player (custom message from XML, or auto-generated default).
5. Record tale: `RP_TraitChangedByTrip`.

### Spectrum Step Sequence

The step sequence is built from all valid degrees in `[minDegree, maxDegree]`, with the "no trait" state at position 0 on the number line.

Example for `NaturalMood`, `minDegree: -2`, `maxDegree: 2`:

```
Step 0: degree -2 (Depressive)
Step 1: degree -1 (Pessimist)
Step 2: none      (no NaturalMood trait)
Step 3: degree 1  (Optimist)
Step 4: degree 2  (Sanguine)
```

Movement is always exactly one step per trip. A pawn at Pessimist who rolls "higher" moves to none — the Pessimist trait is removed. A pawn at none who rolls "higher" moves to Optimist — the trait is added at degree 1.

### Notification Messages

Custom messages support these tokens:

| Token | Substitution |
|-------|-------------|
| `{PAWN}` | Pawn's short name |
| `{TRAIT}` | Display label of the trait gained or lost |
| `{DRUG}` | Label of the drug |
| `{PAWN_pronoun}` | Pawn's subject pronoun (he/she/they) |

If custom messages are omitted in XML, the system generates defaults:
- *"{Pawn} developed the {trait} trait after taking {drug}."*
- *"{Pawn} is no longer {trait} after taking {drug}."*
- *"{Pawn} shifted from {old trait} to {new trait} after taking {drug}."*
