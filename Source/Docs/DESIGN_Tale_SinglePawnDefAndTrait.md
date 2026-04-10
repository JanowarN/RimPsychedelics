# Tale_SinglePawnDefAndTrait — Design Specification

## Purpose

Custom tale class that stores a pawn, a ThingDef (the drug), and a TraitDef (the trait that changed). Used by the four trait modification tales to enable art grammar that references both the drug and the specific trait involved.

This class does not exist in vanilla. It is new C# work for the base mod.

## Location

`Source/RimPsychedelics/Core/Tale_SinglePawnDefAndTrait.cs`

Also add to `RP_TaleDefOf.cs` for static def references.

## Vanilla Precedent

The closest vanilla class is `Tale_SinglePawnAndDef`, which stores one `TaleData_Pawn` and one `TaleData_Def`. Our class extends the same pattern with a third data field for the trait.

Vanilla tale data classes used as reference:
- `TaleData_Pawn` — serializes pawn info (name, gender, faction, kind, etc.)
- `TaleData_Def` — serializes a single Def reference (stored as defName string)

## Class Structure

```csharp
namespace RimPsychedelics
{
    public class Tale_SinglePawnDefAndTrait : Tale
    {
        public TaleData_Pawn pawnData;
        public TaleData_Def defData;      // drug ThingDef
        public TaleData_Def traitData;    // TraitDef (stored as TaleData_Def)

        public Tale_SinglePawnDefAndTrait() { }

        public Tale_SinglePawnDefAndTrait(
            Pawn pawn,
            ThingDef drug,
            TraitDef trait)
        {
            pawnData = TaleData_Pawn.GenerateFrom(pawn);
            defData = TaleData_Def.GenerateFrom(drug);
            traitData = TaleData_Def.GenerateFrom(trait);
        }
    }
}
```

## Key Implementation Notes

### TaleData_Def for TraitDef

`TaleData_Def` stores any `Def` by its defName and resolves it on load. Since `TraitDef` extends `Def`, `TaleData_Def.GenerateFrom(trait)` should work — it calls `def.defName` and `def.label` which TraitDef has. Verify this in decompiled source. If `TaleData_Def.GenerateFrom` is restricted to `ThingDef`, a custom `TaleData_Trait` class will be needed instead, following the same pattern.

**If custom TaleData_Trait is needed:**

```csharp
public class TaleData_Trait : TaleData
{
    public TraitDef traitDef;

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref traitDef, "traitDef");
    }

    public override void GenerateTestData()
    {
        traitDef = DefDatabase<TraitDef>.GetRandom();
    }

    public static TaleData_Trait GenerateFrom(TraitDef def)
    {
        TaleData_Trait data = new TaleData_Trait();
        data.traitDef = def;
        return data;
    }
}
```

### ExposeData

```csharp
public override void ExposeData()
{
    base.ExposeData();
    Scribe_Deep.Look(ref pawnData, "pawnData");
    Scribe_Deep.Look(ref defData, "defData");
    Scribe_Deep.Look(ref traitData, "traitData");
}
```

Follows the same pattern as `Tale_SinglePawnAndDef`. The `Scribe_Deep.Look` calls delegate serialization to each TaleData subclass's own `ExposeData`.

### GenerateTestData

Required by vanilla's tale testing infrastructure. Creates dummy data for debug tools.

```csharp
public override void GenerateTestData()
{
    base.GenerateTestData();
    pawnData = TaleData_Pawn.GenerateRandom();
    defData = TaleData_Def.GenerateFrom(
        DefDatabase<ThingDef>.AllDefsListForReading.RandomElement());
    traitData = TaleData_Def.GenerateFrom(
        DefDatabase<TraitDef>.AllDefsListForReading.RandomElement());
}
```

### Produce — Grammar Tokens

The `Produce` method (or `GetRulePack` in some versions) injects grammar rules that the art description system can reference in `rulesStrings`. This is where `[def_label]` and `[trait_label]` become available.

```csharp
public override bool Concerns(Thing th)
{
    return base.Concerns(th) 
        || pawnData.pawn == th;
}

public override void Produce(RulePackDef rulePack, 
    List<Rule> outRules, 
    Dictionary<string, string> outConstants)
{
    base.Produce(rulePack, outRules, outConstants);
    pawnData.TaleDataProduction(outRules, outConstants, "PAWN");
    defData.TaleDataProduction(outRules, outConstants, "def");
    traitData.TaleDataProduction(outRules, outConstants, "trait");
}
```

