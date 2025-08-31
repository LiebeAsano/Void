using UnityEngine;
using VoidTemplate.Objects.SingularityRock;
using RWCustom;
using static Pom.Pom;

namespace VoidTemplate.Objects.PomObjects
{
    public class TrainCellTrigger : UpdatableAndDeletable
    {
        private const string TRIGGER = "Trigger";

        public MiniEnergyCell chargedCellInRoom;

        private PlacedObject pObj;

        private ManagedData data;

        public Phases phase = Phases.NotViewed;

        public int shakeScreenTimer = -1;

        public Vector2 TriggerZone => data.GetValue<Vector2>(TRIGGER);

        public Vector2 MoveToPos => room.MiddleOfTile(pObj.pos);

        public TrainCellTrigger(PlacedObject pObj)
        {
            this.pObj = pObj;
            data = pObj.data as ManagedData;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!room.fullyLoaded) return;

            if (!room.BeingViewed)
            {
                if (phase != Phases.NotViewed)
                {
                    room.lockedShortcuts.Clear();
                    phase = Phases.NotViewed;
                }
                return;
            }
            else if (phase == Phases.NotViewed)
            {
                phase = Phases.None;
            }

            if (phase != Phases.CellIncerted && !room.lockedShortcuts.Contains(room.shortcutsIndex[1]))
            {
                LockShortcut(1);
            }

            if (chargedCellInRoom == null)
            {
                for (int i = 0; i < room.physicalObjects.Length; i++)
                {
                    for (int j = 0; j < room.physicalObjects[i].Count; j++)
                    {
                        if (room.physicalObjects[i][j] is MiniEnergyCell cell && cell.Charged)
                        {
                            chargedCellInRoom = cell;
                            LockShortcut(0);
                            goto CELL_FOUND;
                        }
                    }
                }
                return;
            }
        CELL_FOUND:

            if (chargedCellInRoom.room != room || chargedCellInRoom.slatedForDeletetion)
            {
                phase = Phases.None;
                UnlockShortcut(0);
                chargedCellInRoom = null;
                shakeScreenTimer = -1;
                return;
            }
            else if (phase == Phases.None)
            {
                if (!Trigger())
                {
                    return;
                }
                phase = Phases.Suction;
                chargedCellInRoom.AllGraspsLetGoOfThisObject(true);
                chargedCellInRoom.gravity = 0;
            }

            if ((int)phase > 1)
            {
                chargedCellInRoom.Forbid();
            }

            if (phase == Phases.Suction)
            {
                chargedCellInRoom.CollideWithObjects = false;
                chargedCellInRoom.firstChunk.vel += Vector2.ClampMagnitude(MoveToPos - chargedCellInRoom.firstChunk.pos, 40) / 40f;
                if (Custom.DistLess(chargedCellInRoom.firstChunk.pos, MoveToPos, 20))
                {
                    phase = Phases.CellIncerted;
                    chargedCellInRoom.CollideWithObjects = true;
                    chargedCellInRoom.canBeHitByWeapons = false;
                    UnlockShortcut(1);
                    shakeScreenTimer = 0;
                }
            }

            if (phase == Phases.CellIncerted)
            {
                chargedCellInRoom.firstChunk.pos = MoveToPos;
                if (shakeScreenTimer > -1)
                {
                    shakeScreenTimer++;
                    if (shakeScreenTimer >= 100)
                    {
                        room.ScreenMovement(MoveToPos, default, 1.3f);
                        shakeScreenTimer = -1;
                    }
                }
            }

            bool Trigger()
            {
                return (chargedCellInRoom.firstChunk.pos - pObj.pos).sqrMagnitude <= TriggerZone.sqrMagnitude;
            }
        }

        public void LockShortcut(int shortcutIndex)
        {
            if (!room.lockedShortcuts.Contains(room.shortcutsIndex[shortcutIndex]))
            {
                room.lockedShortcuts.Add(room.shortcutsIndex[shortcutIndex]);
                for (int i = 0; i < room.game.cameras.Length; i++)
                {
                    if (room.game.cameras[i].room == room)
                    {
                        var sGraphics = room.game.cameras[i].shortcutGraphics;
                        if (sGraphics.entranceSprites.Length > shortcutIndex && sGraphics.entranceSprites[shortcutIndex, 0] != null)
                        {
                            sGraphics.entranceSprites[shortcutIndex, 0].isVisible = false;
                        }
                    }
                }
            }
        }

        public void UnlockShortcut(int shortcutIndex)
        {
            if (room.lockedShortcuts.Remove(room.shortcutsIndex[shortcutIndex]))
            {
                for (int i = 0; i < room.game.cameras.Length; i++)
                {
                    if (room.game.cameras[i].room == room)
                    {
                        var sGraphics = room.game.cameras[i].shortcutGraphics;
                        if (sGraphics.entranceSprites.Length > shortcutIndex && sGraphics.entranceSprites[shortcutIndex, 0] != null)
                        {
                            sGraphics.entranceSprites[shortcutIndex, 0].isVisible = true;
                        }
                    }
                }
            }
        }

        public static void Register()
        {
            RegisterFullyManagedObjectType([new Vector2Field(TRIGGER, new(100, 0), Vector2Field.VectorReprType.circle)], typeof(TrainCellTrigger), "Train Cell Trigger", "The Void");
        }

        public enum Phases
        {
            NotViewed = 0,
            None,
            Suction,
            CellIncerted
        }
    }
}
