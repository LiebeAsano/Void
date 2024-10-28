using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Pom.Pom;
using static Pom.Pom.ManagedFieldWithPanel;

namespace VoidTemplate.Objects.PomObjects
{
    internal class TheVoidRoomWideStaggerByGhostData : ManagedData
    {
        [IntegerField("MinIntervalFrames", 0, int.MaxValue, 400, ControlType.text, "Minimal Interval")]
        public int MinimalInterval = 0;
        [IntegerField("MaxIntervalFrames", 0, int.MaxValue, 800, ControlType.text, "Maximal Interval")]
        public int MaximalInterval = 10000;
        [IntegerField("MinDurationFrames", 0, int.MaxValue, 5, ControlType.text, "Minimal Duration")]
        public int MinimalDuration = 0;
        [IntegerField("MaxDurationFrames", 0, int.MaxValue, 20, ControlType.text, "Maximal Duration")]
        public int MaximalDuration = 10000;

        public TheVoidRoomWideStaggerByGhostData(PlacedObject owner) : base(owner, [])
        {
        }
    }

    internal class TheVoidRoomWideStaggerByGhost : UpdatableAndDeletable
    {
        public static void Register()
        {
            RegisterManagedObject<TheVoidRoomWideStaggerByGhost, TheVoidRoomWideStaggerByGhostData, ManagedRepresentation>("TheVoidRoomWideStaggerByGhost", "The Void", true);
        }

        private ConditionalWeakTable<Creature, IntStorage> stunCountdowns = new();

        private TheVoidRoomWideStaggerByGhostData data;
        private System.Random random = new();

        public TheVoidRoomWideStaggerByGhost(Room room, PlacedObject placedObject)
        {
            this.room = room;
            data = placedObject.data as TheVoidRoomWideStaggerByGhostData;
        }

        public override void Update(bool eu)
        {
            if (room.world.worldGhost == null)
                return;

            if (room.world.game.session is not StoryGameSession storyGameSession || storyGameSession.saveState.deathPersistentSaveData.karmaCap >= 9)
            {
                return;
            }

            foreach (AbstractCreature player in room.game.Players.Where(p => p.Room == room.abstractRoom))
            {
                Creature playerCreature = player.realizedCreature;

                if (playerCreature == null) continue;

                IntStorage stunCountdown;

                if (playerCreature.Stunned)
                {
                    stunCountdowns.Remove(playerCreature);
                    continue;
                }

                if (!stunCountdowns.TryGetValue(playerCreature, out stunCountdown))
                {
                    stunCountdowns.Add(playerCreature, GenerateNextStunCountdown());
                    continue;
                }

                if (stunCountdown <= 0)
                {
                    playerCreature.Stun(GenerateStunDuration());
                    stunCountdowns.Remove(playerCreature);
                    continue;
                }

                stunCountdown.value--;
            }
        }

        private int GenerateNextStunCountdown() => random.Next(data.MinimalInterval, data.MaximalInterval);
        private int GenerateStunDuration() => random.Next(data.MinimalDuration, data.MaximalDuration);

        //private bool ParseData(out ParsedData parsedData)
        //{
        //    parsedData = new();


        //}

        //private struct ParsedData
        //{
        //    public int MinimalInterval;
        //    public int MaximalInterval;
        //    public int MinimalDuration;
        //    public int MaximumDuration;
        //}
        private class IntStorage
        {
            public int value;

            public static implicit operator int(IntStorage storage) => storage.value;
            public static implicit operator IntStorage(int value) => new() { value = value };
        }
    }
}
