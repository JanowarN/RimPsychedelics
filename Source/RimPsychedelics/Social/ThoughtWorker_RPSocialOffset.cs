using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class ThoughtWorker_RPSocialOffset : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            SocialOffsetExtension ext = def.GetModExtension<SocialOffsetExtension>();
            if (ext?.hediffDef == null || ext.severityThresholds == null)
                return ThoughtState.Inactive;

            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(ext.hediffDef);
            if (hediff == null)
                return ThoughtState.Inactive;

            float severity = hediff.Severity;

            for (int i = 0; i < ext.severityThresholds.Count; i++)
            {
                if (severity >= ext.severityThresholds[i])
                    return ThoughtState.ActiveAtStage(i);
            }

            return ThoughtState.ActiveAtStage(ext.severityThresholds.Count);
        }
    }
}
