using System;
using UnityEngine;

namespace RimPsychedelics
{
    public static class ValenceCurveEvaluator
    {
        // logit(0.95) = ln(0.95 / 0.05) — plateau point reaches 95% of floor-to-ceiling range.
        private const float LogitP95 = 2.944f;

        public static float EvaluateGoodTripChance(
            float mood,
            PsychedelicDrugExtension ext,
            bool isPsychonaut,
            bool isRitual)
        {
            // Per-field Psychonaut fallback: -1 sentinel means "use normal curve value"
            float chanceAtZero = isPsychonaut && ext.psychonautGoodTripChanceAtZeroMood >= 0f
                ? ext.psychonautGoodTripChanceAtZeroMood
                : ext.goodTripChanceAtZeroMood;

            float upperLimit = isPsychonaut && ext.psychonautGoodTripChanceUpperLimit >= 0f
                ? ext.psychonautGoodTripChanceUpperLimit
                : ext.goodTripChanceUpperLimit;

            float transitionPoint = isPsychonaut && ext.psychonautGoodTripTransitionPoint >= 0f
                ? ext.psychonautGoodTripTransitionPoint
                : ext.goodTripTransitionPoint;

            float plateauPoint = isPsychonaut && ext.psychonautGoodTripPlateauPoint >= 0f
                ? ext.psychonautGoodTripPlateauPoint
                : ext.goodTripPlateauPoint;

            float chance = EvaluateSigmoid(mood, chanceAtZero, upperLimit, transitionPoint, plateauPoint);

            if (isRitual)
                chance += ext.ritualGoodTripChanceBonus;

            return Mathf.Clamp01(chance);
        }

        private static float EvaluateSigmoid(
            float mood,
            float chanceAtZero,
            float upperLimit,
            float transitionPoint,
            float plateauPoint)
        {
            double m = mood;
            double czm = chanceAtZero;
            double u = upperLimit;
            double tp = transitionPoint;
            double pp = plateauPoint;

            // Steepness derived from transition-to-plateau gap
            double k = LogitP95 / (pp - tp);

            // Sigmoid value at mood = 0, used to solve for the floor
            double s0 = 1.0 / (1.0 + Math.Exp(k * tp));

            // Floor (lower asymptote) — chosen so f(0) = chanceAtZero exactly
            double floor = (czm - u * s0) / (1.0 - s0);

            // Evaluate: f(mood) = floor + (upperLimit - floor) * σ(k * (mood - transitionPoint))
            double sigmoid = 1.0 / (1.0 + Math.Exp(-k * (m - tp)));
            return (float)(floor + (u - floor) * sigmoid);
        }
    }
}
