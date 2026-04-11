using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace RimPsychedelics
{
    public class Tale_SinglePawnDefAndTrait : Tale_SinglePawnAndDef
    {
        public TaleData_Def traitData;
        public string degreeLabel;

        public Tale_SinglePawnDefAndTrait() { }

        public Tale_SinglePawnDefAndTrait(Pawn pawn, ThingDef drug, TraitDef trait, string degreeLabel)
            : base(pawn, drug)
        {
            traitData = TaleData_Def.GenerateFrom(trait);
            this.degreeLabel = degreeLabel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref traitData, "traitData");
            Scribe_Values.Look(ref degreeLabel, "degreeLabel");
        }

        public override void GenerateTestData()
        {
            base.GenerateTestData();
            traitData = TaleData_Def.GenerateFrom(
                DefDatabase<TraitDef>.AllDefsListForReading.RandomElement());
            degreeLabel = "test trait";
        }

        protected override IEnumerable<Rule> SpecialTextGenerationRules(Dictionary<string, string> outConstants)
        {
            foreach (Rule rule in base.SpecialTextGenerationRules(outConstants))
                yield return rule;

            // Use degree-specific label for [trait_label] if available,
            // fall back to TaleData_Def rules for the generic TraitDef label
            if (!degreeLabel.NullOrEmpty())
                yield return new Rule_String("trait_label", degreeLabel);
            else
                foreach (Rule rule in traitData.GetRules("trait"))
                    yield return rule;
        }
    }
}
