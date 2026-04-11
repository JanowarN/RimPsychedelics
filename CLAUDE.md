# RimPsychedelics

RimWorld 1.6 mod adding psychedelic drugs with a multi-phase trip system, trait modification, and Psychonaut progression. No DLC required. Soft dependency on VSIE.

## Build

```bash
cd Source/RimPsychedelics
dotnet build
```

Output: `1.6/Assemblies/RimPsychedelics.dll`

NuGet packages (compile-time only, not copied to output):
- `Krafs.Rimworld.Ref` 1.6.4633 — RimWorld assembly references
- `Lib.Harmony` 2.3.3

## Project Structure

```
About/              About.xml
Languages/
  English/Keyed/    Translation keys (RP_Keys.xml)
Source/
  Docs/             Design docs (architecture, systems, drug profiles, etc.)
  RimPsychedelics/  C# source (base mod assembly)
    Core/           Enums, data containers, extension, DefOf classes, mod entry, tale class, valence evaluator
    Hediffs/        Come-up hediff, resolution comp + properties
    Ingestion/      PsychedelicIngestionOutcomeDoer
    Traits/         TraitLogic, TraitNotification
    Social/         ThoughtWorker_RPSocialOffset, Thought_SharedTrip, SocialOffsetExtension, TaleFadeExtension
    Compat/         VSIE_Compat
    Harmony/        Patch_SharedTrip
Textures/
  Things/Item/Drug/       Drug stack textures (_a, _b, _c variants)
  Things/Item/Resource/   Precursor resource textures
  Things/Plant/           Plant, tree, stump textures (growth stages, leafless)
1.6/
  Assemblies/       Build output
  Defs/
    Core/           Shared defs (hediffs, traits, thoughts, tales, plants, recipes, research)
    Drugs/          Per-drug defs (fluff.xml, LYS.xml, mindcap.xml)
  Patches/          XPath patches (biomes, Dubs Bad Hygiene compat)
```

A future `Source/RimPsychedelics_Ideology/` project will produce a separate `RimPsychedelics_Ideology.dll` with a one-way dependency on the base assembly.

## Conventions

- **Namespace prefix**: `RP_`
- **DefName prefix**: `RP_`
- **Harmony ID**: `rimpsychedelics.main`
- **XML-driven drug design**: All per-drug config lives in `PsychedelicDrugExtension` on the drug ThingDef. New drugs require zero C#.
- **Single source of truth**: Hediff chain mappings exist only on the extension. `HediffCompProperties_TripResolution` holds only a `drugDef` back-reference. No duplication.
- **Defs organized by domain**, not by def type. Each drug file contains its complete definition (ThingDef, ChemicalDef, tolerance hediff, etc.).
- **Minimal Harmony**: Core trip system uses vanilla extension points (IngestionOutcomeDoer, HediffWithComps, HediffComp). Single Harmony patch for shared trip tale recording.
- **Notification tokens**: Use vanilla-style square brackets — `[PAWN_nameDef]`, `[PAWN_pronoun]`, `[PAWN_possessive]`, `[DRUG_label]`, `[TRAIT_label]`.
- **Sigmoid math in double precision**: ValenceCurveEvaluator does all sigmoid calculation in double, casts to float once at return.

## Design Docs

All in `Source/Docs/`. Read these before making architectural decisions:

- `overview-decisions.md` — Start here. Mod identity, drug descriptions, confirmed design decisions, build order
- `core-systems.md` — Tolerance, hediff chain, valence sigmoid, resolution sequence, Psychonaut progression, trait modification
- `cs-architecture.md` — Class structure, responsibilities, data flow, save/load, Harmony patches
- `xml-architecture.md` — Def structure, wiring patterns, naming conventions, single-source-of-truth rationale
- `drug-profiles-final.md` — Per-drug numbers: timings, capacity penalties, valence curves, trait pools
- `expansion-integration.md` — Ideology, VSIE, tales/art integration
- `architecture-plan-v2.md` — Complete architecture specification
- `DESIGN_Tale_SinglePawnDefAndTrait.md` — Custom tale class design (extends Tale_SinglePawnAndDef)

## Key Design Decisions

- No addiction mechanics. Tolerance acts as a hard cooldown gate (dose sets tolerance to max, must decay fully before next meaningful dose).
- Binary trip valence (Good/Bad) determined at ingestion by mood-dependent sigmoid curve.
- Three drugs: LYS (LSD, late-game luxury), Dried Mindcap (psilocybin, starter), Fluff (MDMA, social, no trait modification).
- Single mod setting: enable/disable trait changes.
- Body size explicitly ignored in all drug calculations.
- Harmony patches from the original design (Patch_TraitNotification, Patch_TraitConflicts) were not needed — notification and conflict logic is handled directly in TraitLogic.
- VSIE shared trip memories gated behind VSIE active + EnableMemories setting. Tale always recorded for art regardless.
- Shared trip tales only fire when both pawns are on the same drug and interact during peak.
- Opinion from shared trips fades over time using TaleFadeExtension (configurable falloffDays and minimumOpinionOffset in XML).

## Mod Integrations

- **VSIE** (soft): Shared trip social memories via Thought_SharedTrip + ThoughtWorker_SharedTrip. Gated behind VSIE active + EnableMemories. Inspiration auto-hooks via VSIE's own postfix on TryStartInspiration.
- **Alpha Biomes** (XPath): Sassafras trees patched into AB_IdyllicMeadows, AB_TarPits, AB_MiasmicMangrove, AB_FeraliskInfestedJungle. MayRequire="sarg.alphabiomes".
- **Dubs Bad Hygiene** (XPath): ThirstRateMultiplier tracks hungerRateFactor, BladderRateMultiplier tracks restFallFactor on all trip hediff stages. Fluff recovery rebounds thirst only, not bladder. MayRequire="Dubwise.DubsBadHygiene".

## Current State

Base mod C# is complete through Phase 6 (tolerance refinement) plus Phase 8 (VSIE integration). Builds clean with 0 warnings.

Remaining:
- Phase 7: Ideology expansion (separate assembly, not started)
- Phase 9: Polish (tale grammar, balance tuning, placeholder descriptions, Steam Workshop prep)
- Runtime verification needed for TaleData_Def.GenerateFrom(TraitDef) and Tale_DoublePawnAndDef constructor arg matching
