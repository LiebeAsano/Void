using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;
using static Pom.Pom;

namespace VoidTemplate.Objects.PomObjects
{
    public class ShortcutBlockMark : UpdatableAndDeletable
    {
        PlacedObject pObj;

        public IntVector2 ShortcutPos { get => room.GetTilePosition(pObj.pos); }

        public ShortcutBlockMark(PlacedObject pObj)
        {
            this.pObj = pObj;
        }

        public override void Update(bool eu)
        {
            if (!room.shortCutsReady || slatedForDeletetion)
            {
                return;
            }
            var shortcut = room.shortcutData(ShortcutPos);
            if (shortcut.shortCutType == ShortcutData.Type.RoomExit)
            {
                if (!room.world.TryGetShortcutBlock(out var block))
                {
                    block = new();
                    room.world.CreateShortcutBlock(block);
                    block.blockedShortcuts.Add(new(room.abstractRoom, shortcut.destNode));
                }
                else
                {
                    bool legalBlock = true;
                    for (int i = 0; i < block.blockedShortcuts.Count; i++)
                    {
                        if (block.blockedShortcuts[i].CompareRoomAndNode(room.abstractRoom, shortcut.destNode))
                        {
                            legalBlock = false;
                            break;
                        }
                    }
                    if (legalBlock)
                    {
                        block.blockedShortcuts.Add(new(room.abstractRoom, shortcut.destNode));
                    }
                }
            }
            else if (room.GetTile(ShortcutPos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
            {
                Utils.LogExInf($"Room: {room.abstractRoom.name}. Impossible to block shortcut! Wrong type of shortcut: {shortcut.shortCutType}!");
            }
            else
            {
                Utils.LogExInf($"Room: {room.abstractRoom.name}. Impossible to block shortcut! Shortcut unfound! Tile type: {room.GetTile(ShortcutPos).Terrain}");
            }
            Destroy();
        }

        public static void Register()
        {
            RegisterFullyManagedObjectType(null, typeof(ShortcutBlockMark), "Shortcut Block Mark", "The Void");
        }
    }
}
