using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using SlugBase.Features;

namespace VoidTemplate.RainCycleChanges
{
    public static class PostRainCycle
    {
        private static readonly ConditionalWeakTable<RainCycle, RainCycleExt> rainCycleExt = new();

        public static RainCycleExt GetRainCycleExt(this RainCycle rainCycle) => rainCycleExt.GetValue(rainCycle, _ => new(rainCycle));

        public static void Hook()
        {
            On.StoryGameSession.ctor += StoryGameSession_ctor;
            On.RainCycle.Update += RainCycle_Update;
            On.GlobalRain.ResetRain += GlobalRain_ResetRain;
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
        }

        private static bool startMalnourished;

        private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);
            startMalnourished = game.Players[0].realizedCreature is Player player && player.Malnourished; 
        }

        private static void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
        {
            World oldWorld = self.activeWorld;
            World newWorld = self.worldLoader?.ReturnWorld();
            orig(self, warpUsed);
            if (oldWorld != null && newWorld != null)
            {
                newWorld.rainCycle.GetRainCycleExt().myTimer = oldWorld.rainCycle.GetRainCycleExt().myTimer;
            }
        }

        private static void GlobalRain_ResetRain(On.GlobalRain.orig_ResetRain orig, GlobalRain self)
        {
            orig(self);

            var postCycle = self.game.world.rainCycle.GetRainCycleExt();
            postCycle.subtractedFood = 0;
            postCycle.stunned = 0;
            postCycle.myTimer = 0;
            postCycle.metersReplaced = false;

            if (DeadlessRain._states.TryGetValue(self, out var st)) st.t = 0f;

            for (int i = 0; i < self.game.cameras.Length; i++)
            {
                if (self.game.cameras[i].hud is HUD.HUD { rainMeter: not null } hud &&
                    hud.rainMeter.GetAfterCycleMode().Value)
                {
                    hud.rainMeter.slatedForDeletion = true;
                    hud.AddPart(new RainMeter(hud, hud.fContainers[1]));
                }
            }
        }

        private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {
            orig(self);
            if (self.world.game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void)
                self.GetRainCycleExt().AfterCycleUpdate();
        }

        public class RainCycleExt
        {
            public const int postAfterCyceleTicks = 72000; // 30 minutes is 72000 ticks

            public RainCycle owner;

            public int postCycleLength;

            public int myTimer;

            public int stunned;

            public int subtractedFood;

            public bool metersReplaced;

            public readonly int subtractFoodInterval;

            public OverseersWorldAI.ShelterFinder myShelterFinder;

            public GlobalRain GRain
            {
                get
                {
                    return owner.world.game.globalRain;
                }
            }

            public bool PostCycleStarted
            {
                get
                {
                    return GRain.deathRain?.deathRainMode == GlobalRain.DeathRain.DeathRainMode.Mayhem;
                }
            }

            public float AmountLeft { get => (float)(postCycleLength - myTimer) / postCycleLength; }

            public bool TimeToLockShelters
            {
                get => myTimer > 2400;
            }

            public bool AllowToSubtractFood { get => myTimer > 4800 && myTimer <= (postCycleLength - 4800); }

            public int TimeToStartNewCycle
            {
                get
                {
                    return postCycleLength - myTimer;
                }
            }

            public RainCycleExt(RainCycle owner)
            {
                this.owner = owner;
                postCycleLength = postAfterCyceleTicks - owner.cycleLength;
                if (postCycleLength <= 0)
                {
                    postCycleLength = 14400;
                }
                subtractFoodInterval = (postCycleLength - 9600) / owner.world.game.session.characterStats.foodToHibernate;
            }

            public void AfterCycleUpdate()
            {
                if (myShelterFinder == null)
                {
                    myShelterFinder = new(owner.world);
                    new Thread(StartToMapThread).Start();
                }
                if (PostCycleStarted && myShelterFinder.done)
                {
                    if (!metersReplaced)
                    {
                        for (int i = 0; i < owner.world.game.cameras.Length; i++)
                        {
                            if (owner.world.game.cameras[i].hud is HUD.HUD { rainMeter: not null } hud && !hud.rainMeter.GetAfterCycleMode().Value)
                            {
                                hud.rainMeter.slatedForDeletion = true;
                                hud.AddPart(new RainMeter(hud, hud.fContainers[1]));
                            }
                        }
                        metersReplaced = true;
                    }
                    myTimer++;
                    if (AllowToSubtractFood && owner.world.game.Players[0].realizedCreature is Player player && player.playerState.alive && (4800 - myTimer) % subtractFoodInterval == 0)
                    {
                        if (player.FoodInStomach > 0)
                        {
                            player.SubtractFood(1);
                        }
                        else if (!owner.world.game.GetStorySession.saveState.GetVoidMarkV3() || startMalnourished)
                        {
                            player.Die();
                        }
                        else if (!player.Malnourished)
                        {
                            player.SetMalnourished(true);
                        }
                        else
                        {
                            stunned++;
                            player.stun += 40 * stunned;
                        }
                        subtractedFood++;
                    }
                    if (TimeToStartNewCycle <= 0)
                    {
                        float newCycleLength = Mathf.Lerp(owner.world.game.rainWorld.setup.cycleTimeMin, owner.world.game.rainWorld.setup.cycleTimeMax, Random.value);
                        RainCycle newRainCycle = new(owner.world, newCycleLength);
                        newRainCycle.GetRainCycleExt().myShelterFinder = myShelterFinder;
                        owner.world.rainCycle = newRainCycle;
                        if (newRainCycle.maxPreTimer > 0)
                        {
                            newRainCycle.maxPreTimer = 0;
                            newRainCycle.preTimer = 0;
                            newRainCycle.preCycleRainPulse_WaveC = 0;
                            GRain.preCycleRainPulse_Scale = 0;
                            GRain.drainWorldFlood = 0;
                        }
                        GRain.ResetRain();
                        for (int i = 0; i < owner.world.abstractRooms.Length; i++)
                        {
                            var room = owner.world.abstractRooms[i];
                            for (int j = 0; j < room.creatures.Count; j++)
                            {
                                room.creatures[j].state.CycleTick();
                            }
                            for (int j = 0; j < room.entitiesInDens.Count; j++)
                            {
                                if (room.entitiesInDens[j] is AbstractCreature crit)
                                {
                                    crit.state.CycleTick();
                                }
                            }
                        }

                        for (int i = 0; i < owner.world.activeRooms.Count; i++)
                        {
                            Room room = owner.world.activeRooms[i];
                            if (room.ReadyForPlayer)
                            {
                                for (int j = room.lockedShortcuts.Count - 1; j >= 0; j--)
                                {
                                    var shortcut = room.shortcutData(room.lockedShortcuts[j]);
                                    if (shortcut.shortCutType == ShortcutData.Type.RoomExit)
                                    {
                                        AbstractRoom leadingRoom = room.world.GetAbstractRoom(room.abstractRoom.connections[shortcut.destNode]);
                                        if (leadingRoom != null && leadingRoom.shelter && !leadingRoom.world.brokenShelters[leadingRoom.shelterIndex])
                                        {
                                            room.lockedShortcuts.RemoveAt(j);
                                        }
                                    }
                                }
                            }
                        }
                        var absPlayer = owner.world.game.FirstAlivePlayer;
                        if (absPlayer != null && absPlayer.state.alive)
                        {
                            SaveProgress();
                            for (int i = 0; i < owner.world.game.cameras.Length; i++)
                            {
                                if (owner.world.game.cameras[i].hud is HUD.HUD { karmaMeter: var karmaMeter })
                                    karmaMeter.reinforceAnimation = 1;
                            }
                        }
                    }
                }
            }

            public void SaveProgress()
            {
                var saveState = owner.world.game.GetStorySession.saveState;
                saveState.cycleNumber++;
                if (saveState.deathPersistentSaveData.karma < saveState.deathPersistentSaveData.karmaCap)
                    saveState.deathPersistentSaveData.karma++;

                if (!owner.world.game.session.characterStats.malnourished)
                {
                    RainWorldGame.ForceSaveNewDenLocation(owner.world.game, ComputeNearestShelter(), true);
                }
            }

            public string ComputeNearestShelter()
            {
                AbstractCreature player = owner.world.game.FirstAlivePlayer ?? owner.world.game.FirstAnyPlayer;
                if (player != null && player.Room != null)
                {
                    float minDistance = float.MaxValue;
                    int nearestShelterIndex = -1;
                    for (int i = 0; i < player.world.shelters.Length; i++)
                    {
                        for (int j = 0; j < player.Room.connections.Length; j++)
                        {
                            float distance = myShelterFinder.DistanceToShelter(i, new(player.Room.index, -1, -1, j));
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nearestShelterIndex = i;
                            }
                        }
                    }
                    if (nearestShelterIndex > -1)
                    {
                        return owner.world.GetAbstractRoom(owner.world.shelters[nearestShelterIndex]).name;
                    }
                }
                return owner.world.GetAbstractRoom(owner.world.shelters[Random.Range(0, owner.world.shelters.Length)]).name;
            }

            public void StartToMapThread()
            {
                for (; ; )
                {
                    if (myShelterFinder.done) break;
                    myShelterFinder.Update();
                }
            }
        }
    }
}
