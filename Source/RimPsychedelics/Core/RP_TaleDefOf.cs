using RimWorld;

namespace RimPsychedelics
{
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
        public static TaleDef RP_SharedTrip;

        static RP_TaleDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RP_TaleDefOf));
    }
}
