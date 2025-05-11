using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace YaomaCore
{
    [StaticConstructorOnStartup]
    public class Gizmo_DeflectorShieldStatus : Gizmo_EnergyShieldStatus
    {
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, deflector.IsApparel ? deflector.parent.LabelCap : "ShieldInbuilt".Translate().Resolve());
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            float fillPercent = deflector.Energy / Mathf.Max(1f, deflector.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax, true, -1));
            Widgets.FillableBar(rect4, fillPercent, FullShieldBarTex, EmptyShieldBarTex, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, (deflector.Energy * 100f).ToString("F0") + " / " + (deflector.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax, true, -1) * 100f).ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect2, "YaomaXenotype_DeflectorShieldTip".Translate());
            return new GizmoResult(GizmoState.Clear);
        }

        public CompDeflectorShield deflector;

        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));

        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

    }
}
