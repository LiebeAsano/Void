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

        public Phases phase = Phases.None;

        public int shakeScreenTimer = -1;

        public bool[] blockedShortcuts;

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

            blockedShortcuts ??= new bool[room.shortcuts.Length];

            if (!room.BeingViewed && phase != Phases.CellIncerted)
            {
                if (phase != Phases.None)
                {
                    for (int i = 0; i < blockedShortcuts.Length; i++)
                    {
                        if (blockedShortcuts[i])
                        {
                            UnlockShortcut(i);
                        }
                    }
                    phase = Phases.None;
                }
                return;
            }
            else if (phase == Phases.None)
            {
                phase = Phases.Viewed;
            }
            UpdateShortcutGraphics();

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
                            LockShortcut(2);
                            goto CELL_FOUND;
                        }
                    }
                }
                return;
            }
        CELL_FOUND:

            if (chargedCellInRoom.room != room || chargedCellInRoom.slatedForDeletetion)
            {
                phase = Phases.Viewed;
                UnlockShortcut(2);
                chargedCellInRoom = null;
                shakeScreenTimer = -1;
                return;
            }
            else if (phase == Phases.Viewed)
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
                chargedCellInRoom.firstChunk.vel += (MoveToPos - chargedCellInRoom.firstChunk.pos).normalized;
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
                chargedCellInRoom.counter = 0.25f;
                if (shakeScreenTimer > -1)
                {
                    shakeScreenTimer++;
                    if (shakeScreenTimer >= 100)
                    {
                        room.ScreenMovement(null, default, 0.8f);
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
                blockedShortcuts[shortcutIndex] = true;
            }
        }

        public void UnlockShortcut(int shortcutIndex)
        {
            if (room.lockedShortcuts.Remove(room.shortcutsIndex[shortcutIndex]))
            {
                blockedShortcuts[shortcutIndex] = false;
            }
        }

        public void UpdateShortcutGraphics()
        {
            if (room.BeingViewed)
            {
                for (int cam = 0; cam < room.game.cameras.Length; cam++)
                {
                    if (room.game.cameras[cam].room == room)
                    {
                        var sGraphics = room.game.cameras[cam].shortcutGraphics;
                        for (int i = 0; i < blockedShortcuts.Length; i++)
                        {
                            if (sGraphics.entranceSprites.Length > i && sGraphics.entranceSprites[i, 0] != null)
                            {
                                sGraphics.entranceSprites[i, 0].isVisible = !blockedShortcuts[i];
                            }
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
            None = 0,
            Viewed,
            Suction,
            CellIncerted
        }
    }
}
