using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Objects.SingularityRock;

namespace VoidTemplate.Objects.PomObjects
{
    public class MiniEnergyCellSpawner : UpdatableAndDeletable, INotifyWhenRoomIsReady
    {
        public PlacedObject pObj;

        public MiniEnergyCellSpawner(Room room, PlacedObject pObj)
        {
            this.pObj = pObj;
            if (room.game.session is not StoryGameSession || room.game.rainWorld.ExpeditionMode || room.game.rainWorld.safariMode)
            {
                slatedForDeletetion = true;
                return;
            }
            for (int i = 0; i < room.game.Players.Count; i++)
            {
                if ((room.game.Players[i].realizedCreature as Player).objectInStomach is MiniEnergyCellAbstract)
                {
                    slatedForDeletetion = true;
                    return;
                }
            }
            for (int i = 0; i < room.world.abstractRooms.Length; i++)
            {
                for (int j = 0; j < room.world.abstractRooms[i].entities.Count; j++)
                {
                    if (room.world.abstractRooms[i].entities[j] is MiniEnergyCellAbstract)
                    {
                        slatedForDeletetion = true;
                        return;
                    }
                }
            }
        }

        public static void Register()
        {
            Pom.Pom.RegisterFullyManagedObjectType(null, typeof(MiniEnergyCellSpawner), "Mini Energy Cell Spawner", "The Void");
        }

        public void AIMapReady()
        {
            if (!slatedForDeletetion)
            {
                MiniEnergyCellAbstract cell = new(room.world, room.GetWorldCoordinate(pObj.pos), room.game.GetNewID());
                room.abstractRoom.AddEntity(cell);
                cell.RealizeInRoom();
                Destroy();
            }
        }

        public void ShortcutsReady()
        {
            
        }
    }
}
