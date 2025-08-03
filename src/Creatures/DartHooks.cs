using RWCustom;
using UnityEngine;

namespace VoidTemplate;

    internal class DartHooks
    {
        public static void Apply()
        {
           
            On.BigSpider.Spit += PoisonSpit; 
            On.BigSpider.FlyingWeapon += PoisonFlyingWeapon;
            On.AbstractPhysicalObject.Realize += PoisonDart;
        }

        private static void PoisonSpit(On.BigSpider.orig_Spit orig, BigSpider self)
        {
            if (self.Template.type != CreatureTemplateType.Dartspider)
            {
            orig(self);
            }
            if (self.Template.type == CreatureTemplateType.Dartspider)
            {
            Vector2 vector = self.AI.spitModule.aimDir;
            if (self.safariControlled)
            {
                vector = ((!self.inputWithDiagonals.HasValue || !self.inputWithDiagonals.Value.AnyDirectionalInput) ? self.travelDir.normalized : new Vector2(self.inputWithDiagonals.Value.x, self.inputWithDiagonals.Value.y).normalized);
                Creature creature = null;
                float num = float.MaxValue;
                float current = Custom.VecToDeg(vector);
                for (int i = 0; i < self.abstractCreature.Room.creatures.Count; i++)
                {
                    if (self.abstractCreature != self.abstractCreature.Room.creatures[i] && self.abstractCreature.Room.creatures[i].realizedCreature != null)
                    {
                        float target = Custom.AimFromOneVectorToAnother(self.mainBodyChunk.pos, self.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                        float num2 = Custom.Dist(self.mainBodyChunk.pos, self.abstractCreature.Room.creatures[i].realizedCreature.mainBodyChunk.pos);
                        if (Mathf.Abs(Mathf.DeltaAngle(current, target)) < 22.5f && num2 < num)
                        {
                            num = num2;
                            creature = self.abstractCreature.Room.creatures[i].realizedCreature;
                        }
                    }
                }

                if (creature != null)
                {
                    vector = Custom.DirVec(self.mainBodyChunk.pos, creature.mainBodyChunk.pos);
                }
            }

            self.charging = 0f;
            self.mainBodyChunk.pos += vector * 12f;
            self.mainBodyChunk.vel += vector * 2f;
            AbstractPhysicalObject obj = new AbstractPhysicalObject(self.room.world, CreatureTemplateType.DartPoison, null, self.abstractCreature.pos, self.room.game.GetNewID());
            obj.RealizeInRoom();
            (obj.realizedObject as DartPoison).Shoot(self.mainBodyChunk.pos, vector, self);
            self.room.PlaySound(SoundID.Big_Spider_Spit, self.mainBodyChunk);
            self.AI.spitModule.SpiderHasSpit();
        }

        }

        private static void PoisonFlyingWeapon(On.BigSpider.orig_FlyingWeapon orig, BigSpider self, Weapon weapon)
        {
            if (self.Template.type != CreatureTemplateType.Dartspider)
            {
                orig(self, weapon);
            }
            if (self.Template.type == CreatureTemplateType.Dartspider)
            {
            if (!self.Consious || self.safariControlled || self.jumpStamina < 0.3f || self.room.GetTile(self.room.GetTilePosition(self.mainBodyChunk.pos) + new IntVector2(0, 1)).Solid || Custom.DistLess(self.mainBodyChunk.pos, weapon.thrownPos, 60f) || (!self.room.GetTile(self.room.GetTilePosition(self.mainBodyChunk.pos) + new IntVector2(0, -1)).Solid && self.room.GetTile(self.room.GetTilePosition(self.mainBodyChunk.pos) + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Floor && !self.room.GetTile(self.room.GetTilePosition(self.mainBodyChunk.pos)).AnyBeam) || self.grasps[0] != null || Vector2.Dot((self.bodyChunks[1].pos - self.bodyChunks[0].pos).normalized, (self.bodyChunks[0].pos - weapon.firstChunk.pos).normalized) < -0.2f || !self.AI.VisualContact(weapon.firstChunk.pos, 0.3f))
            {
                return;
            }
            if (Custom.DistLess(weapon.firstChunk.pos + weapon.firstChunk.vel.normalized * 140f, self.mainBodyChunk.pos, 140f) && (Mathf.Abs(Custom.DistanceToLine(self.bodyChunks[0].pos, weapon.firstChunk.pos, weapon.firstChunk.pos + weapon.firstChunk.vel)) < 7f || Mathf.Abs(Custom.DistanceToLine(self.bodyChunks[1].pos, weapon.firstChunk.pos, weapon.firstChunk.pos + weapon.firstChunk.vel)) < 7f))
            {
                self.Jump(Custom.DirVec(self.mainBodyChunk.pos, weapon.thrownPos + new Vector2(0f, 400f)), 1f);
                BodyChunk bodyChunk = self.bodyChunks[0];
                bodyChunk.pos.y = bodyChunk.pos.y + 20f;
                BodyChunk bodyChunk2 = self.bodyChunks[1];
                bodyChunk2.pos.y = bodyChunk2.pos.y + 10f;
                self.jumpStamina = Mathf.Max(0f, self.jumpStamina - 0.15f);
            }
            }

        }
        private static void PoisonDart(On.AbstractPhysicalObject.orig_Realize orig, global::AbstractPhysicalObject self)
        {
            orig(self);
            if (self.type == CreatureTemplateType.DartPoison && self.realizedObject == null)
            {
                self.realizedObject = new DartPoison(self);
            }


        }

    }



