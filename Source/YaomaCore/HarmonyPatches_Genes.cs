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
    public static class HarmonyPatches_Genes
    {
        public static void Patch(Harmony harmony)
        {
            // GetBodyTypeFor -> GetBodyTypeFor_PostFix
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GetBodyTypeFor"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(GetBodyTypeFor_PostFix)));

            // GetBodyTypeFor -> GetBodyTypeFor_PostFix
            harmony.Patch(AccessTools.Method(typeof(GeneUtility), "ToBodyType"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(ToBodyType_PostFix)));
        }

        // POSTFIX: when generating a pawn, forces male and fat bodytype to be female when the pawn has the Hermaproditism gene.
        public static void GetBodyTypeFor_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaCore_Hermaphroditism) != null && 
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = BodyTypeDefOf.Female;
        }

        // POSTFIX: when adding this gene, forces male and fat bodytype to be female when the pawn has the Hermaproditism gene.
        public static void ToBodyType_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaCore_Hermaphroditism) != null &&
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = BodyTypeDefOf.Female;
        }

    }
}
