using System.Collections.Generic;
using Verse;

namespace RimPsychedelics
{
    public struct RitualIngestionContext
    {
        public float comeUpRateMultiplier;
        public bool applyRitualValenceBonus;
    }

    public static class RitualIngestionRegistry
    {
        private static readonly Dictionary<Pawn, RitualIngestionContext> pending
            = new Dictionary<Pawn, RitualIngestionContext>();

        public static void Set(Pawn pawn, RitualIngestionContext ctx)
        {
            if (pawn == null) return;
            pending[pawn] = ctx;
        }

        public static bool TryConsume(Pawn pawn, out RitualIngestionContext ctx)
        {
            if (pawn != null && pending.TryGetValue(pawn, out ctx))
            {
                pending.Remove(pawn);
                return true;
            }
            ctx = default(RitualIngestionContext);
            return false;
        }

        public static void Clear(Pawn pawn)
        {
            if (pawn == null) return;
            pending.Remove(pawn);
        }
    }
}
