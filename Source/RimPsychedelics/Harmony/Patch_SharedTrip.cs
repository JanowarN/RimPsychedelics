using HarmonyLib;
using RimWorld;
using Verse;

namespace RimPsychedelics
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
    public static class Patch_SharedTrip
    {
        private static void Postfix(bool __result, Pawn ___pawn, Pawn recipient)
        {
            if (!__result)
                return;

            // Check if both pawns have active RP peak hediffs
            HediffComp_TripResolution initiatorComp = FindTripResolutionComp(___pawn);
            HediffComp_TripResolution recipientComp = FindTripResolutionComp(recipient);

            if (initiatorComp == null || recipientComp == null)
                return;

            // Only trigger when both are on the same drug
            ThingDef initiatorDrug = initiatorComp.Props.drugDef;
            ThingDef recipientDrug = recipientComp.Props.drugDef;

            if (initiatorDrug == null || initiatorDrug != recipientDrug)
                return;

            TaleRecorder.RecordTale(RP_TaleDefOf.RP_SharedTrip, ___pawn, recipient, initiatorDrug);
        }

        private static HediffComp_TripResolution FindTripResolutionComp(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffComp_TripResolution comp = hediffs[i].TryGetComp<HediffComp_TripResolution>();
                if (comp != null)
                    return comp;
            }
            return null;
        }
    }
}
