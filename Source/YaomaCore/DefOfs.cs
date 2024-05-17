using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
	[DefOf]
	public static class DefOfs
	{
		// GeneDef
		public static GeneDef YaomaCore_Hermaphroditism;

		// StatDef
		public static StatDef YaomaCore_HealthScaleFactor;

		static DefOfs()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(DefOfs));
		}
		
	}
}
