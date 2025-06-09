using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System;
using System.Collections.Generic;
using Verse.Noise;
using System.Linq;

namespace YaomaCore
{
    public class Bullet_Chain : Bullet
    {
        public int bulletChainLenLeft = -1;

        public float bulletRangeLeft = -1f;

        public Thing originalLauncher;

        public Vector3 originalOrigin;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref bulletChainLenLeft, "bulletChainLenLeft", 0, false);
            Scribe_Values.Look<float>(ref bulletRangeLeft, "bulletRangeLeft", 0, false);
            Scribe_References.Look<Thing>(ref originalLauncher, "originalLauncher");
            Scribe_Values.Look<Vector3>(ref originalOrigin, "originalOrigin", default(Vector3), false);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // basic bullet behavior adjusted to also retrieve damage results - contains armor absorb info
            Map map = base.Map;
            IntVec3 position = base.Position;
            bool projectileDeflected = false;

            if (originalLauncher is null) originalLauncher = launcher;

            // log that a ranged impact happened
            base.Impact(hitThing, blockedByShield);
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(originalLauncher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);

            // awkward traverse to run private method
            new Traverse(this).Method("NotifyImpact", hitThing, map, position).GetValue();

            if (hitThing != null)
            {
                Pawn pawn;
                bool instigatorGuilty = (pawn = (originalLauncher as Pawn)) == null || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, (float)DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, originalLauncher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty, true, QualityCategory.Normal, true);
                dinfo.SetWeaponQuality(equipmentQuality);
                DamageWorker.DamageResult primaryDamageResult = hitThing.TakeDamage(dinfo);
                primaryDamageResult.AssociateWithLog(battleLogEntry_RangedImpact);
                if (primaryDamageResult.deflected) projectileDeflected = true;

                Pawn hitPawn = hitThing as Pawn;
                if (hitPawn != null)
                {
                    Pawn_StanceTracker stances = hitPawn.stances;
                    stances?.stagger.Notify_BulletImpact(this);
                }
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfoExtra = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, originalLauncher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty, true, QualityCategory.Normal, true);

                            DamageWorker.DamageResult secondaryDamageResult = hitThing.TakeDamage(dinfoExtra);
                            secondaryDamageResult.AssociateWithLog(battleLogEntry_RangedImpact);
                            if (secondaryDamageResult.deflected) projectileDeflected = true;
                        }
                    }
                }
                if (Rand.Chance(def.projectile.bulletChanceToStartFire) && (hitPawn == null || Rand.Chance(FireUtility.ChanceToAttachFireFromEvent(hitPawn))))
                {
                    hitThing.TryAttachFire(def.projectile.bulletFireSizeRange.RandomInRange, originalLauncher);
                    //return;
                }
            }
            else
            {
                if (!blockedByShield)
                {
                    SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(position, map, false));
                    if (position.GetTerrain(map).takeSplashes) FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt((float)DamageAmount) * 1f, 4f);
                    else FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
                }
                if (Rand.Chance(def.projectile.bulletChanceToStartFire)) FireUtility.TryStartFireIn(position, map, def.projectile.bulletFireSizeRange.RandomInRange, originalLauncher, null);
            }

            ChainFromImpact(hitThing, map, projectileDeflected, blockedByShield);
        }

        public virtual void ChainFromImpact(Thing hitThing, Map map, bool deflectedByArmor = false, bool blockedByShield = false)
        {
            if (hitThing is null) return;

            // nullcheck for mod extension for further chain info
            ModExtension_BulletChain extensionInfo = def.GetModExtension<ModExtension_BulletChain>();
            if (extensionInfo is null) return;

            // initializes chain count start
            if (bulletChainLenLeft < 0 || bulletRangeLeft < 0)
            {
                bulletChainLenLeft = extensionInfo.maxChainLength;
                bulletRangeLeft = extensionInfo.maxChainRange;
                originalOrigin = origin;
            }

            // stops bullet chain if bullet is deflected or blocked by shield (depending on setting)
            if (extensionInfo.chainStoppedByArmorDeflect && deflectedByArmor) return;
            if (extensionInfo.chainStoppedByShield && blockedByShield) return;

            // calculate how much bullet range we have left to work with, if we want bullet range to be limited by max range
            if (extensionInfo.limitedByMaxRange)
            {
                bulletRangeLeft -= IntVec3Utility.DistanceTo(origin.ToIntVec3(), hitThing.Position);
                if (bulletRangeLeft <= 0) return;
            }

            // get list of all spawned pawns that fufill specific targeting conditions
            IEnumerable<Pawn> pawnsInRange = GetPawnsFufillCondition(map.mapPawns.AllPawnsSpawned, hitThing, map, extensionInfo);
            if (pawnsInRange.EnumerableNullOrEmpty()) return; // no pawns found? kill it

            // select pawn as new chain target (closest or random)
            Pawn closestPawn = extensionInfo.useClosestTargetForChain? pawnsInRange.OrderBy(x => Vector3.Distance(origin, x.Position.ToVector3())).FirstOrDefault() : pawnsInRange.RandomElementWithFallback();
            if (closestPawn is null) return;

            // launch a new chaining projectile
            Bullet_Chain newBulletChain = (Bullet_Chain)GenSpawn.Spawn(def, hitThing.Position, map, WipeMode.Vanish);
            newBulletChain.bulletChainLenLeft = bulletChainLenLeft - 1;
            newBulletChain.bulletRangeLeft = bulletRangeLeft;
            newBulletChain.originalLauncher = originalLauncher;
            newBulletChain.originalOrigin = origin;
            newBulletChain.Launch(hitThing, new LocalTargetInfo(closestPawn), new LocalTargetInfo(closestPawn), HitFlags, preventFriendlyFire);
        }

        public IEnumerable<Pawn> GetPawnsFufillCondition(IEnumerable<Pawn> pawns, Thing hitThing, Map map, ModExtension_BulletChain extensionInfo)
        {
            Pawn hitPawn = hitThing as Pawn;
            foreach (Pawn pawn in pawns)
            {
                if (pawn is null) continue; // nullcheck

                // discount this pawn if
                // (a) are within range of the bullet (b) are not the same location as the origin (c) fufill the quaternion angle requirement
                Vector3 directTargetVector = (pawn.Position - hitThing.Position).ToVector3().Yto0();
                if (IntVec3Utility.DistanceTo(hitThing.Position, pawn.Position) > bulletRangeLeft || 
                    (hitPawn != null && hitPawn == pawn) || 
                    Quaternion.Angle(ExactRotation, Quaternion.LookRotation(directTargetVector)) > extensionInfo.DeviateDegrees) continue;

                // line of sight check - don't get this pawn if they fail LOS
                if (extensionInfo.lineOfSight)
                {
                    bool checkLOS = extensionInfo.sourceLOSIsBullet ? GenSight.LineOfSight(hitThing.Position, pawn.Position, map) : 
                        GenSight.LineOfSight(originalOrigin.ToIntVec3(), pawn.Position, map);
                    if (!checkLOS) continue;
                }

                // hostility check - don't get this pawn if they fail the hostility check
                if (extensionInfo.targetHostileOnly && !pawn.HostileTo(originalLauncher)) continue;

                yield return pawn;
            }

            yield break;
        }


    }
}
