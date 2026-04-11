using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class PsychedelicDrugExtension : DefModExtension
    {
        // --- Hediff references ---
        public HediffDef hediffDefComeUp;
        public HediffDef hediffDefPositiveTrip;
        public HediffDef hediffDefNegativeTrip;
        public HediffDef hediffDefPositiveResolution;
        public HediffDef hediffDefNegativeResolution;

        // --- Tolerance ---
        public ChemicalDef toleranceChemical;
        public float noTripToleranceThreshold = 0.5f;

        // --- Psychonaut progression ---
        public float psychonautExperienceGain = 0f;
        public bool countsPsychonautExperience = false;

        // --- Valence curves: normal pawn (required) ---
        public float goodTripChanceAtZeroMood;
        public float goodTripChanceUpperLimit;
        public float goodTripTransitionPoint;
        public float goodTripPlateauPoint;

        // --- Valence curves: Psychonaut (optional, -1 = fall back to normal) ---
        public float psychonautGoodTripChanceAtZeroMood = -1f;
        public float psychonautGoodTripChanceUpperLimit = -1f;
        public float psychonautGoodTripTransitionPoint = -1f;
        public float psychonautGoodTripPlateauPoint = -1f;

        // --- Ritual bonus (optional) ---
        public float ritualGoodTripChanceBonus = 0f;

        // --- Inspiration ---
        public bool canGoodTripInspire;
        public bool canBadTripInspire;
        public float inspirationChance;

        // --- Trait modification ---
        public bool canModifyTraits;
        public float traitChangeChance;
        public float traitChangeChancePsychonaut;
        public List<TraitSpectrumEntry> traitSpectrums;
        public List<TraitStandaloneEntry> traitStandalones;
    }
}
