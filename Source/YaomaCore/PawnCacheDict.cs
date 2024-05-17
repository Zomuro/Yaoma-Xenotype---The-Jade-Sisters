using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
    public static class PawnCacheDict
    {
        public static Dictionary<Pawn,PawnCache> cacheDict = new Dictionary<Pawn, PawnCache>();

        public static PawnCache RetrieveCache(Pawn pawn)
        {
            PawnCache pawnCache;
            if (!cacheDict.TryGetValue(pawn, out pawnCache))
            {
                pawnCache = new PawnCache(pawn);
                pawnCache.ResetCache();
                cacheDict[pawn] = pawnCache;
                return pawnCache;
            }

            if (pawnCache.Stale) pawnCache.ResetCache();
            return pawnCache;
        }
    }
}
