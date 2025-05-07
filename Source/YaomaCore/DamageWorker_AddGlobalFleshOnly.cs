using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public class DamageWorker_AddGlobalFleshOnly : DamageWorker_AddGlobal
    {
        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn != null && pawn.RaceProps.IsFlesh) // only apply to pawns who are organic
            {
                Hediff hediff = HediffMaker.MakeHediff(dinfo.Def.hediff, pawn, null);
                hediff.Severity = dinfo.Amount;
                pawn.health.AddHediff(hediff, null, new DamageInfo?(dinfo), null);
            }
            return new DamageWorker.DamageResult();
        }
    }
}
