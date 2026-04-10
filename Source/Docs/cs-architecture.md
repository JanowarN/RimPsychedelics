# RimPsychedelics — C# Architecture

## Purpose

This document defines the C# class structure, responsibilities, data flow, and implementation patterns for RimPsychedelics. It is the canonical reference for how the code is organized and what each class does.

**This is not a systems design reference.** For how the systems work mechanically (algorithms, decision logic, flows), see Core Systems. For XML def structure and wiring, see XML Architecture. For per-drug values, see Drug Profiles.

---

## Table of Contents

1. [Project Structure](#1-project-structure)
2. [Class Responsibilities](#2-class-responsibilities)
3. [Data Flow](#3-data-flow)
4. [Save/Load (ExposeData)](#4-saveload-exposedata)
5. [Harmony Patches](#5-harmony-patches)
6. [Mod Settings](#6-mod-settings)
7. [Soft Dependencies](#7-soft-dependencies)
8. [Ideology Expansion Assembly](#8-ideology-expansion-assembly)

---

## 1. Project Structure

### Base Mod

```
Source/RimPsychedelics/
├── Core/
│   ├── RimPsychedelicsMod.cs
│   ├── TripValence.cs
│   ├── PsychedelicDrugExtension.cs
│   ├── ValenceCurveEvaluator.cs
│   ├── RP_TaleDefOf.cs
│   ├── RP_ThoughtDefOf.cs
│   ├── TraitSpectrumEntry.cs
│   └── TraitStandaloneEntry.cs
├── Hediffs/
│   ├── Hediff_PsychedelicComeUp.cs
│   ├── HediffCompProperties_TripResolution.cs
│   └── HediffComp_TripResolution.cs
├── Ingestion/
│   └── PsychedelicIngestionOutcomeDoer.cs
├── Social/                               
│   ├── ThoughtWorker_RPSocialOffset.cs   
│   └── SocialOffsetExtension.cs          
├── Traits/
│   ├── TraitLogic.cs
│   └── TraitNotification.cs
├── Harmony/
│   ├── HarmonyPatches.cs
│   ├── Patch_TraitNotification.cs
│   └── Patch_TraitConflicts.cs
└── Compat/
    └── VSIE_Compat.cs
```

### Ideology Expansion

```
Source/RimPsychedelics_Ideology/
├── Rituals/
│   ├── RitualBehaviorWorker_VisionQuest.cs
│   ├── RitualBehaviorWorker_GroupTrip.cs
│   ├── RitualOutcomeEffectWorker_VisionQuest.cs
│   ├── RitualOutcomeEffectWorker_GroupTrip.cs
│   └── RitualOutcomeEffectWorker_PsychedelicParty.cs
└── Precepts/
    └── PreceptComp_PsychedelicUse.cs
```

### Build Output

```
RimPsychedelics/Assemblies/RimPsychedelics.dll
RimPsychedelics_Ideology/Assemblies/RimPsychedelics_Ideology.dll
```

Two separate assemblies. The Ideology expansion references the base mod assembly.

---

## 2. Class Responsibilities

### Core/

#### `RimPsychedelicsMod.cs`

Mod entry point. Responsibilities:
- Harmony patch initialization (`HarmonyInstance.PatchAll()`).
- Mod settings storage and UI (single toggle: enable/disable trait changes).
- Static access point for settings: `RimPsychedelicsMod.Settings.traitChangesEnabled`.

Extends `Mod`. Settings class extends `ModSettings`.

#### `TripValence.cs`

Enum with two values: `Good`, `Bad`. Used throughout the hediff chain to track and pass valence.

```csharp
public enum TripValence
{
    Good,
    Bad
}
```

#### `PsychedelicDrugExtension.cs`

`DefModExtension` attached to drug ThingDefs. Contains all per-drug XML parameters. This class is a data container — it has no behavior, only fields.

**Field groups:**
- Hediff references (5 `HediffDef` fields)
- Tolerance config (`ChemicalDef` + `float` threshold)
- Psychonaut progression (`float` gain + `bool` roll flag)
- Valence curves (9 `float` fields)
- Inspiration settings (2 `bool` + 1 `float`)
- Trait modification scalars (1 `bool` + 2 `float`)
- Trait pools (2 `List<>` fields)

**Access pattern**: Other classes retrieve this via `drugThingDef.GetModExtension<PsychedelicDrugExtension>()`. The extension is the single source of truth for all per-drug configuration — see XML Architecture Section 1 for rationale.

#### `ValenceCurveEvaluator.cs`

Static utility class. Single public method:

```csharp
public static float EvaluateGoodTripChance(
    float mood,
    PsychedelicDrugExtension ext,
    bool isPsychonaut,
    bool isRitual)
```

Selects curve parameters based on `isPsychonaut`. For Psychonaut pawns, each parameter falls back to its normal-curve counterpart if not specified in the extension (null/default). This means partial Psychonaut overrides are supported — a drug can override just the floor and transition point while inheriting the ceiling and plateau from its normal curve.

Evaluates the sigmoid, adds ritual bonus if applicable, clamps to [0, 1]. Returns the probability of a good trip — the caller handles the random roll.

#### `RP_TaleDefOf.cs`

`[DefOf]` class. Static references to custom TaleDefs:
- `RP_HadGoodTrip`
- `RP_HadBadTrip`
- `RP_GainedPsychonaut`
- `RP_TraitChangedByTrip`
- `RP_SharedTrip`

#### `RP_ThoughtDefOf.cs`

`[DefOf]` class. Static references:
- `RP_RecentTrip` — invisible 15-day memory thought used as a flag for Ideology Essential precept.

#### `TraitSpectrumEntry.cs`

Data class for spectrum trait pool entries. Deserialized from XML within `PsychedelicDrugExtension.traitSpectrums`. Fields map directly to the XML schema documented in XML Architecture Section 6.1.

No behavior — this is a data container consumed by `TraitLogic`.

#### `TraitStandaloneEntry.cs`

Data class for standalone trait pool entries. Same pattern as `TraitSpectrumEntry`, deserialized from `PsychedelicDrugExtension.traitStandalones`.

---

### Hediffs/

#### `Hediff_PsychedelicComeUp.cs`

Custom hediff class extending `HediffWithComps`. The only custom hediff class in the mod.

**Runtime fields (set by IngestionOutcomeDoer):**

| Field | Type | Set By | Purpose |
|-------|------|--------|---------|
| `maxSeverityCap` | float | IngestionOutcomeDoer | Tolerance-derived severity ceiling |
| `wasZeroTolerance` | bool | IngestionOutcomeDoer | Gates trait/inspiration at resolution |
| `nextHediffDef` | HediffDef | IngestionOutcomeDoer | Pre-selected peak hediff |
| `tripValence` | TripValence | IngestionOutcomeDoer | Good or Bad, determined at ingestion |
| `comeUpRateMultiplier` | float | IngestionOutcomeDoer | 1.0 normal, ~2.5 ritual |

**Key overrides:**

`PostTick()` or severity management:
- Severity increases at base rate × `comeUpRateMultiplier`.
- Severity clamped to `maxSeverityCap` each tick.
- When severity reaches `maxSeverityCap`: trigger transition.

Transition logic:
1. Safety check: pawn null or dead → abort (come-up removed, no peak applied).
2. Remove self from pawn.
3. Apply `nextHediffDef` at severity = `maxSeverityCap`.
4. Find `HediffComp_TripResolution` on the new hediff.
5. Set comp's `wasZeroTolerance` and `tripValence` from own fields.

`ExposeData()`:
- All five runtime fields saved/loaded via `Scribe_Values` and `Scribe_Defs`.

#### `HediffCompProperties_TripResolution.cs`

Comp properties class extending `HediffCompProperties`.

**Single field:**

```csharp
public ThingDef drugDef;
```

This is the back-reference to the drug ThingDef. At runtime, the comp uses this to look up `PsychedelicDrugExtension`. No other fields — resolution hediff selection comes from the extension, not from comp properties. See XML Architecture Section 1.

#### `HediffComp_TripResolution.cs`

Comp extending `HediffComp`. Attached to peak hediffs. Fires the resolution sequence when the parent hediff is removed.

**Runtime fields (set by come-up transition):**

| Field | Type | Set By | Purpose |
|-------|------|--------|---------|
| `wasZeroTolerance` | bool | Hediff_PsychedelicComeUp | Passed through from ingestion |
| `tripValence` | TripValence | Hediff_PsychedelicComeUp | Determines resolution hediff + trait bias |

**Key override:**

`CompPostPostRemoved()`:
1. Tiered safety check:
   - Pawn null or dead → abort all resolution.
   - Pawn alive but despawned → Tier 2 (skip inspiration and tales).
   - Pawn alive and spawned → Tier 3 (everything fires).
2. Look up `Props.drugDef` → `PsychedelicDrugExtension`.
3. Execute resolution sequence (respecting tier):
   - A. Apply post-trip hediff — Tier 2+.
   - B. Psychonaut progression — Tier 2+.
   - C. Inspiration roll — Tier 3 only.
   - D. Trait modification — Tier 2+.
   - E. Trip recording: tales Tier 3 only; `RP_RecentTrip` thought Tier 2+.

`ExposeData()`:
- Both runtime fields saved/loaded.

**Props access:**

```csharp
public HediffCompProperties_TripResolution Props =>
    (HediffCompProperties_TripResolution)props;
```

---

### Ingestion/

#### `PsychedelicIngestionOutcomeDoer.cs`

Extends `IngestionOutcomeDoer`. Entry point for the entire trip system.

**XML field:**

```csharp
public ChemicalDef toleranceChemical;
```

Used to look up the pawn's current tolerance hediff severity.

**`DoIngestionOutcomeSpecial()` override:**

The full ingestion flow documented in Core Systems Section 2, Phase 1. Key operations:
1. Read tolerance from `toleranceChemical`.
2. Gate check against `noTripToleranceThreshold`.
3. Calculate `maxSeverityCap`.
4. Roll valence via `ValenceCurveEvaluator`.
5. Apply come-up hediff and configure its instance fields.

**Extension lookup:** Reads `PsychedelicDrugExtension` from the parent drug's ThingDef (available via the ingestion context).

---

### Traits/

#### `TraitLogic.cs`

Static utility class. Core trait modification logic:

**Pool construction:**

```csharp
public static TraitModification BuildAndSelectModification(
    Pawn pawn,
    PsychedelicDrugExtension ext,
    TripValence valence)
```

Iterates spectrum and standalone entries, rolls direction/action per-entry using bias, validates against pawn state, builds weighted pool, selects result. Returns a `TraitModification` struct (or null if pool is empty) containing the pre-determined change.

See Core Systems Section 7 for the algorithm.

**Safe trait operations:**

```csharp
public static void SafeAddTrait(Pawn pawn, Trait trait)
public static void SafeRemoveTrait(Pawn pawn, Trait trait)
```

Wrappers around vanilla trait manipulation with conflict checking. These handle edge cases like: pawn already has the trait (skip), trait conflicts with existing trait (skip), and pawn's trait list being in an unexpected state.

#### `TraitNotification.cs`

Static utility class. Token substitution for notification messages:

```csharp
public static string FormatMessage(
    string template,
    Pawn pawn,
    ThingDef drug,
    string traitLabel)
```

Replaces `{PAWN}`, `{DRUG}`, `{TRAIT}`, `{PAWN_pronoun}` tokens.

Also handles default message generation when custom messages are not provided in XML.

---

### Social/

#### `SocialOffsetExtension.cs`

`DefModExtension` attached to ThoughtDefs that use `ThoughtWorker_RPSocialOffset`. Contains configuration that maps a hediff's severity to ThoughtDef stage indices.

**Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `hediffDef` | HediffDef | The hediff to check for on the pawn. |
| `severityThresholds` | List\<float\> | Descending severity values. The pawn's hediff severity is compared top-down; the first threshold exceeded determines the stage index. Below all thresholds = last stage. |

No behavior — this is a data container read by `ThoughtWorker_RPSocialOffset`.

**Design note:** The extension lives on the ThoughtDef, not on the hediff or the drug ThingDef. This is intentional — each ThoughtDef is an independent social thought that happens to read hediff state. Multiple ThoughtDefs can reference the same hediff with different threshold/offset configurations. The extension has no dependency on `PsychedelicDrugExtension` or any other RP type.

#### `ThoughtWorker_RPSocialOffset.cs`

Extends `ThoughtWorker_Social`. A generic, reusable worker that translates hediff severity into social opinion stages. Contains no drug-specific logic.

**Override:**

```csharp
protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
```

**Logic:**
1. Read `SocialOffsetExtension` from `this.def.GetModExtension<SocialOffsetExtension>()`.
2. Check if pawn `p` has the specified hediff → if not, return `ThoughtState.Inactive`.
3. Read hediff severity.
4. Iterate `severityThresholds` top-down. First threshold the severity exceeds → return `ThoughtState.ActiveAtStage(index)`.
5. Below all thresholds → return `ThoughtState.ActiveAtStage(thresholds.Count)` (the final stage).

**What the worker does NOT do:**
- No pawn-pair filtering. The opinion offset applies to all other pawns equally. This is the intended "love everyone" / "resent everyone" behavior for Fluff.
- No caching. RimWorld's `ThoughtWorker_Social` infrastructure handles pawn iteration and caching automatically.
- No reference to `PsychedelicDrugExtension`, `HediffComp_TripResolution`, or any other RP class. The worker is fully decoupled — it only knows about hediffs and severity thresholds.

**Reuse:** Any modder can create new social opinion effects for any hediff by writing a ThoughtDef that points to this worker class and attaches a `SocialOffsetExtension`. Zero C# required.

---

### Harmony/

#### `HarmonyPatches.cs`

Patch registration. Called by `RimPsychedelicsMod` constructor.

```csharp
var harmony = new Harmony("rimpsychedelics.main");
harmony.PatchAll(Assembly.GetExecutingAssembly());
```

Individual patches use `[HarmonyPatch]` attributes — no manual patching.

#### `Patch_TraitNotification.cs`

Sends a `LetterDef` notification to the player when a trait is added or removed by the trip system. The letter includes the formatted message (custom or default) from `TraitNotification`.

#### `Patch_TraitConflicts.cs`

Prevents conflicting trait combinations that could arise from the trait modification system. Ensures vanilla's trait conflict rules are respected during add operations.

---

### Compat/

#### `VSIE_Compat.cs`

Soft dependency bridge for Vanilla Social Interactions Expanded.

**Runtime detection:**

```csharp
public static bool IsActive =>
    ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded");
```

**Responsibilities:**
- Inspiration bridge: route inspiration grants through VSIE's system when available.
- Social interaction memories (shared trips, bonding on Fluff, talking about trips).

**Attribute pattern:**

```csharp
[MayRequireMod("vanillaexpanded.vsie")]
```

All VSIE-dependent types and methods use this attribute to prevent hard references from breaking when VSIE is not installed. VSIE-referencing code must never be reached at runtime if VSIE is not active — guard all call sites with the `IsActive` check.

---

## 3. Data Flow

### Ingestion → Come-Up → Resolution

This is the primary data flow through the hediff chain. See Core Systems Section 2 for the mechanical description. The implementation mirrors it directly:

```
PsychedelicIngestionOutcomeDoer
  │
  │  Creates Hediff_PsychedelicComeUp, sets 5 instance fields
  │
  ▼
Hediff_PsychedelicComeUp
  │
  │  At maxSeverityCap: creates peak hediff,
  │  finds HediffComp_TripResolution on it,
  │  sets 2 runtime fields (wasZeroTolerance, tripValence)
  │
  ▼
HediffComp_TripResolution
  │
  │  At severity 0 (CompPostPostRemoved):
  │  reads Props.drugDef → PsychedelicDrugExtension
  │
  ├──► Post-trip hediff (applied directly)
  ├──► Psychonaut progression (inline or utility)
  ├──► Inspiration (VSIE_Compat or vanilla)
  ├──► Trait modification (TraitLogic)
  └──► Trip recording (RP_TaleDefOf, RP_ThoughtDefOf)
```

### Extension Lookup Pattern

The extension is accessed from three locations:

| Caller | How It Gets the ThingDef | When |
|--------|--------------------------|------|
| `PsychedelicIngestionOutcomeDoer` | Ingestion context (parent drug) | At ingestion |
| `Hediff_PsychedelicComeUp` | Not needed — all data already in instance fields | — |
| `HediffComp_TripResolution` | `Props.drugDef` (from comp properties XML) | At resolution |

The come-up hediff never reads the extension directly. It operates entirely on the instance fields set by the IngestionOutcomeDoer.

### Trait Modification Flow

```
HediffComp_TripResolution.CompPostPostRemoved()
  │
  │  Checks eligibility (wasZeroTolerance, canModifyTraits, settings, roll)
  │
  ▼
TraitLogic.BuildAndSelectModification(pawn, ext, valence)
  │
  │  Iterates ext.traitSpectrums and ext.traitStandalones
  │  Rolls bias per entry, validates, builds pool, selects
  │
  │  Returns: TraitModification (or null)
  │
  ▼
TraitLogic.SafeAddTrait / SafeRemoveTrait
  │
  ▼
TraitNotification.FormatMessage(...)
  │
  ▼
Letter sent to player
```

---

## 4. Save/Load (ExposeData)

Two classes carry runtime state that must survive save/load:

### Hediff_PsychedelicComeUp

```csharp
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Values.Look(ref maxSeverityCap, "maxSeverityCap", 1f);
    Scribe_Values.Look(ref wasZeroTolerance, "wasZeroTolerance", false);
    Scribe_Defs.Look(ref nextHediffDef, "nextHediffDef");
    Scribe_Values.Look(ref tripValence, "tripValence", TripValence.Good);
    Scribe_Values.Look(ref comeUpRateMultiplier, "comeUpRateMultiplier", 1f);
}
```

### HediffComp_TripResolution

```csharp
public override void CompExposeData()
{
    Scribe_Values.Look(ref wasZeroTolerance, "wasZeroTolerance", false);
    Scribe_Values.Look(ref tripValence, "tripValence", TripValence.Good);
}
```

### What is NOT saved

- `PsychedelicDrugExtension` fields — these are XML def data, loaded once at startup. Never saved per-instance.
- `ValenceCurveEvaluator` — stateless utility, no instance data.
- `TraitLogic` — stateless utility.
- `VSIE_Compat` — stateless runtime check.

### Save/Load Resilience

If a save is loaded with a come-up hediff active, all five instance fields are restored. The come-up continues from its saved severity and reaches `maxSeverityCap` normally.

If a save is loaded with a peak hediff active, the comp's two runtime fields are restored. Resolution fires normally when severity decays to 0.

If a save is loaded after the hediff chain has completed (only post-trip hediff remains), no RP-specific state needs restoration — the post-trip hediff is a standard vanilla `HediffWithComps` with no custom fields.

---

## 5. Harmony Patches

### Patch Strategy

RP uses Harmony minimally. The core trip system (ingestion → come-up → peak → resolution) is entirely self-contained and requires no patches — it uses vanilla extension points (IngestionOutcomeDoer, HediffWithComps, HediffComp).

Patches are only used for:
1. **Trait notification letters** — hooking into the trait system to send player notifications when traits change due to trips.
2. **Trait conflict prevention** — ensuring vanilla's conflict rules are checked during RP trait add operations.
3. **Biome patches** — XPath patches (XML, not Harmony) add sassafras trees to vanilla biome plant lists. These live in `Patches/Patches_Biomes.xml` and operate on the loaded def database at startup. No C# patching required.

### Patch Scope

All patches are in the `Harmony/` folder with `[HarmonyPatch]` attributes. No manual patching. The Harmony instance ID is `"rimpsychedelics.main"`.

Patches should be kept narrow and defensive:
- Prefix patches that might skip original methods should be avoided. Postfix and transpiler preferred.
- All patches should check for RP-specific context before modifying behavior — don't alter vanilla behavior for non-RP situations.

---

## 6. Mod Settings

### Structure

```csharp
public class RimPsychedelicsSettings : ModSettings
{
    public bool traitChangesEnabled = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref traitChangesEnabled, "traitChangesEnabled", true);
    }
}
```

### Access

```csharp
RimPsychedelicsMod.Settings.traitChangesEnabled
```

Checked in `HediffComp_TripResolution` before entering the trait modification branch.

### UI

Single checkbox in the mod settings menu. Label and tooltip explain that this controls whether psychedelic trips can permanently modify pawn traits.

---

## 7. Soft Dependencies

### VSIE (Vanilla Social Interactions Expanded)

**Package ID**: `vanillaexpanded.vsie`

**Pattern**: All VSIE-dependent code lives in `Compat/VSIE_Compat.cs`. Call sites check `VSIE_Compat.IsActive` before invoking any VSIE-dependent methods. Types and methods that reference VSIE types use `[MayRequireMod]` attributes.

**Integration points:**
- Inspiration granting (use VSIE system instead of vanilla).
- Social memories (shared trips, Fluff bonding, trip conversations).

**Failure mode**: If VSIE is not installed, all VSIE code paths are skipped. The base mod functions identically without it — vanilla inspiration and no social memories.

---

## 8. Ideology Expansion Assembly

### Mod Identity

- **Assembly**: `RimPsychedelics_Ideology.dll`
- **Dependencies**: `RimPsychedelics.dll` (hard), Ideology DLC (hard)
- **Soft dependencies**: VSIE (for enhanced ritual social outcomes)

### Classes

#### Rituals/

**`RitualBehaviorWorker_VisionQuest.cs`**
- Single-pawn ritual. Seeker ingests, wanders outdoors, returns.
- Sets `comeUpRateMultiplier` (~2.5) on the come-up hediff during ingestion phase.
- Passes ritual context flag for valence bonus.

**`RitualBehaviorWorker_GroupTrip.cs`**
- Multi-pawn ritual. All participants ingest with ritual-modified come-up.
- Leader role (optional) does NOT ingest.
- Tolerance failure check: if any ingesting participant exceeds 50% tolerance, ritual immediately fails.

**`RitualOutcomeEffectWorker_VisionQuest.cs`**
**`RitualOutcomeEffectWorker_GroupTrip.cs`**
- Post-ritual outcome processing. Applies ritual-specific mood/social effects.
- Tolerance failure outcome: all participants/spectators receive mood debuff, no trips applied, doses consumed.

**`RitualOutcomeEffectWorker_PsychedelicParty.cs`**
- Social gathering. No tolerance failure check. No ritual-modified come-up. No ritual valence bonus. Standard party buff mechanics.

#### Precepts/

**`PreceptComp_PsychedelicUse.cs`**
- Handles the Essential precept: checks for absence of `RP_RecentTrip` thought.
- If thought is absent (>15 days since last trip), applies mood debuff.
- Reads `RP_ThoughtDefOf.RP_RecentTrip` from the base mod assembly.

### Cross-Assembly References

The Ideology assembly references base mod types:
- `PsychedelicDrugExtension` (for ritual valence bonus)
- `Hediff_PsychedelicComeUp` (for setting `comeUpRateMultiplier`)
- `TripValence` (enum)
- `RP_ThoughtDefOf` (for Essential precept)
- `VSIE_Compat` (for ritual social memories, if VSIE present)

No base mod code references the Ideology assembly. The dependency is strictly one-way.
