using SlugBase.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Pom.Pom;

namespace VoidTemplate.Objects.PomObjects
{
    internal class AlbinoVultureTriggerSpawner : UpdatableAndDeletable
    {
        private class SpawnedVultureData(bool isAlbino = false)
        {
            public bool IsAlbino { get; } = isAlbino;
        }

        private static ConditionalWeakTable<AbstractCreature, SpawnedVultureData> SpawnedVultureDataCWT = new();
        //private static Dictionary<string, bool> VultureSpawnedThisCycleByRooms { get; } = new Dictionary<string, bool>();

        private const string TRIGGER_AREA_DATA_KEY = "Trigger Area";
        private const string SPAWN_POINT_DATA_KEY = "Spawn Point";

        private const string SAVEDATA_TRIGGER_WAS_FIRED_KEY = "TheVoid:AlbinoVultureTriggerWasFired";

        internal static ManagedField[] managedFields = [
            new Vector2Field(TRIGGER_AREA_DATA_KEY, new(100, 0), Vector2Field.VectorReprType.circle),
            new Vector2Field(SPAWN_POINT_DATA_KEY, new(0, 200), Vector2Field.VectorReprType.line)
            ];

        private ManagedData ManagedData { get; }
        private PlacedObject PlacedObject { get; }

        private bool IsActive { get; set; }

        public Vector2 TriggerAreaRadiusEndpoint => ManagedData.GetValue<Vector2>(TRIGGER_AREA_DATA_KEY);
        public Vector2 SpawnPoint => ManagedData.GetValue<Vector2>(SPAWN_POINT_DATA_KEY);

        public AlbinoVultureTriggerSpawner(Room room, PlacedObject pObj)
        {
            IsActive = false;

            ManagedData = (ManagedData)pObj.data;
            PlacedObject = pObj;

            if (room.game.session is StoryGameSession storyGameSession)
            {
                SlugBaseSaveData slugBaseSaveData = storyGameSession.saveState.miscWorldSaveData.GetSlugBaseData();

                if (slugBaseSaveData.TryGet(SAVEDATA_TRIGGER_WAS_FIRED_KEY, out bool triggerWasFired))
                {
                    IsActive = !triggerWasFired;
                }
                else
                {
                    IsActive = true;
                }
            }
        }

        public static void Register()
        {
            RegisterFullyManagedObjectType(managedFields, typeof(AlbinoVultureTriggerSpawner), "Albino Vulture Spawner", "The Void");
            On.VultureGraphics.ctor += VultureGraphics_ctor;
        }

        private static void VultureGraphics_ctor(On.VultureGraphics.orig_ctor orig, VultureGraphics self, Vulture ow)
        {
            orig(self, ow);

            if (SpawnedVultureDataCWT.TryGetValue(ow.abstractCreature, out SpawnedVultureData spawnedVultureData))
            {
                self.albino = spawnedVultureData.IsAlbino;
            }
        }

        public override void Update(bool eu)
        {
            if (!IsActive)
            {
                return;
            }
            
            foreach (Creature alivePlayerCreature in room.game.AlivePlayers.
                Select(crit => crit.realizedCreature))
            {
                foreach(BodyChunk bodyChunk in alivePlayerCreature.bodyChunks)
                {
                    if (IsRoomPointInsideTrigger(bodyChunk.pos))
                    {
                        SpawnVulture();
                        return;
                    }
                }
            }
        }

        private void SpawnVulture()
        {
            IsActive = false;

            if (room.game.session is StoryGameSession storyGameSession)
            {
                SlugBaseSaveData slugBaseSaveData = storyGameSession.saveState.miscWorldSaveData.GetSlugBaseData();
                slugBaseSaveData.Set(SAVEDATA_TRIGGER_WAS_FIRED_KEY, true);
            }

            _Plugin.logger.LogDebug("Triggered");

            WorldCoordinate worldSpawnCoordinate = room.GetWorldCoordinate(PlacedObject.pos + SpawnPoint);
            AbstractCreature abstractVultureCreature = new(room.world, StaticWorld.GetCreatureTemplate("King Vulture"), null, worldSpawnCoordinate, room.game.GetNewID())
            {
                saveCreature = false
            };


            SpawnedVultureDataCWT.Add(abstractVultureCreature, new SpawnedVultureData(isAlbino: true));

            abstractVultureCreature.setCustomFlags();
            abstractVultureCreature.Move(worldSpawnCoordinate);

            room.abstractRoom.AddEntity(abstractVultureCreature);
            abstractVultureCreature.RealizeInRoom();
        }

        private bool IsRoomPointInsideTrigger(Vector2 point)
        {
            return
                (point - PlacedObject.pos).sqrMagnitude <= TriggerAreaRadiusEndpoint.sqrMagnitude;
        }
    }
}
