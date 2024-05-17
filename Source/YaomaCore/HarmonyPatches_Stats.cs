using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace YaomaCore.HarmonyPatches
{
    public static class HarmonyPatches_Stats
    {
        public static void Patch(Harmony harmony)
        {
            // get_HealthScale -> HealthScale_PostFix
            harmony.Patch(AccessTools.Method(typeof(Pawn), "get_HealthScale"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Stats), nameof(HealthScale_PostFix)));
        }

        // POSTFIX: modifies result to take into account pawn's HealthScaleFactor
        public static void HealthScale_PostFix(Pawn __instance, ref float __result)
        {
            __result *= __instance.GetStatValue(DefOfs.YaomaCore_HealthScaleFactor, true, 1000);
            return; 
        }
    }
}
