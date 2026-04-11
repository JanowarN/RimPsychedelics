using RimWorld;

namespace RimPsychedelics
{
    public class TraitStandaloneEntry
    {
        // Required
        public TraitDef traitDef;

        // Optional — defaults applied by XML deserializer when omitted
        public int degree = 0;
        public float selectionWeight = 1f;
        public float goodTripBias = 0f;
        public float badTripBias = 0f;
        public string addedMessage;
        public string removedMessage;
    }
}
