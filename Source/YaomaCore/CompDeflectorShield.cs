using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YaomaCore
{
    [StaticConstructorOnStartup]
    public class CompDeflectorShield : CompShield
    {
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (IsApparel) foreach (Gizmo gizmo in GetShieldGizmos()) yield return gizmo;
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Break",
                    action = delegate ()
                    {
                        Traverse traverse = new Traverse(this);
                        traverse.Method("Break", null).GetValue();
                    }
                };
                if (ticksToReset > 0)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: Clear reset",
                        action = delegate ()
                        {
                            ticksToReset = 0;
                        }
                    };
                }
            }
            yield break;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!IsApparel) foreach (Gizmo gizmo in GetShieldGizmos()) yield return gizmo;
            yield break;
        }

        public IEnumerable<Gizmo> GetShieldGizmos()
        {
            Pawn pawn;
            if ((PawnOwner.Faction == Faction.OfPlayer || ((pawn = (parent as Pawn)) != null && pawn.RaceProps.IsMechanoid)) && Find.Selector.SingleSelectedThing == PawnOwner)
            {
                yield return new Gizmo_DeflectorShieldStatus
                {
                    deflector = this
                };
            }
            yield break;
        }

    }
}
