using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class PsychedelicIngestionOutcomeDoer : IngestionOutcomeDoer
    {
        // XML field — matches the ChemicalDef for this drug's tolerance
        public ChemicalDef toleranceChemical;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            PsychedelicDrugExtension ext = ingested.def.GetModExtension<PsychedelicDrugExtension>();
            if (ext == null)
                return;

            // 1. Read current tolerance severity
            float tolerance = 0f;
            if (toleranceChemical?.toleranceHediff != null)
            {
                Hediff tolHediff = pawn.health.hediffSet.GetFirstHediffOfDef(toleranceChemical.toleranceHediff);
                if (tolHediff != null)
                    tolerance = tolHediff.Severity;
            }

            // 2. Gate check — tolerance above threshold wastes the dose
            if (tolerance > ext.noTripToleranceThreshold)
            {
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message(
                        "RP_NoEffectTolerance".Translate(pawn.LabelShort, ingested.def.label),
                        pawn,
                        MessageTypeDefOf.NeutralEvent);
                }
                return;
            }

            // 3. Calculate maxSeverityCap from tolerance (1.0 at zero, scaled down)
            float maxSeverityCap = 1f - tolerance;

            // 4. Track zero tolerance for gating inspiration/traits at resolution
            bool wasZeroTolerance = tolerance == 0f;

            // 5. Roll valence
            float mood = pawn.needs?.mood?.CurLevelPercentage ?? 0.5f;
            bool isPsychonaut = pawn.story?.traits?.HasTrait(
                DefDatabase<TraitDef>.GetNamedSilentFail("Psychonaut")) == true;

            float goodChance = ValenceCurveEvaluator.EvaluateGoodTripChance(
                mood, ext, isPsychonaut, isRitual: false);

            TripValence valence = Rand.Value < goodChance
                ? TripValence.Good
                : TripValence.Bad;

            // 6-7. Select peak hediff from extension based on valence
            HediffDef peakDef = valence == TripValence.Good
                ? ext.hediffDefPositiveTrip
                : ext.hediffDefNegativeTrip;

            // 8-9. Apply come-up hediff and configure instance fields
            if (ext.hediffDefComeUp == null)
                return;

            Hediff_PsychedelicComeUp comeUp = (Hediff_PsychedelicComeUp)HediffMaker.MakeHediff(
                ext.hediffDefComeUp, pawn);
            comeUp.maxSeverityCap = maxSeverityCap;
            comeUp.wasZeroTolerance = wasZeroTolerance;
            comeUp.nextHediffDef = peakDef;
            comeUp.tripValence = valence;
            comeUp.comeUpRateMultiplier = 1f;

            pawn.health.AddHediff(comeUp);
        }
    }
}
