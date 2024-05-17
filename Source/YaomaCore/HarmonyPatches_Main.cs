using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace YaomaCore.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches_Main
    {
        static HarmonyPatches_Main()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Harmony harmony = new Harmony("Zomuro.YaomaXenotype");
            HarmonyPatches_Genes.Patch(harmony);
            HarmonyPatches_Stats.Patch(harmony);

            // reporting tool for checking on Harmony Patches - modified from Humanoid Alien Races's HarmonyPatches
            stopwatch.Stop();
            Log.Message(
                string.Format("YaomaCore.HarmonyPatches.HarmonyPatches_Main: successfully completed {0} patches in {1} secs.",
                    harmony.GetPatchedMethods().Select(new Func<MethodBase, Patches>(Harmony.GetPatchInfo)).SelectMany(
                        (Patches p) => p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers)).Count((Patch p) => p.owner == harmony.Id),
                    stopwatch.Elapsed.TotalSeconds
                )
            );

        }
    }
}
