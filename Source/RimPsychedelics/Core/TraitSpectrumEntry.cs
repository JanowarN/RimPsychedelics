using RimWorld;

namespace RimPsychedelics
{
    public class TraitSpectrumEntry
    {
        // Required
        public TraitDef traitDef;
        public int minDegree;
        public int maxDegree;

        // Optional — defaults applied by XML deserializer when omitted
        public float selectionWeight = 1f;
        public float goodTripBias = 0f;
        public float badTripBias = 0f;
        public string higherMessage;
        public string lowerMessage;
    }
}
