using RimWorld;

namespace RimPsychedelics
{
    [DefOf]
    public static class RP_HistoryEventDefOf
    {
        public static HistoryEventDef RP_IngestedPsychedelic;
        public static HistoryEventDef RP_IngestedPsychedelic_NotRitual;

        static RP_HistoryEventDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RP_HistoryEventDefOf));
    }
}
