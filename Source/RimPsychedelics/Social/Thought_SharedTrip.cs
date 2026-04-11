using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimPsychedelics
{
    public class Thought_SharedTrip : Thought_SituationalSocial
    {
        public override float OpinionOffset()
        {
            if (ThoughtUtility.ThoughtNullified(pawn, def))
                return 0f;

            float baseOffset = CurStage.baseOpinionOffset;

            TaleFadeExtension fade = def.GetModExtension<TaleFadeExtension>();
            if (fade == null || fade.falloffDays <= 0f)
                return baseOffset;

            Tale tale = FindSharedTripTale(pawn, otherPawn);
            if (tale == null)
                return baseOffset;

            float ageDays = tale.AgeTicks / 60000f;
            float t = Mathf.Clamp01(ageDays / fade.falloffDays);
            return Mathf.Lerp(baseOffset, fade.minimumOpinionOffset, t);
        }

        private static Tale FindSharedTripTale(Pawn p, Pawn other)
        {
            List<Tale> tales = Find.TaleManager.AllTalesListForReading;
            for (int i = 0; i < tales.Count; i++)
            {
                if (tales[i].def != RP_TaleDefOf.RP_SharedTrip)
                    continue;

                Tale_DoublePawnAndDef tale = tales[i] as Tale_DoublePawnAndDef;
                if (tale == null)
                    continue;

                if ((tale.firstPawnData?.pawn == p && tale.secondPawnData?.pawn == other) ||
                    (tale.firstPawnData?.pawn == other && tale.secondPawnData?.pawn == p))
                    return tale;
            }
            return null;
        }
    }
}
