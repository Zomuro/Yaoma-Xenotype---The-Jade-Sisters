using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public class DamageWorker_Blunt_ScalingBodySize : DamageWorker_Blunt
    {
        protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageWorker.DamageResult result)
        {
            // scale total damage dealt with body size
            float totalDamageScaled = totalDamage * pawn.BodySize;
            base.ApplySpecialEffectsToPart(pawn, totalDamageScaled, dinfo, result);
        }
    }
}
