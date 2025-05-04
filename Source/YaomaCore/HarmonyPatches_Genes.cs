using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;

namespace YaomaCore.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches_Genes
    {
        public static List<BodyTypeDef> bodyTypes = new List<BodyTypeDef>() { BodyTypeDefOf.Female, BodyTypeDefOf.Thin, BodyTypeDefOf.Hulk };

        public static void Patch(Harmony harmony)
        {
            // GetBodyTypeFor -> GetBodyTypeFor_PostFix
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GetBodyTypeFor"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(GetBodyTypeFor_PostFix)));

            // GetBodyTypeFor -> GetBodyTypeFor_PostFix
            harmony.Patch(AccessTools.Method(typeof(GeneUtility), "ToBodyType"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(ToBodyType_PostFix)));

        }

        // POSTFIX: when generating a pawn, forces male and fat bodytype to be female when the pawn has YaomaCore's Hermaproditism gene.
        public static void GetBodyTypeFor_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaXenotype_Hermaphroditism) != null && 
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = bodyTypes.RandomElementWithFallback(BodyTypeDefOf.Female);
        }

        // POSTFIX: when adding this gene, forces male and fat bodytype to be female when the pawn has YaomaCore's Hermaproditism gene.
        public static void ToBodyType_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaXenotype_Hermaphroditism) != null &&
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = bodyTypes.RandomElementWithFallback(BodyTypeDefOf.Female);
        }

        
    }
}
