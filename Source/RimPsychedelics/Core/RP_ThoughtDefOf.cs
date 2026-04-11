using RimWorld;

namespace RimPsychedelics
{
    [DefOf]
    public static class RP_ThoughtDefOf
    {
        public static ThoughtDef RP_RecentTrip;

        static RP_ThoughtDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RP_ThoughtDefOf));
    }
}
