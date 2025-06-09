using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public class HediffComp_ShouldRemoveGeneric : HediffComp
    {
        public HediffCompProperties_ShouldRemoveGeneric Props => (HediffCompProperties_ShouldRemoveGeneric) props;

        public override bool CompShouldRemove
        {
            get
            {
                // if pawn raceprop flesh type is not flesh, set to remove hediff
                if (Props.fleshOnly && !Pawn.RaceProps.IsFlesh) return true;

                // if pawn ends up being incapable of a capacity, set for removal
                if (!Props.capacities.NullOrEmpty() && 
                    (from c in Props.capacities 
                     where !Pawn.health.capacities.CapableOf(c) 
                     select c).Count() > 0) 
                    return true;

                return base.CompShouldRemove;
            }
        }

    }
}
