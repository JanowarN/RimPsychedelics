# RimPsychedelics — Expansion & Integration

## Purpose

This document covers systems that extend the core mod: Ideology integration (separate dependent mod), VSIE soft dependency, and art/tale integration. Each section is a self-contained specification for its integration point.

**For core systems these build on**: see Core Systems.
**For C# class structure**: see C# Architecture.
**For per-drug parameter values**: see Drug Profiles.

---

## Table of Contents

1. [Ideology Integration](#1-ideology-integration)
2. [VSIE Integration](#2-vsie-integration)
3. [Art Integration (Tales)](#3-art-integration-tales)

---

## 1. Ideology Integration

### Mod Structure

| | |
|-|-|
| **Mod Name** | RimPsychedelics - Ideology |
| **Hard Dependencies** | RimPsychedelics (base), Ideology DLC |
| **Soft Dependencies** | VSIE (for enhanced ritual social outcomes) |
| **Assembly** | `RimPsychedelics_Ideology.dll` |
| **Dependency Direction** | Ideology mod → base mod (one-way, never reverse) |

### Folder Structure

```
RimPsychedelics_Ideology/
├── About/
│   ├── About.xml
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

### 1.1 Precepts

| Precept | Type | Effect |
|---------|------|--------|
| Psychedelic Use: Approved | Drug use | No guilt/mood penalty. Social approval from believers. |
| Psychedelic Use: Disapproved | Drug use | −6 mood on use. Social disapproval. |
| Psychedelic Use: Prohibited | Drug use | −12 mood on use. Major social penalty. |
| Psychedelic Use: Essential | Drug use | +4 mood on use. −4 mood if no trip in >15 days. |
| Psychonaut: Exalted | Special | Psychonaut pawns get +opinion from believers. Role bonus. |

#### Essential Precept Mechanics

The Essential precept needs to detect whether a pawn has tripped recently. This uses the `RP_RecentTrip` memory thought defined in the base mod:

- `RP_RecentTrip` is an invisible, zero-mood, 15-day-duration thought applied at every trip resolution.
- The Essential precept comp checks for the **absence** of this thought.
- If absent (>15 days since last qualifying trip): apply −4 mood debuff.
- If present: no debuff (the +4 mood on use is a separate effect).

The precept comp (`PreceptComp_PsychedelicUse`) lives in the Ideology assembly and reads `RP_ThoughtDefOf.RP_RecentTrip` from the base mod assembly.

---

### 1.2 Rituals

Three rituals, each with distinct character and mechanical implications.

#### Vision Quest

**Theme**: Solitary, introspective, ceremonial. The seeking pawn goes on a guided solo journey.

| | |
|-|-|
| **Participants** | 1 seeker (ingests) + optional leader + spectators |
| **Duration** | ~3–5 hours |
| **Tolerance Failure** | Yes — seeker above 50% tolerance fails the ritual |
| **Come-Up Modifier** | ~2.5x rate multiplier |
| **Valence Bonus** | Yes — ritual bonus applied |

**Phases**:
1. Gathering — seeker and leader at ritual spot.
2. Ingestion — seeker takes the drug. Come-up hediff receives `comeUpRateMultiplier` (~2.5).
3. Wandering — seeker wanders outdoors solo. Leader departs.
4. Sharing — seeker returns to ritual spot for closing phase.

#### Group Ritual

**Theme**: Communal, connective. Multiple pawns trip together in a ceremonial context.

| | |
|-|-|
| **Participants** | Multiple participants (ingest) + optional leader (does NOT ingest) + spectators |
| **Duration** | ~3–4 hours |
| **Tolerance Failure** | Yes — ANY ingesting participant above 50% tolerance fails the entire ritual |
| **Come-Up Modifier** | ~2.5x rate multiplier for all participants |
| **Valence Bonus** | Yes — ritual bonus applied to all participants |

**Tolerance failure**: If any single ingesting participant has tolerance above 50%, the entire ritual immediately fails with a negative outcome. All participants and spectators receive a mood debuff. No trips are applied. Drug doses are consumed.

**Failure message**: "[Pawn] had dosed too recently to join the experience, and it made everyone uncomfortable."

**Leader exception**: The leader does not ingest and is therefore exempt from the tolerance check.

#### Psychedelic Party

**Theme**: Casual, social. A party where drugs happen to be involved.

| | |
|-|-|
| **Participants** | Multiple attendees + spectators |
| **Duration** | ~1–2 hours |
| **Tolerance Failure** | None |
| **Come-Up Modifier** | None (1.0x, standard rate) |
| **Valence Bonus** | None |

No ritual-specific mechanics. Standard party buff system. Pawns who are tolerant and take a dose simply waste it per the normal tolerance rules — no ritual failure.

The Psychedelic Party exists to give Ideology users a casual social gathering option without the ceremonial stakes of Vision Quest or Group Ritual.

---

### 1.3 Ritual-Modified Trip Mechanics

Vision Quest and Group Ritual modify the trip in two ways:

**Come-up acceleration**: The come-up hediff's `comeUpRateMultiplier` is set to ~2.5 during the ingestion phase. This compresses the onset to fit within a reasonable ritual duration. The IngestionOutcomeDoer checks for ritual context and sets the multiplier accordingly.

| Drug | Normal Come-Up | Ritual Come-Up (~2.5x) |
|------|---------------|----------------------|
| LYS | ~3.5 hours | ~85 minutes |
| Mindcap | ~1 hour | ~25 minutes |
| Fluff | ~1 hour | ~25 minutes |

**Valence bonus**: The drug's `ritualGoodTripChanceBonus` is added as a flat bonus to the sigmoid curve result after evaluation. This is applied before the random roll and clamped to [0, 1]. The bonus is per-drug — see Drug Profiles for values.

Both modifications are passed through the normal ingestion flow. The ritual behavior worker signals ritual context to the IngestionOutcomeDoer, which applies both the multiplier and the bonus. No special hediff classes or comps are needed for ritual trips.

---

### 1.4 Ritual Tolerance Failure

Tolerance is NOT a precondition for starting a ritual — the ritual proceeds regardless. The failure check happens at the ingestion phase.

**Applies to**: Vision Quest, Group Ritual.
**Does NOT apply to**: Psychedelic Party.

**Check**: Any ingesting participant with tolerance severity above 50% at the ingestion phase triggers failure.

**On failure**:
1. Ritual immediately ends with negative outcome.
2. All participants and spectators receive mood debuff.
3. No trips are applied to any participant.
4. Drug doses are consumed (wasted).

**Design rationale**: Making tolerance a precondition would require the player to track cooldowns before scheduling rituals. By allowing the ritual to start and failing at ingestion, the consequence is clear and memorable — players learn to manage tolerance timing naturally.

---

## 2. VSIE Integration

### Overview

Vanilla Social Interactions Expanded adds a richer social memory system. RP hooks into this when available to add trip-related social memories.

| | |
|-|-|
| **Package ID** | `vanillaexpanded.vsie` |
| **Dependency Type** | Soft (runtime check) |
| **Code Location** | `Compat/VSIE_Compat.cs` (base mod) |
| **Failure Mode** | All VSIE features silently skipped if not installed |

### Runtime Detection

```csharp
ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded")
```

All VSIE-dependent code paths are guarded by this check. Types referencing VSIE types use `[MayRequireMod]` attributes to prevent hard reference failures.

---

### 2.1 Base Mod — VSIE Memories

These fire in the base mod when VSIE is installed.

| Memory | Trigger | Valence | Notes |
|--------|---------|---------|-------|
| Shared a trip | Two pawns both have active trip hediffs, nearby | Per-pawn, valence-dependent | Asymmetric outcomes possible (one good, one bad). Also records `RP_SharedTrip` tale. |
| Talked about a good trip | Initiator has recent good trip thought | Mild positive for listener | Spreads the afterglow socially. |
| Talked about a bad trip | Initiator has recent bad trip thought | Mild negative for listener, small positive for initiator | Processing the experience. Telling the story helps. |
| Bonded on Fluff | Both pawns on Fluff, nearby | Strong positive for both | Fluff's empathogenic nature. Drug-specific. |

**Shared trip asymmetry**: Two pawns tripping together may have different valences (one rolled good, one rolled bad). Each pawn's memory reflects their own experience, not their partner's. A pawn on a good trip who shares it with a pawn on a bad trip gets a positive memory; the bad-tripping pawn gets a negative one.

---

### 2.2 Ideology Mod — VSIE Memories

These fire in the Ideology expansion when VSIE is installed.

| Memory | Trigger | Valence | Notes |
|--------|---------|---------|-------|
| Group ritual together | Both participated in Group Ritual | Always positive | Ceremonial bonding, regardless of individual trip valence. |
| Psychedelic party together | Both attended Psychedelic Party | Per-pawn, valence-dependent | Casual context — no ceremonial override. |

---

### 2.3 Inspiration Bridge

When granting inspiration at resolution:
- VSIE installed → use VSIE's inspiration system.
- VSIE not installed → vanilla `InspirationHandler.TryStartInspiration()`.

The bridge is in `VSIE_Compat`. The resolution comp (`HediffComp_TripResolution`) calls through the bridge rather than directly invoking either system.

---

## 3. Art Integration (Tales)

### Overview

RP uses vanilla's Tale system to generate art descriptions. Tales are recorded at specific moments (resolution, trait changes, shared trips) and picked up by the art generator when pawns create sculptures, engravings, etc.

No custom art generation code — tales are defined in XML, recorded via `TaleRecorder.RecordTale()` in C#, and consumed by vanilla's art description system.

---

### 3.1 TaleDefs

| TaleDef | taleClass | Type | baseInterest | Trigger |
|---------|-----------|------|-------------|---------|
| `RP_HadGoodTrip` | `Tale_SinglePawnAndDef` | Volatile | 15 | Good trip resolution |
| `RP_HadBadTrip` | `Tale_SinglePawnAndDef` | Volatile | 15 | Bad trip resolution |
| `RP_GainedPsychonaut` | `Tale_SinglePawn` | PermanentHistorical | 25 | Psychonaut trait granted |
| `RP_TraitChangedByTrip` | `Tale_SinglePawnAndDef` | Volatile | 20 | Trait modification at resolution |
| `RP_SharedTrip` | `Tale_DoublePawn` | Volatile | 12 | Two pawns with active trip hediffs nearby (VSIE) |

### 3.2 Tale Classes and Parameters

**`Tale_SinglePawnAndDef`** — stores one pawn + one ThingDef. Used for trip tales and trait change tales. The ThingDef is the drug, allowing art grammar to reference `[def_label]` for the drug name.

**`Tale_SinglePawn`** — stores one pawn. Used for Psychonaut acquisition — the event is about the pawn, not a specific drug.

**`Tale_DoublePawn`** — stores two pawns. Used for shared trips — the art describes the relationship between the two trippers.

### 3.3 Tale Type Implications

**Volatile** tales decay over time and are eventually forgotten. Trip tales, trait changes, and shared trips use this type — they're memorable events but not permanent historical records.

**PermanentHistorical** tales persist indefinitely. Only `RP_GainedPsychonaut` uses this — becoming a Psychonaut is a defining life event that should show up in art forever.

### 3.4 Interest Values

`baseInterest` controls how likely a tale is to be selected for art descriptions relative to other tales in the pool. Higher = more likely to appear.

| Value | Rationale |
|-------|-----------|
| 12 | Shared trip — common event, lower priority |
| 15 | Good/bad trip — standard significance |
| 20 | Trait change — memorable, personality-altering |
| 25 | Psychonaut acquisition — major life event |

### 3.5 Art Grammar

Each TaleDef includes a `rulePack` with `rulesStrings` that the art generator uses to build descriptions. The grammar covers:

| Rule | Purpose |
|------|---------|
| `tale_noun` | Title fragment ("the purple vision", "the mushroom journey") |
| `image` | Scene description ("a figure dissolving into light") |
| `desc_sentence` | Narrative detail ("colors bled from the walls") |
| `circumstance_phrase` | Atmospheric color ("under a sky that breathed") |

Full grammar text for all TaleDefs is Phase 9 work — polish and writing, no code changes. Grammar is pure XML in `Defs/Core/Tales_Psychedelics.xml`.

### 3.6 Recording Points

Tales are recorded at these moments in C#:

| Tale | Recording Location | Parameters |
|------|-------------------|------------|
| `RP_HadGoodTrip` | `HediffComp_TripResolution.CompPostPostRemoved()` | pawn, drug ThingDef |
| `RP_HadBadTrip` | `HediffComp_TripResolution.CompPostPostRemoved()` | pawn, drug ThingDef |
| `RP_GainedPsychonaut` | `HediffComp_TripResolution.CompPostPostRemoved()` (Psychonaut branch) | pawn |
| `RP_TraitChangedByTrip` | `HediffComp_TripResolution.CompPostPostRemoved()` (trait branch) | pawn, drug ThingDef |
| `RP_SharedTrip` | VSIE social interaction handler | pawn1, pawn2 |

All tales except `RP_SharedTrip` are recorded in the same method — the resolution comp. `RP_SharedTrip` is recorded by VSIE compatibility code and only fires when VSIE is installed.

### 3.7 C# Access

Tales are referenced via `RP_TaleDefOf` (`[DefOf]` class in the base mod):

```csharp
TaleRecorder.RecordTale(RP_TaleDefOf.RP_HadGoodTrip, pawn, drugDef);
```

The `[DefOf]` class handles resolution at startup. No string-based def lookups at runtime.

---

## 4. Planned Integrations (No Plan Yet)

The following mod integrations are intended but have no design work done. Listed here to track scope.

| Mod | Package ID | Integration Type | Notes |
|-----|-----------|-----------------|-------|
| Vanilla Ideology Expanded - Memes and Structures | TBD | Soft dependency | Extended precept/meme support. Likely XPath patches or additional precept defs. Scope TBD. |
| AlphaBiomes | TBD | Soft dependency | Sassafras tree spawning in AlphaBiomes biomes, and potentially Mindcap mushroom placement. Likely XPath biome patches only. |
| RimJobWorld | TBD | Soft dependency | Integration scope TBD. |

These will be specified in this document as plans are developed.
