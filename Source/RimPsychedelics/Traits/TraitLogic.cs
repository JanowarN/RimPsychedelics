using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimPsychedelics
{
    public class TraitModification
    {
        public Trait toRemove;
        public Trait toAdd;
        public string message;
        public TaleDef tale;
    }

    public static class TraitLogic
    {
        private struct Candidate
        {
            public Trait toRemove;
            public Trait toAdd;
            public string message;
            public TaleDef tale;
            public float weight;
        }

        public static TraitModification BuildAndSelectModification(
            Pawn pawn,
            PsychedelicDrugExtension ext,
            ThingDef drugDef,
            TripValence valence)
        {
            var pool = new List<Candidate>();

            if (ext.traitSpectrums != null)
            {
                for (int i = 0; i < ext.traitSpectrums.Count; i++)
                    ProcessSpectrumEntry(ext.traitSpectrums[i], pawn, drugDef, valence, pool);
            }

            if (ext.traitStandalones != null)
            {
                for (int i = 0; i < ext.traitStandalones.Count; i++)
                    ProcessStandaloneEntry(ext.traitStandalones[i], pawn, drugDef, valence, pool);
            }

            if (pool.Count == 0)
                return null;

            // Weighted random selection
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
                totalWeight += pool[i].weight;

            float roll = Rand.Value * totalWeight;
            float cumulative = 0f;
            Candidate selected = pool[pool.Count - 1];

            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += pool[i].weight;
                if (roll < cumulative)
                {
                    selected = pool[i];
                    break;
                }
            }

            return new TraitModification
            {
                toRemove = selected.toRemove,
                toAdd = selected.toAdd,
                message = selected.message,
                tale = selected.tale
            };
        }

        public static void ApplyModification(Pawn pawn, TraitModification mod, ThingDef drugDef, bool spawned)
        {
            if (mod.toRemove != null)
                SafeRemoveTrait(pawn, mod.toRemove);
            if (mod.toAdd != null)
                SafeAddTrait(pawn, mod.toAdd);

            // Notification letter
            if (PawnUtility.ShouldSendNotificationAbout(pawn) && mod.message != null)
            {
                Find.LetterStack.ReceiveLetter(
                    "RP_TraitChanged".Translate(),
                    mod.message,
                    LetterDefOf.NeutralEvent,
                    pawn);
            }

            // Tale — requires custom Tale_SinglePawnDefAndTrait class (TODO: build tale class)
            if (spawned && mod.tale != null)
                TaleRecorder.RecordTale(mod.tale, pawn, drugDef, mod.toAdd?.def ?? mod.toRemove?.def);
        }

        public static void SafeAddTrait(Pawn pawn, Trait trait)
        {
            if (pawn?.story?.traits == null)
                return;
            if (pawn.story.traits.HasTrait(trait.def))
                return;
            if (ConflictsWithExisting(pawn, trait.def))
                return;
            pawn.story.traits.GainTrait(trait);
        }

        public static void SafeRemoveTrait(Pawn pawn, Trait trait)
        {
            if (pawn?.story?.traits == null)
                return;
            Trait existing = pawn.story.traits.GetTrait(trait.def);
            if (existing == null)
                return;
            pawn.story.traits.RemoveTrait(existing);
        }

        // --- Spectrum entries ---

        private static void ProcessSpectrumEntry(
            TraitSpectrumEntry entry,
            Pawn pawn,
            ThingDef drugDef,
            TripValence valence,
            List<Candidate> pool)
        {
            TraitDef traitDef = entry.traitDef;
            if (traitDef == null)
                return;

            // 1. Pawn has this trait at a degree outside the defined range? Exclude.
            Trait existing = pawn.story.traits.GetTrait(traitDef);
            if (existing != null && (existing.Degree < entry.minDegree || existing.Degree > entry.maxDegree))
                return;

            // 2. Build step sequence: integers from minDegree to maxDegree.
            //    Each step is a degree (int) or null for the "no trait" state at position 0.
            var steps = new List<int?>();
            for (int d = entry.minDegree; d <= entry.maxDegree; d++)
            {
                if (HasDegree(traitDef, d))
                    steps.Add(d);
                else if (d == 0)
                    steps.Add(null); // "no trait" position
            }

            if (steps.Count < 2)
                return;

            // 3. Find pawn's current position in the step sequence
            int currentIdx;
            if (existing == null)
            {
                currentIdx = steps.IndexOf(null);
                if (currentIdx < 0)
                    return; // "no trait" not in range — pawn must have the trait to participate
            }
            else
            {
                currentIdx = FindDegreeIndex(steps, existing.Degree);
                if (currentIdx < 0)
                    return;
            }

            // 4. Roll direction using bias
            float bias = valence == TripValence.Good ? entry.goodTripBias : entry.badTripBias;
            bool goHigher = Rand.Value < (bias + 1f) / 2f;

            // 5. Calculate target
            int targetIdx = goHigher ? currentIdx + 1 : currentIdx - 1;

            // 6. Out of bounds?
            if (targetIdx < 0 || targetIdx >= steps.Count)
                return;

            int? targetDegree = steps[targetIdx];

            // Determine what to add/remove
            Trait toRemove = null;
            Trait toAdd = null;

            if (targetDegree == null)
            {
                // Moving to "no trait" — just remove
                toRemove = existing;
            }
            else if (existing == null)
            {
                // Moving from "no trait" to a degree — add
                toAdd = new Trait(traitDef, targetDegree.Value);
            }
            else
            {
                // Degree to degree — remove old, add new
                toRemove = existing;
                toAdd = new Trait(traitDef, targetDegree.Value);
            }

            // 7. Conflict check when adding a trait the pawn doesn't already have
            if (existing == null && toAdd != null && ConflictsWithExisting(pawn, traitDef))
                return;

            // Determine tale: intensifying (away from center) vs softening (toward center)
            int currentDist = existing != null ? Math.Abs(existing.Degree) : 0;
            int targetDist = targetDegree.HasValue ? Math.Abs(targetDegree.Value) : 0;
            TaleDef tale = targetDist > currentDist
                ? RP_TaleDefOf.RP_TraitIntensifiedByTrip
                : RP_TaleDefOf.RP_TraitSoftenedByTrip;

            // Build notification message
            string customMsg = goHigher ? entry.higherMessage : entry.lowerMessage;
            string message;

            if (customMsg != null)
            {
                string label = toAdd != null
                    ? GetTraitLabel(traitDef, toAdd.Degree)
                    : GetTraitLabel(traitDef, toRemove.Degree);
                message = TraitNotification.FormatMessage(customMsg, pawn, drugDef, label);
            }
            else if (toRemove != null && toAdd != null)
            {
                message = TraitNotification.DefaultShiftedMessage(
                    pawn, drugDef,
                    GetTraitLabel(traitDef, toRemove.Degree),
                    GetTraitLabel(traitDef, toAdd.Degree));
            }
            else if (toAdd != null)
            {
                message = TraitNotification.DefaultGainedMessage(
                    pawn, drugDef, GetTraitLabel(traitDef, toAdd.Degree));
            }
            else
            {
                message = TraitNotification.DefaultLostMessage(
                    pawn, drugDef, GetTraitLabel(traitDef, toRemove.Degree));
            }

            pool.Add(new Candidate
            {
                toRemove = toRemove,
                toAdd = toAdd,
                message = message,
                tale = tale,
                weight = entry.selectionWeight
            });
        }

        // --- Standalone entries ---

        private static void ProcessStandaloneEntry(
            TraitStandaloneEntry entry,
            Pawn pawn,
            ThingDef drugDef,
            TripValence valence,
            List<Candidate> pool)
        {
            TraitDef traitDef = entry.traitDef;
            if (traitDef == null)
                return;

            // Roll action using bias
            float bias = valence == TripValence.Good ? entry.goodTripBias : entry.badTripBias;
            bool doAdd = Rand.Value < (bias + 1f) / 2f;

            Trait existing = pawn.story.traits.GetTrait(traitDef);
            string label = GetTraitLabel(traitDef, entry.degree);

            if (doAdd)
            {
                if (existing != null)
                    return;
                if (ConflictsWithExisting(pawn, traitDef))
                    return;

                string msg = entry.addedMessage != null
                    ? TraitNotification.FormatMessage(entry.addedMessage, pawn, drugDef, label)
                    : TraitNotification.DefaultGainedMessage(pawn, drugDef, label);

                pool.Add(new Candidate
                {
                    toAdd = new Trait(traitDef, entry.degree),
                    message = msg,
                    tale = RP_TaleDefOf.RP_TraitGainedByTrip,
                    weight = entry.selectionWeight
                });
            }
            else
            {
                if (existing == null)
                    return;

                string existingLabel = GetTraitLabel(traitDef, existing.Degree);
                string msg = entry.removedMessage != null
                    ? TraitNotification.FormatMessage(entry.removedMessage, pawn, drugDef, existingLabel)
                    : TraitNotification.DefaultLostMessage(pawn, drugDef, existingLabel);

                pool.Add(new Candidate
                {
                    toRemove = existing,
                    message = msg,
                    tale = RP_TaleDefOf.RP_TraitLostByTrip,
                    weight = entry.selectionWeight
                });
            }
        }

        // --- Helpers ---

        private static bool ConflictsWithExisting(Pawn pawn, TraitDef newTraitDef)
        {
            List<Trait> traits = pawn.story.traits.allTraits;
            for (int i = 0; i < traits.Count; i++)
            {
                TraitDef existingDef = traits[i].def;

                if (existingDef.conflictingTraits != null && existingDef.conflictingTraits.Contains(newTraitDef))
                    return true;
                if (newTraitDef.conflictingTraits != null && newTraitDef.conflictingTraits.Contains(existingDef))
                    return true;

                if (newTraitDef.exclusionTags != null && existingDef.exclusionTags != null)
                {
                    for (int t = 0; t < newTraitDef.exclusionTags.Count; t++)
                    {
                        if (existingDef.exclusionTags.Contains(newTraitDef.exclusionTags[t]))
                            return true;
                    }
                }
            }
            return false;
        }

        private static bool HasDegree(TraitDef def, int degree)
        {
            List<TraitDegreeData> data = def.degreeDatas;
            if (data == null)
                return false;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].degree == degree)
                    return true;
            }
            return false;
        }

        private static int FindDegreeIndex(List<int?> steps, int degree)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == degree)
                    return i;
            }
            return -1;
        }

        private static string GetTraitLabel(TraitDef def, int degree)
        {
            List<TraitDegreeData> data = def.degreeDatas;
            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].degree == degree)
                        return data[i].label ?? def.defName;
                }
            }
            return def.defName;
        }
    }
}
