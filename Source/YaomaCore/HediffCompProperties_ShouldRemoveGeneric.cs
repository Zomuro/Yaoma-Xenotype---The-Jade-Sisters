using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public class HediffCompProperties_ShouldRemoveGeneric : HediffCompProperties
    {
        public HediffCompProperties_ShouldRemoveGeneric()
        {
            compClass = typeof(HediffComp_ShouldRemoveGeneric);
        }

        public bool fleshOnly = true;

        public List<PawnCapacityDef> capacities;
    }
}
