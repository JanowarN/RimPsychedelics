using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimPsychedelics
{
    public static class VSIE_Compat
    {
        private static bool? cachedIsActive;
        private static FieldInfo memoriesField;
        private static bool memoriesFieldResolved;

        public static bool IsActive
        {
            get
            {
                if (!cachedIsActive.HasValue)
                    cachedIsActive = ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded");
                return cachedIsActive.Value;
            }
        }

        public static bool MemoriesEnabled
        {
            get
            {
                if (!IsActive)
                    return false;

                if (!memoriesFieldResolved)
                {
                    var settingsType = AccessTools.TypeByName(
                        "VanillaSocialInteractionsExpanded.VanillaSocialInteractionsExpandedSettings");
                    memoriesField = settingsType?.GetField("EnableMemories",
                        BindingFlags.Public | BindingFlags.Static);
                    memoriesFieldResolved = true;
                }

                if (memoriesField == null)
                    return false;

                return (bool)memoriesField.GetValue(null);
            }
        }
    }
}
