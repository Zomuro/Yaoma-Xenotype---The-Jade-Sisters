using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public class PawnCache
    {
        public Pawn pawn;

        public float healthScaleFactor = 1f;

        public int cacheTick;

        public int cacheTickInterval = 240;

        public PawnCache(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public bool Stale => Find.TickManager.TicksGame > cacheTick + cacheTickInterval;

        public void CacheHealthFactor()
        {
            healthScaleFactor = pawn.GetStatValue(DefOfs.YaomaXenotype_HealthScaleFactor);
        }

        public void ResetCache()
        {
            CacheHealthFactor();
            cacheTick = Find.TickManager.TicksGame;
        }

    }
}
