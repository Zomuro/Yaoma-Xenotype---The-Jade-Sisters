using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;

namespace YaomaCore.HarmonyPatches
{
    // This code is based off of
    public static class HermaphroditeUtility
    {
        public static MethodInfo _method_select_parents = AccessTools.Method(typeof(HermaphroditeUtility), nameof(SelectParentsDriver));
        public static FieldInfo _field_mother = AccessTools.Field(typeof(ParentPair), nameof(ParentPair.mother));
        public static FieldInfo _field_father = AccessTools.Field(typeof(ParentPair), nameof(ParentPair.father));
        public static FieldInfo _field_pregnancy_chance = AccessTools.Field(typeof(JobDriver_Lovin), "PregnancyChance");
        public static MethodInfo _method_select_gender = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.SelectGender));
        public static FieldInfo _field_gender_prerequisite = AccessTools.Field(typeof(RecipeDef), nameof(RecipeDef.genderPrerequisite));
        public static MethodInfo _method_are_pawns_compatible = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.ArePawnsCompatible));
        //public static MethodInfo _method_build_preggers_failure = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.BuildPregnancyFailure));
        public static MethodInfo _method_can_fertilize = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.CanFertilize));
        public static MethodInfo _method_can_implant = AccessTools.Method(typeof(HermaphroditeUtility), nameof(HermaphroditeUtility.CanImplant));
        public static FieldInfo _field_gender = AccessTools.Field(typeof(Pawn), nameof(Pawn.gender));

        public struct ParentPair
        {
            public Pawn mother;
            public Pawn father;
        }

        public static bool IsPawnHerm(Pawn pawn)
        {
            if (pawn.genes != null && pawn.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism)) return true;
            return false;
        }

        public static bool ArePawnsCompatible(Pawn pawn1, Pawn pawn2)
        {
            //if the two pawns are not the same gender or one or the other has genes and this gene
            return pawn1.gender != pawn2.gender ||
                (pawn1.genes != null && pawn1.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism)) ||
                (pawn2.genes != null && pawn2.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism));
        }

        public static AcceptanceReport BuildPregnancyFailure(Pawn pawn1, Pawn pawn2)
        {
            // {PAWN1_nameDef} and {PAWN2_nameDef} are temporarily incompatible because one of them is pregnant or lactating.
            return new AcceptanceReport("YaomaCore_BuildPregnancyFail".Translate(pawn1.Named("PAWN1"), pawn2.Named("PAWN2")).Resolve());

                //$"{pawn1.Name.ToStringShort} and {pawn2.Name.ToStringShort} are temporarily incompatible because one of them is pregnant or lactating.");
        }

        public static bool CanFulfillParentType(Pawn pawn, Gender gender)
        {
            return pawn.gender == gender || (pawn.genes != null && pawn.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism));
        }

        public static void CanFulfillParentType(Pawn pawn, ref List<Pawn> motherList, ref List<Pawn> fatherList)
        {
            if (pawn.genes != null && pawn.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism))
            {
                motherList.Add(pawn);
                fatherList.Add(pawn);
                return;
            }

            switch (pawn.gender)
            {
                case Gender.Female:
                    motherList.Add(pawn);
                    break;

                case Gender.Male:
                    fatherList.Add(pawn);
                    break;

                default:
                    break;
            }
        }

        public static ParentPair SelectParents(Pawn partner1, Pawn partner2)
        {
            // dev mode on -> introduce logs
            if (Prefs.DevMode) Log.Message($"({nameof(HermaphroditeUtility)}) Partner 1: {partner1.Name}; Partner 2: {partner2.Name}");

            // organize pawns into mothers and fathers
            List<Pawn> mothers = new List<Pawn>();
            List<Pawn> fathers = new List<Pawn>();
            CanFulfillParentType(partner1, ref mothers, ref fathers);
            CanFulfillParentType(partner2, ref mothers, ref fathers);

            /*if (CanFulfillParentType(partner1, Gender.Female)) mothers.Add(partner1);
            if (CanFulfillParentType(partner1, Gender.Male)) fathers.Add(partner1);

            if (CanFulfillParentType(partner2, Gender.Female)) mothers.Add(partner2);
            if (CanFulfillParentType(partner2, Gender.Male)) fathers.Add(partner2);*/

            // remove any pawns who are pregnant or lactating - the game will consider these sterile, and removing them earlier gives us a nicer error message.
            mothers.RemoveAll(i => i.health != null &&
                (i.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman) ||
                i.health.hediffSet.HasHediff(HediffDefOf.Lactating)));

            fathers.RemoveAll(i => i.health != null &&
                (i.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman) ||
                i.health.hediffSet.HasHediff(HediffDefOf.Lactating)));

            // dev mode on -> introduce logs
            if (Prefs.DevMode) Log.Message($"({nameof(HermaphroditeUtility)}) Mother candidates: {mothers.ToStringSafeEnumerable()}; Father candidates: {fathers.ToStringSafeEnumerable()}");

            // create parent pairs

            // no mom or dad candidates, or partner is the same -> empty parent pair
            if (partner1 == partner2 || mothers.NullOrEmpty() || fathers.NullOrEmpty()) return new ParentPair();
            // only one mom?
            else if (mothers.Count == 1) return new ParentPair { mother = mothers[0], father = fathers.FirstOrDefault(i => i != mothers[0]) };
            // only one dad?
            else if (fathers.Count == 1) return new ParentPair { mother = mothers.FirstOrDefault(i => i != fathers[0]), father = fathers[0] };
            else // roll the dice on who's the mom vs who's the dad
            {
                int motherIdx = Rand.Chance(0.5f) ? 0 : 1;
                int fatherIdx = motherIdx == 0 ? 1 : 0;
                return new ParentPair { mother = mothers[motherIdx], father = fathers[fatherIdx] };
            }

            /*if (partner1 == partner2 || mothers.Count == 0 || fathers.Count == 0)
            {
                return new ParentPair();
            }
            else if (mothers.Count == 1)
            {
                return new ParentPair { mother = mothers[0], father = fathers.FirstOrDefault(i => i != mothers[0]) }; // Only possible mother.
            }
            else if (fathers.Count == 1)
            {
                return new ParentPair { mother = mothers.FirstOrDefault(i => i != fathers[0]), father = fathers[0] }; // Only possible father.
            }
            else
            {
                int motherIdx = Rand.Chance(0.5f) ? 0 : 1;
                int fatherIdx = motherIdx == 0 ? 1 : 0;
                return new ParentPair { mother = mothers[motherIdx], father = fathers[fatherIdx] };
            }*/
        }

        public static ParentPair SelectParentsDriver(JobDriver_Lovin driver)
        {
            //Traverse traverse = new Traverse(driver);
            // Reworked using HarmonyLib
            //Pawn partner = new Traverse(driver).Method("get_TargetPawnA").GetValue<Pawn>();
            Pawn partner = new Traverse(driver).Method("get_Partner").GetValue<Pawn>();
            return SelectParents(driver.pawn, partner);

            /*//Log.Message("Entering SelectParents(driver)");
            // Reflection black magic
            var type = typeof(JobDriver_Lovin);
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            // This get the thing in Target.A as a pawn. our partner
            // The "Partner" member of JobDriver_Lovin isn't a method or a field
            // get_TargetPawnA does the exact same thing.
            MethodInfo TargetMethodInfo = methods.Single(f => f.Name == "get_TargetPawnA");
            // invoke the getter on our driver with no args
            Pawn partner = (Pawn)TargetMethodInfo.Invoke(driver, null);

            return Helpers.SelectParents(driver.pawn, partner);*/
        }

        public static IEnumerable<Pawn> GetAllParentsOfGender(Pawn pawn, Gender gender)
        {
            if(!pawn.RaceProps.IsFlesh || pawn.relations is null) 
                return Enumerable.Empty<Pawn>();

            return pawn.relations.DirectRelations
                    .Where(i => i.def == PawnRelationDefOf.Parent && i.otherPawn.gender == gender)
                    .Select(i => i.otherPawn);

            /*if (pawn.RaceProps.IsFlesh && pawn.relations != null)
            {
                return pawn.relations.DirectRelations
                    .Where(i => i.def == PawnRelationDefOf.Parent && i.otherPawn.gender == gender)
                    .Select(i => i.otherPawn);
            }

            return Enumerable.Empty<Pawn>();*/
        }

        public static Gender? SelectGender(Gender? from_recipe, Pawn pawn)
        {
            return pawn.genes != null && pawn.genes.HasGene(DefOfs.YaomaCore_Hermaphroditism) ? pawn.gender : from_recipe;
        }

        public static bool CanFertilize(Pawn pawn)
        {
            return CanFulfillParentType(pawn, Gender.Male);
        }

        public static bool CanImplant(Pawn pawn)
        {
            return CanFulfillParentType(pawn, Gender.Female);
        }

        /*public static AcceptanceReport CanEverProduceChild_Helper(Pawn first, Pawn second)
        {
            if (first.Dead) return "PawnIsDead".Translate(first.Named("PAWN"));
            if (second.Dead) return "PawnIsDead".Translate(second.Named("PAWN"));

            *//*if (first.gender == second.gender)
            {
                return "PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();
            }*//*

            if (!HermaphroditeUtility.ArePawnsCompatible(first, second)) return "PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();

            HermaphroditeUtility.ParentPair pair = HermaphroditeUtility.SelectParents(first, second);
            // handle the following case - null cases OR father is fine, mother is pregnant or lactating
            if (pair.mother is null || pair.father is null) return HermaphroditeUtility.BuildPregnancyFailure(pair.mother, pair.father);

            *//*Pawn pawn = (first.gender == Gender.Male) ? first : second;
            Pawn pawn2 = (first.gender == Gender.Female) ? first : second;*//*

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
        }*/

        /*protected static IEnumerable<Toil> HelperPrefixMakeNewToils_Lovin(JobDriver_Lovin driver)
        {
            Traverse traverse = new Traverse(driver);

            TargetIndex bedIndex = traverse.Field("BedInd").GetValue<TargetIndex>();
            TargetIndex partnerIndex = traverse.Field("PartnerInd").GetValue<TargetIndex>();
            Pawn partner = traverse.Method("get_Partner").GetValue<Pawn>();
            Building_Bed bed = traverse.Method("get_Bed").GetValue<Building_Bed>();

            driver.FailOnDespawnedOrNull(bedIndex);
            driver.FailOnDespawnedOrNull(partnerIndex);
            driver.FailOn(() => !partner.health.capacities.CanBeAwake);
            driver.KeepLyingDown(bedIndex);
            yield return Toils_Bed.ClaimBedIfNonMedical(bedIndex, TargetIndex.None);
            yield return Toils_Bed.GotoBed(bedIndex);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate ()
            {
                if (partner.CurJob == null || partner.CurJob.def != JobDefOf.Lovin)
                {
                    Job newJob = JobMaker.MakeJob(JobDefOf.Lovin, driver.pawn, bed);
                    partner.jobs.StartJob(newJob, JobCondition.InterruptForced, null, false, true, null, null, false, false, null, false, true, false);
                    driver.ticksLeft = (int)(2500f * Mathf.Clamp(Rand.Range(0.1f, 1.1f), 0.1f, 2f));
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, this.pawn.Named(HistoryEventArgsNames.Doer)), true);
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(this.pawn, this.Partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(this.pawn, this.Partner) && (PawnUtility.ShouldSendNotificationAbout(this.pawn) || PawnUtility.ShouldSendNotificationAbout(this.Partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(this.pawn.Named("BONDPAWN"), this.Partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(new TargetInfo[]
                        {
                            this.pawn,
                            this.Partner
                        }), null, null, null, null, 0, true);
                        return;
                    }
                }
                else
                {
                    this.ticksLeft = 9999999;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            Toil toil2 = Toils_LayDown.LayDown(this.BedInd, true, false, false, false, PawnPosture.LayingOnGroundNormal, false);
            toil2.FailOn(() => this.Partner.CurJob == null || this.Partner.CurJob.def != JobDefOf.Lovin);
            toil2.AddPreTickAction(delegate
            {
                this.ticksLeft--;
                if (this.ticksLeft <= 0)
                {
                    base.ReadyForNextToil();
                    return;
                }
                if (this.pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowMetaIcon(this.pawn.Position, this.pawn.Map, FleckDefOf.Heart, 0.42f);
                }
            });
            toil2.AddFinishAction(delegate
            {
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                if (this.pawn.health != null && this.pawn.health.hediffSet != null)
                {
                    if (this.pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer))
                    {
                        goto IL_C4;
                    }
                }
                if (this.Partner.health == null || this.Partner.health.hediffSet == null)
                {
                    goto IL_CF;
                }
                if (!this.Partner.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer))
                {
                    goto IL_CF;
                }
            IL_C4:
                thought_Memory.moodPowerFactor = 1.5f;
            IL_CF:
                if (this.pawn.needs.mood != null)
                {
                    this.pawn.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, this.Partner);
                }
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, this.pawn.Named(HistoryEventArgsNames.Doer)), true);
                HistoryEventDef def = this.pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, this.Partner) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse;
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, this.pawn.Named(HistoryEventArgsNames.Doer)), true);
                this.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + this.GenerateRandomMinTicksToNextLovin(this.pawn);
                if (ModsConfig.BiotechActive)
                {
                    Pawn pawn = (this.pawn.gender == Gender.Male) ? this.pawn : ((this.Partner.gender == Gender.Male) ? this.Partner : null);
                    Pawn pawn2 = (this.pawn.gender == Gender.Female) ? this.pawn : ((this.Partner.gender == Gender.Female) ? this.Partner : null);
                    if (pawn != null && pawn2 != null && Rand.Chance(JobDriver_Lovin.PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                    {
                        bool flag;
                        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(pawn, pawn2, out flag);
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
            toil2.socialMode = RandomSocialMode.Off;
            yield return toil2;
            yield break;
        }*/



        /*public static bool HelperPrefixInRelation(Pawn me, Pawn other, ref bool __result)
        {
            __result = me != other && (
                GetAllParentsOfGender(other, Gender.Female).Contains(me) ||
                GetAllParentsOfGender(other, Gender.Male).Contains(me) ||
                (ModsConfig.BiotechActive && other.GetBirthParent() == me));

            return false;
        }*/

        /*public static IEnumerable<CodeInstruction> HelperTranspilerAvailableOnNow(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                // Scan until we find a load to genderPrerequisite.
                if (!il[i].LoadsField(_field_gender_prerequisite)) continue;

                // Right after, we insert a call to our own function.
                il.Insert(++i, new CodeInstruction(OpCodes.Ldarg_1));
                il.Insert(++i, new CodeInstruction(OpCodes.Call, _method_select_gender));

                return il.AsEnumerable();
            }

            throw new Exception("Could not locate the correct instruction to patch - a mod incompatibility or game update broke it.");
        }*/

        /*public static void HelperPostfixInbredChanceFromParents(Pawn mother, Pawn father, ref PawnRelationDef relation, ref float __result)
        {
            if (mother != null && father != null && mother == father)
            {
                relation = PawnRelationDefOf.Sibling;
                __result = 1.0f;
            }
        }*/

        /*public static IEnumerable<CodeInstruction> HelperTransplierCanFertilizeReport(IEnumerable<CodeInstruction> instructions)
        {
            return ModifyIL(instructions, _method_can_fertilize);
        }

        public static IEnumerable<CodeInstruction> HelperTransplierCanImplantReport(IEnumerable<CodeInstruction> instructions)
        {
            return ModifyIL(instructions, _method_can_implant);
        }

        public static IEnumerable<CodeInstruction> ModifyIL(IEnumerable<CodeInstruction> instructions, MethodInfo pred)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                // Scan until we find a load to gender.
                if (!il[i].LoadsField(_field_gender)) continue;

                // Delete the ldfld, and the load to a gender constant.
                il.RemoveRange(i, 2);

                // Insert a call to our function to replace the above two.
                il.Insert(i, new CodeInstruction(OpCodes.Call, pred));

                // Replace the compare with a branch-if-true.
                il[i + 1].opcode = OpCodes.Brtrue_S;

                return il.AsEnumerable();
            }

            throw new Exception("Could not locate the correct instruction to patch - a mod incompatibility or game update broke it.");
        }*/


    }
}