**Important:** The third argument to `TaleDataProduction` is the prefix used in grammar tokens. Setting it to `"trait"` means the XML grammar can reference:
- `[trait_label]` — the trait's display name (e.g., "kind", "pessimist")
- `[trait_definite]` — "the kind" (if available)
- Standard `[def_label]` still references the drug ThingDef
- Standard `[PAWN_nameDef]`, `[PAWN_possessive]`, etc. reference the pawn

**Verify in decompiled source:** Check exactly what methods `TaleData_Def` exposes for grammar production and what prefix conventions vanilla uses. The prefix determines the token names available in XML rulesStrings.

## XML Reference

The four TaleDefs that use this class:

```xml
<taleClass>RimPsychedelics.Tale_SinglePawnDefAndTrait</taleClass>
```

| TaleDef | Type | baseInterest | expireDays | Action |
|---------|------|-------------|------------|--------|
| `RP_TraitGainedByTrip` | Volatile | 25 | 180 | Standalone trait added |
| `RP_TraitLostByTrip` | Volatile | 25 | 180 | Standalone trait removed |
| `RP_TraitIntensifiedByTrip` | Volatile | 25 | 180 | Spectrum moved toward extreme |
| `RP_TraitSoftenedByTrip` | Volatile | 25 | 180 | Spectrum moved toward center |

## Recording Site

All four tales are recorded in `HediffComp_TripResolution.CompPostPostRemoved()`, inside the trait modification branch, after `TraitLogic.BuildAndSelectModification` returns a result and the modification is applied.

```csharp
// After trait modification is applied:
TaleDef taleDef = modification.actionType switch
{
    TraitActionType.Added      => RP_TaleDefOf.RP_TraitGainedByTrip,
    TraitActionType.Removed    => RP_TaleDefOf.RP_TraitLostByTrip,
    TraitActionType.Intensified => RP_TaleDefOf.RP_TraitIntensifiedByTrip,
    TraitActionType.Softened   => RP_TaleDefOf.RP_TraitSoftenedByTrip,
    _ => null
};

if (taleDef != null)
{
    TaleRecorder.RecordTale(taleDef, pawn, drugDef, modification.traitDef);
}
```

**Note:** `TaleRecorder.RecordTale` uses `params object[] args`. The tale class constructor must accept matching parameters. Vanilla resolves tale constructor arguments by type-matching the params array. Verify that passing (Pawn, ThingDef, TraitDef) correctly maps to the three-arg constructor.

## TraitActionType Enum

New enum needed to communicate what kind of modification happened from `TraitLogic` back to the resolution comp:

```csharp
public enum TraitActionType
{
    Added,
    Removed,
    Intensified,
    Softened
}
```

This should be returned as part of the `TraitModification` struct that `TraitLogic.BuildAndSelectModification` already produces. Add the field if not already present.

## RP_TaleDefOf Updates

Add all 8 base mod tale references:

```csharp
[DefOf]
public static class RP_TaleDefOf
{
    public static TaleDef RP_FirstTrip;
    public static TaleDef RP_HadGoodTrip;
    public static TaleDef RP_HadBadTrip;
    public static TaleDef RP_GainedPsychonaut;
    public static TaleDef RP_TraitGainedByTrip;
    public static TaleDef RP_TraitLostByTrip;
    public static TaleDef RP_TraitIntensifiedByTrip;
    public static TaleDef RP_TraitSoftenedByTrip;
}
```

## Dependencies

This class has no external dependencies. It uses only vanilla RimWorld/Verse types:
- `Tale`, `TaleData_Pawn`, `TaleData_Def` (Verse)
- `TaleRecorder`, `RulePackDef`, `Rule` (Verse)
- `TraitDef` (RimWorld)

## Open Questions for Implementation

1. **TaleData_Def + TraitDef compatibility** — Does `TaleData_Def.GenerateFrom()` accept any `Def` subclass, or only `ThingDef`? If restricted, implement `TaleData_Trait` as described above.

2. **Grammar token prefix** — Verify what prefix string produces `[trait_label]` vs `[trait_definite]` in vanilla's grammar system. The prefix `"trait"` is assumed but needs decompiled source confirmation.

3. **TaleRecorder argument resolution** — Verify that `TaleRecorder.RecordTale(taleDef, pawn, thingDef, traitDef)` correctly routes the three data arguments to the custom constructor. Vanilla uses reflection to match args to constructor parameters.

4. **Trait degree label** — For spectrum traits, the trait label at a specific degree (e.g., "pessimist" vs "depressive") is the more useful art token than the base TraitDef label ("NaturalMood"). Consider whether to store the degree-specific label string alongside the TraitDef, or resolve it at grammar production time using `traitDef.DataAtDegree(degree).label`.
