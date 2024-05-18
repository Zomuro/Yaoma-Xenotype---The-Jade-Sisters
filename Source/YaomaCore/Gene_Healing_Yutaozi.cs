using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace YaomaCore
{
	public class Gene_Healing_Yutaozi : Gene_Healing
	{
		// Gene_Healing
		public int ticksToHeal;

		// unfortunately, the only way to configure this is to hardcode this range
		public static readonly IntRange HealingIntervalRange = new IntRange(60000 * 15, 60000 * 45);

		public override void Tick()
		{
			base.Tick();
			ticksToHeal--;
			if (ticksToHeal <= 0)
			{
				HediffComp_HealPermanentWounds.TryHealRandomPermanentWound(pawn, LabelCap);
				ResetInterval();
			}
		}

		private void ResetInterval()
		{
			ticksToHeal = HealingIntervalRange.RandomInRange;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref ticksToHeal, "ticksToHeal", 0, false);
		}
		
	}
}
