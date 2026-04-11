using Verse;

namespace RimPsychedelics
{
    public static class TraitNotification
    {
        public static string FormatMessage(
            string template,
            Pawn pawn,
            ThingDef drug,
            string traitLabel)
        {
            return template
                .Replace("[PAWN_nameDef]", pawn.LabelShort)
                .Replace("[PAWN_pronoun]", SubjectPronoun(pawn))
                .Replace("[PAWN_possessive]", PossessivePronoun(pawn))
                .Replace("[DRUG_label]", drug.label)
                .Replace("[TRAIT_label]", traitLabel);
        }

        public static string DefaultGainedMessage(Pawn pawn, ThingDef drug, string traitLabel)
        {
            return FormatMessage("[PAWN_nameDef] developed the [TRAIT_label] trait after taking [DRUG_label].", pawn, drug, traitLabel);
        }

        public static string DefaultLostMessage(Pawn pawn, ThingDef drug, string traitLabel)
        {
            return FormatMessage("[PAWN_nameDef] is no longer [TRAIT_label] after taking [DRUG_label].", pawn, drug, traitLabel);
        }

        public static string DefaultShiftedMessage(Pawn pawn, ThingDef drug, string oldTraitLabel, string newTraitLabel)
        {
            string template = "[PAWN_nameDef] shifted from " + oldTraitLabel + " to " + newTraitLabel + " after taking [DRUG_label].";
            return FormatMessage(template, pawn, drug, newTraitLabel);
        }

        private static string SubjectPronoun(Pawn pawn)
        {
            switch (pawn.gender)
            {
                case Gender.Male: return "he";
                case Gender.Female: return "she";
                default: return "they";
            }
        }

        private static string PossessivePronoun(Pawn pawn)
        {
            switch (pawn.gender)
            {
                case Gender.Male: return "his";
                case Gender.Female: return "her";
                default: return "their";
            }
        }
    }
}
