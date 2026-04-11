using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimPsychedelics
{
    public class RimPsychedelicsMod : Mod
    {
        public static RimPsychedelicsSettings Settings { get; private set; }

        public RimPsychedelicsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimPsychedelicsSettings>();

            var harmony = new Harmony("rimpsychedelics.main");
            harmony.PatchAll(typeof(RimPsychedelicsMod).Assembly);
        }

        public override string SettingsCategory() => "RimPsychedelics";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "RP_Setting_TraitChanges_Label".Translate(),
                ref Settings.traitChangesEnabled,
                "RP_Setting_TraitChanges_Tooltip".Translate());
            listing.End();
        }
    }

    public class RimPsychedelicsSettings : ModSettings
    {
        public bool traitChangesEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref traitChangesEnabled, "traitChangesEnabled", true);
        }
    }
}
