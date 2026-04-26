using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class HediffComp_TripResolution : HediffComp
    {
        // Runtime fields — set by Hediff_PsychedelicComeUp at transition
        public bool wasZeroTolerance;
        public TripValence tripValence;
        public float maxSeverityCap = 1f;

        public HediffCompProperties_TripResolution Props =>
            (HediffCompProperties_TripResolution)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            Pawn p = parent.pawn;

            // --- Tier 1: pawn null or dead — abort everything ---
            if (p == null || p.Dead)
                return;

            bool spawned = p.Spawned; // Tier 3 if true, Tier 2 if false

            PsychedelicDrugExtension ext = Props.drugDef?.GetModExtension<PsychedelicDrugExtension>();
            if (ext == null)
                return;

            // --- A. Post-trip hediff (Tier 2+) ---
            ApplyPostTripHediff(p, ext);

            // --- B. Psychonaut progression (Tier 2+) ---
            ProcessPsychonautProgression(p, ext, spawned);

            // --- C. Inspiration (Tier 3 only) ---
            if (spawned)
                TryGrantInspiration(p, ext);

            // --- D. Trait modification (Tier 2+) ---
            TryModifyTraits(p, ext, spawned);

            // --- E. Trip recording (tales Tier 3, thought Tier 2+) ---
            RecordTrip(p, ext, spawned);
        }

        private void ApplyPostTripHediff(Pawn p, PsychedelicDrugExtension ext)
        {
            HediffDef resDef = tripValence == TripValence.Good
                ? ext.hediffDefPositiveResolution
                : ext.hediffDefNegativeResolution;

            if (resDef == null)
                return;

            Hediff resHediff = HediffMaker.MakeHediff(resDef, p);
            resHediff.Severity = maxSeverityCap;
            p.health.AddHediff(resHediff);
        }

        private void ProcessPsychonautProgression(Pawn p, PsychedelicDrugExtension ext, bool spawned)
        {
            if (ext.psychonautExperienceGain <= 0f)
                return;

            // Already a Psychonaut — no progression needed
            TraitDef psychonautDef = DefDatabase<TraitDef>.GetNamedSilentFail("Psychonaut");
            if (psychonautDef == null || p.story?.traits?.HasTrait(psychonautDef) == true)
                return;

            // Accumulate experience
            HediffDef expDef = DefDatabase<HediffDef>.GetNamedSilentFail("RP_PsychedelicExperience");
            if (expDef == null)
                return;

            Hediff expHediff = p.health.hediffSet.GetFirstHediffOfDef(expDef);
            bool isFirstTrip = expHediff == null;

            if (isFirstTrip)
            {
                expHediff = HediffMaker.MakeHediff(expDef, p);
                expHediff.Severity = ext.psychonautExperienceGain;
                p.health.AddHediff(expHediff);
            }
            else
            {
                expHediff.Severity += ext.psychonautExperienceGain;
            }

            // First trip tale (Tier 3 only)
            if (isFirstTrip && spawned)
                TaleRecorder.RecordTale(RP_TaleDefOf.RP_FirstTrip, p);

            // Acquisition roll
            if (!ext.countsPsychonautExperience)
                return;

            float severity = expHediff.Severity;
            float threshold = (severity * 2f) - 0.5f;
            if (threshold <= 0f || Rand.Value >= threshold)
                return;

            // Grant Psychonaut
            p.story.traits.GainTrait(new Trait(psychonautDef, 0));
            p.health.RemoveHediff(expHediff);

            if (spawned)
                TaleRecorder.RecordTale(RP_TaleDefOf.RP_GainedPsychonaut, p);
        }

        private void TryGrantInspiration(Pawn p, PsychedelicDrugExtension ext)
        {
            if (!wasZeroTolerance)
                return;

            bool canInspire = tripValence == TripValence.Good
                ? ext.canGoodTripInspire
                : ext.canBadTripInspire;

            if (!canInspire)
                return;

            if (Rand.Value >= ext.inspirationChance)
                return;

            // TODO: route through VSIE_Compat when available
            InspirationDef inspiration = p.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
            p.mindState.inspirationHandler.TryStartInspiration(inspiration, $"Being high on {Props.drugDef.label} caused {p.LabelShortCap} to become inspired.");
        }

        private void TryModifyTraits(Pawn p, PsychedelicDrugExtension ext, bool spawned)
        {
            if (!wasZeroTolerance)
                return;
            if (!ext.canModifyTraits)
                return;
            if (!RimPsychedelicsMod.Settings.traitChangesEnabled)
                return;

            bool isPsychonaut = p.story?.traits?.HasTrait(
                DefDatabase<TraitDef>.GetNamedSilentFail("Psychonaut")) == true;

            float chance = isPsychonaut
                ? ext.traitChangeChancePsychonaut
                : ext.traitChangeChance;

            if (Rand.Value >= chance)
                return;

            TraitModification mod = TraitLogic.BuildAndSelectModification(
                p, ext, Props.drugDef, tripValence);

            if (mod != null)
                TraitLogic.ApplyModification(p, mod, Props.drugDef, spawned);
        }

        private void RecordTrip(Pawn p, PsychedelicDrugExtension ext, bool spawned)
        {
            // Tales — Tier 3 only
            if (spawned)
            {
                TaleDef taleDef = tripValence == TripValence.Good
                    ? (ext.goodTripTale ?? RP_TaleDefOf.RP_HadGoodTrip)
                    : (ext.badTripTale ?? RP_TaleDefOf.RP_HadBadTrip);
                TaleRecorder.RecordTale(taleDef, p, Props.drugDef);
            }

            // RP_RecentTrip thought — Tier 2+
            p.needs?.mood?.thoughts?.memories?.TryGainMemory(RP_ThoughtDefOf.RP_RecentTrip);
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref wasZeroTolerance, "wasZeroTolerance", false);
            Scribe_Values.Look(ref tripValence, "tripValence", TripValence.Good);
            Scribe_Values.Look(ref maxSeverityCap, "maxSeverityCap", 1f);
        }
    }
}
