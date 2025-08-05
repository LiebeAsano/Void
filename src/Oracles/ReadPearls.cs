using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Oracles;

public static class ReadPearls
{
    public static void Hook()
    {
        On.SSOracleBehavior.Update += SSOralceBehavior_Update;
    }

    private static void SSOralceBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        orig(self, eu);
        if (ModManager.MSC)
        {
            if ((self.oracle.ID == MoreSlugcatsEnums.OracleID.DM || (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.Void)) && self.player != null && self.player.room == self.oracle.room)
            {
                List<PhysicalObject>[] physicalObjects = self.oracle.room.physicalObjects;
                for (int num6 = 0; num6 < physicalObjects.Length; num6++)
                {
                    for (int num7 = 0; num7 < physicalObjects[num6].Count; num7++)
                    {
                        PhysicalObject physicalObject = physicalObjects[num6][num7];
                        if (physicalObject is Weapon && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(weapon.firstChunk.pos, self.oracle.firstChunk.pos) < 100f)
                            {
                                weapon.ChangeMode(Weapon.Mode.Free);
                                weapon.SetRandomSpin();
                                weapon.firstChunk.vel *= -0.2f;
                                for (int num8 = 0; num8 < 5; num8++)
                                {
                                    self.oracle.room.AddObject(new Spark(weapon.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
                                }
                                self.oracle.room.AddObject(new Explosion.ExplosionLight(weapon.firstChunk.pos, 150f, 1f, 8, Color.white));
                                self.oracle.room.AddObject(new ShockWave(weapon.firstChunk.pos, 60f, 0.1f, 8, false));
                                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, weapon.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
                            }
                        }
                        bool flag3 = false;
                        bool flag4 = (self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty || self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty || self.action == SSOracleBehavior.Action.General_Idle) && self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior && (self.currSubBehavior as SSOracleBehavior.SSSleepoverBehavior).panicObject == null;
                        if (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.Void && self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior)
                        {
                            flag4 = true;
                            flag3 = true;
                        }
                        if (self.inspectPearl == null
                            && (self.conversation == null || flag3)
                            && physicalObject is DataPearl
                            && (physicalObject as DataPearl).grabbedBy.Count == 0
                            && (physicalObject as DataPearl).AbstractPearl != VoidPearl(self.oracle.room)
                            && (physicalObject as DataPearl).AbstractPearl != RotPearl(self.oracle.room)
                            && ((physicalObject as DataPearl).AbstractPearl.dataPearlType != DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl
                            || (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM
                            && ((physicalObject as DataPearl).AbstractPearl as PebblesPearl.AbstractPebblesPearl).color >= 0))
                            && !self.readDataPearlOrbits.Contains((physicalObject as DataPearl).AbstractPearl)
                            && flag4 && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark
                            && !self.talkedAboutThisSession.Contains(physicalObject.abstractPhysicalObject.ID))
                        {
                            self.inspectPearl = (physicalObject as DataPearl);
                            if (!(self.inspectPearl is SpearMasterPearl) || !(self.inspectPearl.AbstractPearl as SpearMasterPearl.AbstractSpearMasterPearl).broadcastTagged)
                            {
                                Custom.Log(
                                [
                                    string.Format("---------- INSPECT PEARL TRIGGERED: {0}", self.inspectPearl.AbstractPearl.dataPearlType)
                                ]);
                                if (self.inspectPearl is SpearMasterPearl)
                                {
                                    self.LockShortcuts();
                                    if (self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.pos.y > 600f)
                                    {
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.Stun(40);
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.vel = new Vector2(0f, -4f);
                                    }
                                    self.getToWorking = 0.5f;
                                    self.SetNewDestination(new Vector2(600f, 450f));
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                self.inspectPearl = null;
                            }
                        }
                    }
                }
            }
            if (self.oracle.room.world.name == "HR")
            {
                int num9 = 0;
                if (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                {
                    num9 = 2;
                }
                float num10 = Custom.Dist(self.oracle.arm.cornerPositions[0], self.oracle.arm.cornerPositions[2]) * 0.4f;
                if (Custom.Dist(self.baseIdeal, self.oracle.arm.cornerPositions[num9]) >= num10)
                {
                    self.baseIdeal = self.oracle.arm.cornerPositions[num9] + (self.baseIdeal - self.oracle.arm.cornerPositions[num9]).normalized * num10;
                }
            }
            if (self.currSubBehavior.LowGravity >= 0f)
            {
                self.oracle.room.gravity = self.currSubBehavior.LowGravity;
                return;
            }
        }
    }
}
