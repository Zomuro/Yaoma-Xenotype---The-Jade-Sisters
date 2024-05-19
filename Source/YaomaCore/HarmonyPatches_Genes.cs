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

            // CanEverProduceChild_Prefix -> CanEverProduceChild
            harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "CanEverProduceChild"),
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(CanEverProduceChild_Prefix)));

            // JobDriver_Lovin.MakeNewToils (JobDriverLovin_Transpiler)
            /*harmony.Patch(AccessTools.Method(typeof(JobDriver_Lovin), "<MakeNewToils>b__12_4"), null, null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(JobDriverLovin_Transpiler)));*/

            // JobDriver_Lovin.MakeNewToils -> JobDriverLovin_Postfix
            harmony.Patch(AccessTools.Method(typeof(JobDriver_Lovin), "<MakeNewToils>b__12_4"), null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(JobDriverLovin_MakeNewToils_Postfix)));

            // InRelation_Prefix -> InRelation
            harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Child), "InRelation"), 
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(InRelation_Prefix)));

            // Recipe_Surgery.AvailableOnNow (AvailableOnNow_Transpiler)
            harmony.Patch(AccessTools.Method(typeof(Recipe_Surgery), "AvailableOnNow"), null, null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(AvailableOnNow_Transpiler)));

            // InbredChanceFromParents -> InbredChanceFromParents_Postfix
            harmony.Patch(AccessTools.Method(typeof(PregnancyUtility), "InbredChanceFromParents"), null, 
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(InbredChanceFromParents_Postfix)));

            // HumanOvum.CanFertilizeReport (CanFertilizeReport_Transplier)
            harmony.Patch(AccessTools.Method(typeof(HumanOvum), "CanFertilizeReport"), null, null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(CanFertilizeReport_Transplier)));

            // HumanEmbryo.CanImplantReport (CanImplantReportTransplier)
            harmony.Patch(AccessTools.Method(typeof(HumanEmbryo), "CanImplantReport"), null, null,
                new HarmonyMethod(typeof(HarmonyPatches_Genes), nameof(CanImplantReportTransplier)));
        }

        // POSTFIX: when generating a pawn, forces male and fat bodytype to be female when the pawn has YaomaCore's Hermaproditism gene.
        public static void GetBodyTypeFor_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaCore_Hermaphroditism) != null && 
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = bodyTypes.RandomElementWithFallback(BodyTypeDefOf.Female);
        }

        // POSTFIX: when adding this gene, forces male and fat bodytype to be female when the pawn has YaomaCore's Hermaproditism gene.
        public static void ToBodyType_PostFix(Pawn pawn, ref BodyTypeDef __result)
        {
            if (pawn.genes.GetGene(DefOfs.YaomaCore_Hermaphroditism) != null &&
                (__result == BodyTypeDefOf.Male || __result == BodyTypeDefOf.Fat))
                __result = bodyTypes.RandomElementWithFallback(BodyTypeDefOf.Female);
        }

        // PREFIX: if either the first or second pawn is a herm, shunt the acceptance report into our own different exec flow
        public static bool CanEverProduceChild_Prefix(Pawn first, Pawn second, ref AcceptanceReport __result)
        {
            if(HermaphroditeUtility.IsPawnHerm(first) || HermaphroditeUtility.IsPawnHerm(second))
            {
                __result = CanEverProduceChild_Helper(first, second);
                return false;
            }

            return true;
        }

        // transitioned from transplier -> prefix to avoid HAR collision
        public static AcceptanceReport CanEverProduceChild_Helper(Pawn first, Pawn second)
        {
            if (first.Dead) return "PawnIsDead".Translate(first.Named("PAWN"));
            if (second.Dead) return "PawnIsDead".Translate(second.Named("PAWN"));

            if (!HermaphroditeUtility.ArePawnsCompatible(first, second)) return "PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();

            // handle the following case - null cases OR father is fine, mother is pregnant or lactating
            HermaphroditeUtility.ParentPair pair = HermaphroditeUtility.SelectParents(first, second);
            if (pair.mother is null || pair.father is null) return HermaphroditeUtility.BuildPregnancyFailure(pair.mother, pair.father);

            Pawn father = pair.father;
            Pawn mother = pair.mother;

            // fertility check
            bool fatherFertility = father.GetStatValue(StatDefOf.Fertility, true, -1) <= 0f;
            bool motherFertility = mother.GetStatValue(StatDefOf.Fertility, true, -1) <= 0f;
            if (fatherFertility && motherFertility) return "PawnsAreInfertile".Translate(father.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
            if (fatherFertility != motherFertility) return "PawnIsInfertile".Translate((fatherFertility ? father : mother).Named("PAWN")).Resolve();

            // life stage check
            bool fatherCanReprod = !father.ageTracker.CurLifeStage.reproductive;
            bool motherCanReprod = !mother.ageTracker.CurLifeStage.reproductive;
            if (fatherCanReprod && motherCanReprod) return "PawnsAreTooYoung".Translate(father.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
            if (fatherCanReprod != motherCanReprod) return "PawnIsTooYoung".Translate((fatherCanReprod ? father : mother).Named("PAWN")).Resolve();

            // sterility check
            bool motherSterile = mother.Sterile() && PregnancyUtility.GetPregnancyHediff(mother) == null;
            bool fatherSterile = father.Sterile();
            if (fatherSterile && motherSterile) return "PawnsAreSterile".Translate(father.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
            if (fatherSterile != motherSterile) return "PawnIsSterile".Translate((fatherSterile ? father : mother).Named("PAWN")).Resolve();

            return true;
        }

/*        public static MethodInfo _method_select_parents = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.SelectParentsDriver));
        public static FieldInfo _field_mother = AccessTools.Field(typeof(HermaphroditeUtility.ParentPair), nameof(HermaphroditeUtility.ParentPair.mother));
        public static FieldInfo _field_father = AccessTools.Field(typeof(HermaphroditeUtility.ParentPair), nameof(HermaphroditeUtility.ParentPair.father));
        public static FieldInfo _field_pregnancy_chance = AccessTools.Field(typeof(JobDriver_Lovin), "PregnancyChance");*/

        // from Hermaprodite Genes: changes the logic to determine whether a pregnancy attempt is ever possible.
        public static IEnumerable<CodeInstruction> JobDriverLovin_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = il.Count - 1; i > 0; --i)
            {
                // Scan until we find the load to JobDriver_Lovin::PregnancyChance.
                if (il[i].opcode != OpCodes.Ldsfld || !il[i].LoadsField(HermaphroditeUtility._field_pregnancy_chance)) continue;

                // From this load, we scan back until we reach stloc.3, which is storing the mother.
                while (i > 0 && il[i].opcode != OpCodes.Stloc_3) { --i; }

                // Right after this instruction, we push thisptr (position 0), then call our function.
                il.Insert(++i, new CodeInstruction(OpCodes.Ldarg_0));
                il.Insert(++i, new CodeInstruction(OpCodes.Call, HermaphroditeUtility._method_select_parents));

                // Then we load the fields from our struct and store them, stloc.2 for father and stloc.3 for mother.
                il.Insert(++i, new CodeInstruction(OpCodes.Dup));
                il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, HermaphroditeUtility._field_father));
                il.Insert(++i, new CodeInstruction(OpCodes.Stloc_2));
                il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, HermaphroditeUtility._field_mother));
                il.Insert(++i, new CodeInstruction(OpCodes.Stloc_3));

                return il.AsEnumerable();
            }

            throw new Exception("Could not locate the correct instruction to patch - a mod incompatibility or game update broke it.");
        }

        public static IEnumerable<Toil> JobDriverLovin_MakeNewToils_Postfix(IEnumerable<Toil> values, JobDriver_Lovin __instance)
        {
            /*IEnumerable<Toil> tempToils = __result;
            Toil lovinToil = tempToils.FirstOrDefault(x => x.debugName == "LayDown"); // target the portion of the toils list where pawns are lovin' 
            if (lovinToil is null) return; // no lovin toil -> skip the postfix*/

            foreach (var toil in values)
            {
                if (toil.debugName != "LayDown") yield return toil;

                // add an additional finishing action, this time handling
                Traverse traverse = new Traverse(__instance);
                Pawn partner = traverse.Method("get_Partner").GetValue<Pawn>(); // get the partner in this jobdriver
                float pregChance = traverse.Field("PregnancyChance").GetValue<float>(); //test
                if (!HermaphroditeUtility.IsPawnHerm(partner) && !HermaphroditeUtility.IsPawnHerm(__instance.pawn)) yield return toil; // if neither pawn is a herm, don't bother!


                /*Toil hermToil = ToilMaker.MakeToil("HermLayDown");
                hermToil.FailOn(() => partner.CurJob == null || partner.CurJob.def != JobDefOf.Lovin);*/
                // else, add a herm-inclusive finish action to the toil
                toil.AddFinishAction(delegate
                {
                    if (ModsConfig.BiotechActive)
                    {
                        // determine who is father and mother
                        HermaphroditeUtility.ParentPair pair = HermaphroditeUtility.SelectParents(__instance.pawn, partner);
                        Pawn pawn = pair.father;
                        Pawn pawn2 = pair.mother;

                        // get pregnancy chance from jobdriver
                        if (pawn != null && pawn2 != null && Rand.Chance(pregChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                        {
                            GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(pawn, pawn2, out bool flag);
                            if (flag)
                            {
                                Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2, null);
                                hediff_Pregnant.SetParents(null, pawn, inheritedGeneSet);
                                pawn2.health.AddHediff(hediff_Pregnant, null, null, null);
                                return;
                            }
                            if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(pawn2))
                            {
                                Messages.Message("MessagePregnancyFailed".Translate(pawn.Named("FATHER"), pawn2.Named("MOTHER")) + ": " + "CombinedGenesExceedMetabolismLimits".Translate(), new LookTargets(new TargetInfo[]
                                {
                                pawn,
                                pawn2
                                }), MessageTypeDefOf.NegativeEvent, true);
                            }
                        }
                    }
                });
                yield return toil;
            }

            /*// add an additional finishing action, this time handling
            Traverse traverse = new Traverse(__instance);
            Pawn partner = traverse.Method("get_Partner").GetValue<Pawn>(); // get the partner in this jobdriver
            if (!HermaphroditeUtility.IsPawnHerm(partner) && !HermaphroditeUtility.IsPawnHerm(__instance.pawn)) yield break; // if neither pawn is a herm, don't bother!


            Toil hermToil = ToilMaker.MakeToil("HermLayDown");
            hermToil.FailOn(() => partner.CurJob == null || partner.CurJob.def != JobDefOf.Lovin);
            // else, add a herm-inclusive finish action to the toil
            hermToil.AddFinishAction(delegate
            {
                if (ModsConfig.BiotechActive)
                {
                    // determine who is father and mother
                    HermaphroditeUtility.ParentPair pair = HermaphroditeUtility.SelectParents(__instance.pawn, partner);
                    Pawn pawn = pair.father;
                    Pawn pawn2 = pair.mother;

                    // get pregnancy chance from jobdriver
                    float pregChance = traverse.Field("PregnancyChance").GetValue<float>();
                    if (pawn != null && pawn2 != null && Rand.Chance(pregChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                    {
                        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(pawn, pawn2, out bool flag);
                        if (flag)
                        {
                            Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2, null);
                            hediff_Pregnant.SetParents(null, pawn, inheritedGeneSet);
                            pawn2.health.AddHediff(hediff_Pregnant, null, null, null);
                            return;
                        }
                        if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(pawn2))
                        {
                            Messages.Message("MessagePregnancyFailed".Translate(pawn.Named("FATHER"), pawn2.Named("MOTHER")) + ": " + "CombinedGenesExceedMetabolismLimits".Translate(), new LookTargets(new TargetInfo[]
                            {
                                pawn,
                                pawn2
                            }), MessageTypeDefOf.NegativeEvent, true);
                        }
                    }
                }
            });
            hermToil.socialMode = RandomSocialMode.Off;
            yield return hermToil;*/
            yield break;
        }


        public static bool InRelation_Prefix(Pawn me, Pawn other, ref bool __result)
        {
            __result = me != other && (
                HermaphroditeUtility.GetAllParentsOfGender(other, Gender.Female).Contains(me) ||
                HermaphroditeUtility.GetAllParentsOfGender(other, Gender.Male).Contains(me) ||
                (ModsConfig.BiotechActive && other.GetBirthParent() == me));

            return false;
        }

        public static IEnumerable<CodeInstruction> AvailableOnNow_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                // Scan until we find a load to genderPrerequisite.
                if (!il[i].LoadsField(HermaphroditeUtility._field_gender_prerequisite)) continue;

                // Right after, we insert a call to our own function.
                il.Insert(++i, new CodeInstruction(OpCodes.Ldarg_1));
                il.Insert(++i, new CodeInstruction(OpCodes.Call, HermaphroditeUtility._method_select_gender));

                return il.AsEnumerable();
            }

            throw new Exception("Could not locate the correct instruction to patch - a mod incompatibility or game update broke it.");
        }

        public static void InbredChanceFromParents_Postfix(Pawn mother, Pawn father, ref PawnRelationDef relation, ref float __result)
        {
            if (mother != null && father != null && mother == father)
            {
                relation = PawnRelationDefOf.Sibling;
                __result = 1.0f;
            }
        }

        public static IEnumerable<CodeInstruction> CanFertilizeReport_Transplier(IEnumerable<CodeInstruction> instructions)
        {
            return ModifyIL(instructions, HermaphroditeUtility._method_can_fertilize);
        }

        public static IEnumerable<CodeInstruction> CanImplantReportTransplier(IEnumerable<CodeInstruction> instructions)
        {
            return ModifyIL(instructions, HermaphroditeUtility._method_can_implant);
        }

        public static IEnumerable<CodeInstruction> ModifyIL(IEnumerable<CodeInstruction> instructions, MethodInfo pred)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                // Scan until we find a load to gender.
                if (!il[i].LoadsField(HermaphroditeUtility._field_gender)) continue;

                // Delete the ldfld, and the load to a gender constant.
                il.RemoveRange(i, 2);

                // Insert a call to our function to replace the above two.
                il.Insert(i, new CodeInstruction(OpCodes.Call, pred));

                // Replace the compare with a branch-if-true.
                il[i + 1].opcode = OpCodes.Brtrue_S;

                return il.AsEnumerable();
            }

            throw new Exception("Could not locate the correct instruction to patch - a mod incompatibility or game update broke it.");
        }
    }
}
