# RimPsychedelics — Overview & Design Decisions

## Purpose

This is the starting document for the RimPsychedelics project. It describes what the mod is, what the drugs are thematically, what has been decided, what hasn't, and the build plan. Read this first.

**For how the systems work**: see Core Systems.
**For how the code is organized**: see C# Architecture.
**For how the XML is structured**: see XML Architecture.
**For per-drug numbers and parameters**: see Drug Profiles.
**For Ideology, VSIE, and art integration**: see Expansion & Integration.

---

## 1. Mod Identity

| | |
|-|-|
| **Mod Name** | RimPsychedelics (RP) |
| **Namespace Prefix** | `RP_` |
| **Target Game Version** | 1.6 |
| **Required DLCs** | None |
| **Soft Dependencies** | Vanilla Social Interactions Expanded (VSIE) |
| **Expansion Mods** | RimPsychedelics - Ideology (requires RP + Ideology DLC) |
| **Future Modules** | RimPsychedelics - DrugCategory (optional, requires RP) |

---

## 2. What the Mod Does

RimPsychedelics adds a class of drugs that behave like psychedelics. Unlike vanilla drugs, RP drugs:

- Are not addictive.
- Build heavy tolerance that must fully clear before the next meaningful dose.
- Produce a multi-phase trip: onset → peak → aftereffects lasting hours to days.
- Can permanently modify a pawn's personality traits.
- Can grant inspirations.
- Determine trip quality (good or bad) based on the pawn's mood at ingestion.
- Reward repeated use with the Psychonaut trait, which improves future trip outcomes.

The mod includes three drugs with distinct thematic identities, a custom ingestion system, a chained hediff architecture, and a trait modification framework. The entire drug experience is configurable per-drug through XML, requiring no C# for new drug creation.

---

## 3. The Three Drugs

### LYS (LSD Analog)

The premium psychedelic. Long-acting, high-commitment, high-reward. Requires industrial research and neutroamine (trade-gated). Produces the longest trip (~20 hours of impairment) with the most powerful aftereffects (7-day afterglow or 3-day disturbed state). Heavy capacity penalties during peak — pawns are effectively incapacitated. Strongly mood-dependent: miserable pawns face real bad trip risk, happy pawns are rewarded.

**Production**: Grow ergo fungus → harvest ergot alkaloid → combine with neutroamine at drug lab → LYS tabs.

**Niche**: Late-game commitment drug for high-mood pawns. The neutroamine bottleneck makes it a luxury. The long afterglow is the primary incentive over Mindcap.

### Dried Mindcap (Psilocybin Analog)

The starter psychedelic. Accessible, natural, volatile. Shortest trip (~8 hours), easy to grow, no advanced research needed. Same capacity penalties as LYS during peak but resolves faster. Shorter afterglow (3 days vs 7) — the upgrade incentive to LYS. Unique among the three drugs in that bad trips can also grant inspiration.

**Production**: Grow mindcap mushrooms → harvest → dry at campfire or drug lab.

**Niche**: Early-game psychedelic. Available before industrial tech. Lower investment, lower reward. The drug you use while building toward LYS.

### Fluff (MDMA Analog)

The social drug. Warm, empathogenic, connective. Very different from LYS and Mindcap: low capacity penalties during peak (pawns remain functional), massive mood effects, and a universal hangover regardless of trip valence. The safest drug — good trip odds are high even at moderate mood. Does not modify traits.

**Production**: Chop sassafras trees → extract safrole from stumps → process at drug lab → Fluff.

**Niche**: Social lubricant. Use when you want the mood boost without the day-long incapacitation. The hangover is the price, and you pay it every time.

---

## 4. Key Concepts

### The Hediff Chain

Every trip follows the same structural pattern: a come-up hediff with rising severity transitions into a peak hediff with decaying severity, which on removal fires a resolution that applies a post-trip hediff. All decisions (good/bad, intensity, eligibility for special effects) are made at ingestion and carried through the chain.

### Tolerance as Cooldown

Tolerance isn't a gradual resistance curve — it's a hard gate. Each dose sets tolerance to maximum. Until it decays below the drug's threshold, additional doses are completely wasted. This creates a natural cooldown period per drug (3–10 days depending on the drug). Zero tolerance is required for the most meaningful effects (trait changes, inspirations).

### Mood-Dependent Valence

Trip quality is determined by a sigmoid curve evaluated against pawn mood at ingestion. The curve's shape is per-drug: some drugs are forgiving (Fluff), others punish low mood (LYS). The Psychonaut trait provides a more favorable curve.

### Psychonaut Progression

Repeated qualifying trips accumulate hidden experience. After enough trips, the pawn gains the Psychonaut trait permanently. This changes which valence curve is used (better odds) and reduces the chance of trait modification (personality has stabilized). Any drug with `psychonautExperienceGain` > 0 contributes experience; only drugs with `countsPsychonautExperience: true` trigger the acquisition roll.

### XML-Driven Drug Design

