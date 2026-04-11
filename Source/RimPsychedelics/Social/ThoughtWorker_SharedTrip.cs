using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class ThoughtWorker_SharedTrip : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!VSIE_Compat.MemoriesEnabled)
                return ThoughtState.Inactive;

            if (!otherPawn.RaceProps.Humanlike)
                return ThoughtState.Inactive;

            if (!RelationsUtility.PawnsKnowEachOther(p, otherPawn))
                return ThoughtState.Inactive;

            List<Tale> tales = Find.TaleManager.AllTalesListForReading;
            for (int i = 0; i < tales.Count; i++)
            {
                if (tales[i].def != RP_TaleDefOf.RP_SharedTrip)
                    continue;

                Tale_DoublePawnAndDef tale = tales[i] as Tale_DoublePawnAndDef;
                if (tale == null)
                    continue;

                if ((tale.firstPawnData?.pawn == p && tale.secondPawnData?.pawn == otherPawn) ||
                    (tale.firstPawnData?.pawn == otherPawn && tale.secondPawnData?.pawn == p))
                    return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.Inactive;
        }
    }
}
