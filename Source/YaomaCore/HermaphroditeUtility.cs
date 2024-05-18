using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore.HarmonyPatches
{
    // This code is based off of
    public static class HermaphroditeUtility
    {
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
            if (partner1 == partner2 || mothers.NullOrEmpty() || fathers.NullOrEmpty()) // no mom or dad candidates, or partner is the same -> empty parent pair
            {
                return new ParentPair();
            }
            else if (mothers.Count == 1) // only one mom?
            {
                return new ParentPair { mother = mothers[0], father = fathers.FirstOrDefault(i => i != mothers[0]) };
            }
            else if (fathers.Count == 1) // only one dad?
            {
                return new ParentPair { mother = mothers.FirstOrDefault(i => i != fathers[0]), father = fathers[0] };
            }
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

        public static AcceptanceReport HelperPrefixCanEverProduceChild(Pawn first, Pawn second)
        {
            if (first.Dead) return "PawnIsDead".Translate(first.Named("PAWN"));
            if (second.Dead) return "PawnIsDead".Translate(second.Named("PAWN"));

            /*if (first.gender == second.gender)
            {
                return "PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();
            }*/

            if(!ArePawnsCompatible(first, second)) return "PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();

            ParentPair pair = SelectParents(first, second);
            // handle the following case - null cases OR father is fine, mother is pregnant or lactating
            if (pair.mother is null || pair.father is null) return BuildPregnancyFailure(pair.mother, pair.father);

            /*Pawn pawn = (first.gender == Gender.Male) ? first : second;
            Pawn pawn2 = (first.gender == Gender.Female) ? first : second;*/

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
    }
}
