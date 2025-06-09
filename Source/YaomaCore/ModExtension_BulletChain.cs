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
    public class ModExtension_BulletChain : DefModExtension
    {
        public int maxChainLength = 3;

        public float maxChainRange = 100f;

        public bool limitedByMaxRange = true;

        public float chainDeviateDegrees = 30f;

        public bool lineOfSight = true;

        public bool sourceLOSIsBullet = true;

        public bool targetHostileOnly = true;

        public bool useClosestTargetForChain = true;

        public bool chainStoppedByArmorDeflect = true;

        public bool chainStoppedByShield = true;

        //public bool chainAffectedByArmorDiminish = false;

        public float DeviateDegrees => Mathf.Clamp(chainDeviateDegrees, 0f, 180f);

    }
}
