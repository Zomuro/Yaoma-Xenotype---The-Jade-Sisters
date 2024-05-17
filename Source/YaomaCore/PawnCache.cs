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

        public void Recache()
        {

        }


    }
}
