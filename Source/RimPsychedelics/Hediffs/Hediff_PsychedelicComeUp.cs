using Verse;

namespace RimPsychedelics
{
    public class Hediff_PsychedelicComeUp : HediffWithComps
    {
        // Runtime fields — set by PsychedelicIngestionOutcomeDoer at ingestion
        public float maxSeverityCap = 1f;
        public bool wasZeroTolerance;
        public HediffDef nextHediffDef;
        public TripValence tripValence;
        public float comeUpRateMultiplier = 1f;

        public override void PostTick()
        {
            float before = Severity;
            base.PostTick();

            // Scale the severity delta by the rate multiplier
            if (comeUpRateMultiplier != 1f)
            {
                float delta = Severity - before;
                Severity += delta * (comeUpRateMultiplier - 1f);
            }

            if (Severity >= maxSeverityCap)
            {
                Severity = maxSeverityCap;
                TransitionToPeak();
            }
        }

        private void TransitionToPeak()
        {
            Pawn p = pawn;

            // Tier 1 safety: abort if pawn null or dead
            if (p == null || p.Dead)
            {
                p?.health.RemoveHediff(this);
                return;
            }

            if (nextHediffDef == null)
            {
                p.health.RemoveHediff(this);
                return;
            }

            float peakSeverity = maxSeverityCap;
            bool zeroTol = wasZeroTolerance;
            TripValence valence = tripValence;

            // Remove come-up
            p.health.RemoveHediff(this);

            // Apply peak hediff
            Hediff peakHediff = HediffMaker.MakeHediff(nextHediffDef, p);
            peakHediff.Severity = peakSeverity;
            p.health.AddHediff(peakHediff);

            // Pass runtime data to resolution comp
            HediffComp_TripResolution resComp = peakHediff.TryGetComp<HediffComp_TripResolution>();
            if (resComp != null)
            {
                resComp.wasZeroTolerance = zeroTol;
                resComp.tripValence = valence;
                resComp.maxSeverityCap = peakSeverity;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxSeverityCap, "maxSeverityCap", 1f);
            Scribe_Values.Look(ref wasZeroTolerance, "wasZeroTolerance", false);
            Scribe_Defs.Look(ref nextHediffDef, "nextHediffDef");
            Scribe_Values.Look(ref tripValence, "tripValence", TripValence.Good);
            Scribe_Values.Look(ref comeUpRateMultiplier, "comeUpRateMultiplier", 1f);
        }
    }
}