The entire drug experience — timing, capacity penalties, mood effects, valence odds, trait pools, inspiration chances — is defined in XML via the `PsychedelicDrugExtension` on the drug's ThingDef. The C# framework reads the extension and handles all behavior. New drugs can be created without writing code.

---

## 5. Confirmed Design Decisions

This is the canonical record of resolved design questions. Entries are numbered for stable cross-referencing.

1. **No addiction.** None of the three base drugs have addiction mechanics. The framework supports addictive RP drugs but this is untested — carefully consider tolerance cooldown interactions.

2. **Tolerance as hard gate.** All three drugs build to 100% per dose. Trip cutoff threshold is per-drug via `noTripToleranceThreshold`. Zero tolerance required for inspiration and trait modification.

3. **Tolerance cooldowns.** LYS and Mindcap: ~10 days to zero, ~5 days below 50%. Fluff: ~3 days to zero, ~1.5 days below 50%.

4. **Processing locations.** Vanilla drug lab for LYS and Fluff. Drug lab or campfire/crafting spot for Mindcap (simple drying).

5. **No trip visuals.** No screen overlays or shaders. Effects communicated via hediffs, mood, and thoughts.

6. **Single mod setting.** Enable/disable trait changes (default: on). All other tuning in XML.

7. **Body size ignored.** Custom IngestionOutcomeDoer bypasses vanilla body-size-based drug calculations entirely.

8. **Drug categories.** LYS and Mindcap = Hard. Fluff = Social.

9. **Binary valence.** Trips are Good or Bad. No neutral outcome.

10. **Sequential hediff chain.** Come-up (rising) → Peak (decaying) → Resolution → Post-trip (decaying over days). The `PsychedelicDrugExtension` is the single source of truth for the full hediff chain.

11. **Hediff counts.** 5 hediff defs per drug for LYS/Mindcap. 4 for Fluff (shared recovery hediff for both valences).

12. **Equal impairment duration.** Good and bad trips have identical peak duration and capacity penalties per drug. Valence affects mood, social effects, and resolution outcomes — not physical impairment.

13. **Fluff is different.** Low impairment during peak, high mood effects, universal hangover, no trait modification, safest valence curve.

14. **Valence roll at ingestion.** Determined by sigmoid curve before come-up hediff is applied. Based on mood, Psychonaut status, and ritual context only.

15. **No cross-tolerance.** Each drug has independent tolerance. Different drugs on consecutive days at full power.

16. **Sassafras via vanilla systems.** Implemented using vanilla `choppedThingDef` + `StumpBase`. No custom C# for the tree/stump pipeline.

17. **Ideology as separate mod.** Requires base RP + Ideology DLC. One-way assembly dependency.

18. **Independent resolution systems.** Psychonaut progression, inspiration, and trait modification all evaluate independently at resolution. A single trip can trigger all three.

19. **Psychonaut progression at resolution.** Fires when peak hediff ends, not at ingestion. Cross-drug accumulation via shared `RP_PsychedelicExperience` hediff.

20. **Trait modification types.** Spectrums (degree movement) and standalones (add/remove) with bias-driven direction and weighted random selection from pre-filtered pool.

21. **Sensible defaults for trait entries.** All fields except `traitDef` (and degree range for spectrums) have defaults. Auto-generated notification messages when custom messages not provided.

22. **No trip tracker GameComponent.** Art uses vanilla TaleDefs recorded at resolution. Ideology Essential precept uses an invisible 15-day `RP_RecentTrip` memory thought.

23. **Pawn generation safety.** All ChemicalDefs set `onGeneratedAddictedToleranceChance` to `0.0`. Dummy addiction hediffs use `hediffClass` directly, not `ParentName="AddictionBase"`.

24. **Domain-organized folder structure.** Defs organized by domain (Drugs/, Core/, Plants/, Production/) not by def type. Each drug's complete definition in a single file.

25. **Extension as single source of truth.** Resolution hediff mappings are NOT duplicated on comp properties. `HediffCompProperties_TripResolution` contains only `drugDef` (back-reference). Valence is passed explicitly through the chain to support shared peak HediffDefs.

26. **Tiered resolution safety check.** Tier 1 (null/dead): abort everything. Tier 2 (alive, despawned): post-trip hediff, Psychonaut progression, trait modification, and RP_RecentTrip thought fire normally; inspiration and tale recording skipped. Tier 3 (alive, spawned): everything fires. Come-up → peak transition also checks for null/dead before applying the peak hediff.

27. **No custom mental states.** Trips do not impose mental states. Pawns remain draftable and can respond to emergencies while tripping. Effects communicated through hediff stages and mood.

28. **Psychonaut curve parameters are optional.** Each of the four Psychonaut valence curve fields falls back to its normal-curve counterpart if omitted. Modders can override all four, some, or none. Omitting all means the Psychonaut trait has no effect on valence for that drug. Ritual bonus also optional (defaults to 0.0).

