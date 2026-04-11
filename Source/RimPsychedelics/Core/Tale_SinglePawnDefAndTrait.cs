using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace RimPsychedelics
{
    public class Tale_SinglePawnDefAndTrait : Tale_SinglePawnAndDef
    {
        public TaleData_Def traitData;

        public Tale_SinglePawnDefAndTrait() { }

        public Tale_SinglePawnDefAndTrait(Pawn pawn, ThingDef drug, TraitDef trait)
            : base(pawn, drug)
        {
            traitData = TaleData_Def.GenerateFrom(trait);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref traitData, "traitData");
        }

        public override void GenerateTestData()
        {
            base.GenerateTestData();
            traitData = TaleData_Def.GenerateFrom(
                DefDatabase<TraitDef>.AllDefsListForReading.RandomElement());
        }

        protected override IEnumerable<Rule> SpecialTextGenerationRules(Dictionary<string, string> outConstants)
        {
            foreach (Rule rule in base.SpecialTextGenerationRules(outConstants))
                yield return rule;
            foreach (Rule rule in traitData.GetRules("trait"))
                yield return rule;
        }
    }
}