29. **Psychonaut experience gain is per-drug configurable.** `psychonautExperienceGain` (float) replaces the hardcoded 0.1 increment. The `RP_PsychedelicExperience` hediff cap of 0.7 remains hardcoded — gains are clamped. `countsPsychonautExperience` now controls only whether the acquisition roll fires, independent of experience accumulation. A drug can contribute experience without triggering the roll.

30. **Psychonaut trait: standalone, no stat offsets.** Single degree (0), commonality 0.2 (can appear on generated pawns). No stat offsets — the trait interacts only with RP systems (valence curve selection, trait modification chance, progression exclusion). Pawns who generate with the trait bypass progression entirely.

---

## 6. Build Order

| Phase | Focus | Deliverable |
|-------|-------|-------------|
| **1** | Mindcap pipeline | XML: mushroom plant, harvested mindcap, dried mindcap. Vanilla-style placeholder drug effects. Verify grow → harvest → dry → ingest loop. |
| **2** | LYS pipeline | XML: Ergo plant, ergo resource, LYS drug + recipe (neutroamine). Research project. |
| **3** | Sassafras + Fluff | XML: tree/stump, biome patches. Fluff drug + recipe. |
| **4** | Trip system core | C#: `Hediff_PsychedelicComeUp`, `PsychedelicIngestionOutcomeDoer`, `ValenceCurveEvaluator`, `HediffComp_TripResolution`, `PsychedelicDrugExtension`. Placeholder TaleDefs. `RP_RecentTrip` thought. Replace placeholder effects with sequential hediff trips. |
| **5** | Trait system | C#: Psychonaut progression, `TraitLogic`, vanilla trait manipulation, Harmony patches for notifications. |
| **6** | Tolerance refinement | Wire tolerance cap into come-up hediff, post-trip duration scaling, >50% cutoff. |
| **7** | Ideology mod | Precepts, ritual defs, behavior workers, outcome workers. Ritual tolerance failure. Ritual valence bonus. |
| **8** | VSIE integration | Soft dependency, social memories, inspiration bridge. `RP_SharedTrip` tale. |
| **9** | Polish & balance | Tale grammar writing. Trait weight tuning. Balance pass. Mod settings UI. Steam Workshop prep. |

---

## 7. Open Questions

1. **~~Psychonaut trait definition~~** — Resolved. Standalone trait, no spectrum, no stat offsets. Commonality 0.2 (can appear on generated pawns). Interacts only with RP systems: valence curve selection, trait modification chance, and progression exclusion. See Design Decision #30.
2. **Plant defs** — grow days, fertility requirements, yield for Ergo and Mindcap (sassafras is done).
3. **Animal interactions** — can animals eat Mindcap mushrooms or Ergo?
4. **Inspiration chance values** — per-drug, currently TBD.
5. **Fluff recipe details** — Safrole → Fluff processing, research cost.
6. **Mindcap research** — trivial or none?
7. **Vanilla TraitDef degree verification** — confirm actual defName and degree values for all traits in the modification pools.
8. **~~Fluff Psychonaut contribution~~** — Resolved. Fluff does not contribute experience and does not trigger the acquisition roll. `psychonautExperienceGain: 0`, `countsPsychonautExperience: false`.
9. **~~Resolution safety check scope~~** — Resolved. Tiered approach: despawned pawns get post-trip hediff, Psychonaut progression, trait modification, and recent trip thought. Inspiration and tales require spawned. See Design Decision #26.
10. **~~Psychonaut curve design~~** — Resolved. Separate curve parameters (same approach as current), but all four Psychonaut parameters are optional with per-field fallback to the normal curve. Modders can override all, some, or none. See Design Decision #28.
11. **Mod integration plans** — Vanilla Ideology Expanded (Memes and Structures), AlphaBiomes, and RimJobWorld are intended integrations with no design work yet. See Expansion & Integration Section 4.

---

## 8. Document Map

| Document | Contents | Audience |
|----------|----------|----------|
| **Overview & Design Decisions** (this doc) | What the mod is, thematic drug descriptions, confirmed decisions, build order, open questions | Start here |
| **Core Systems** | Tolerance mechanics, hediff chain lifecycle, valence sigmoid, resolution sequence, Psychonaut progression, inspiration, trait modification algorithms | How the systems work |
| **C# Architecture** | Class structure, per-class responsibilities, data flow, save/load, Harmony patches, soft dependencies, Ideology expansion assembly | How the code is built |
| **XML Architecture** | Def structure, wiring patterns, naming conventions, file organization, single-source-of-truth rationale, validation checklist | How the XML is structured and why |
| **Drug Profiles** | Per-drug reference cards: all numbers, timings, capacity penalties, valence curve parameters, trait pools, production chains | Tuning and balance reference |
| **Expansion & Integration** | Ideology (precepts, rituals, tolerance failure), VSIE (memories, inspiration bridge), Tales/art integration | Systems that depend on the core |
| **Modder Guide** | External-facing: how to create custom RP drugs using the framework | External modders |
