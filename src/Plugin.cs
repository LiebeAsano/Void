using System;
using BepInEx;
using System.IO;
using UnityEngine;
using VoidTemplate;
using System.Security.Permissions;
using HarmonyLib;
using static Creature;
using MonoMod.RuntimeDetour.HookGen;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.LowLevel;
using System.Collections.Generic;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace TheVoid
{
    [BepInPlugin(MOD_ID, "TheVoid", "0.0.1")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "liebeasano.thevoid";
        public static readonly SlugcatStats.Name TheVoid = new("TheVoid");
        public delegate void orig_Eat(global::DaddyLongLegs self, bool eu);
        public delegate void hook_Eat(orig_Eat orig, global::DaddyLongLegs self, bool eu);

        public static bool isSpawned = false; // Публичное статическое поле

        public void Awake()
        {
            var harmony = new Harmony("liebeasano.thevoid");
            harmony.PatchAll();
            Harmony harmonyInstance = new Harmony("com.example.patch");
            HookEndpointManager.Add<hook_Eat>(typeof(DaddyLongLegs).GetMethod("Eat", BindingFlags.Instance | BindingFlags.Public), OnDaddyLongLegsEat);
        }

        private static void OnLeechAttached(On.Leech.orig_Attached orig, global::Leech self)
        {
            orig(self);
            //Debug.Log("[TheVoid] Leech attached to something.");

            if (self.grasps.Length > 0 && self.grasps[0] != null)
            {
                var grabbedCreature = self.grasps[0].grabbed as Creature;
                if (grabbedCreature is Player player)
                {
                    //Debug.Log("[TheVoid] Leech attached to player with slugcatStats.name: " + player.slugcatStats.name);

                    if (player.slugcatStats.name == Plugin.TheVoid)
                    {
                        //Debug.Log("[TheVoid] Leech detected attachment to TheVoid player during Attached. Initiating kill sequence.");
                        AsyncKillLeech(self);
                    }
                }
            }
        }

        private static async void AsyncKillLeech(global::Leech leech)
        {
            //Debug.Log("[TheVoid] Leech kill sequence initiated. Waiting 1 second.");
            await Task.Delay(6000);

            if (leech != null && leech.room != null)
            {
                //Debug.Log("[TheVoid] Killing Leech.");
                leech.Die();
            }
            else
            {
                //Debug.Log("[TheVoid] Leech or its room was null.");
            }
        }

        // Patch для обновления состояния сна/гибернации
        [HarmonyPatch(typeof(ShelterDoor), nameof(ShelterDoor.Close))]
        class Patch_ShelterDoor_Close
        {
            static void Postfix(ShelterDoor __instance)
            {
                // Получаем экземпляр RainWorldGame
                var gameInstance = __instance.room.game as RainWorldGame;

                if (gameInstance == null) return;

                // Проверка первого игрока в списке gameInstance.Players
                if (gameInstance.Players.Count > 0)
                {
                    for (int i = 0; i < gameInstance.Players.Count; i++)
                    {
                        var abstractPlayer = gameInstance.Players[i];

                        if (abstractPlayer.realizedCreature is Player slugcat &&
                            slugcat.slugcatStats.name == Plugin.TheVoid &&
                            slugcat.room != null &&
                            slugcat.room == __instance.room)
                        {
                            if (slugcat.FoodInStomach < slugcat.slugcatStats.foodToHibernate)
                            {
                                if (__instance.room.game.session is StoryGameSession session &&
                                    session.characterStats.name == Plugin.TheVoid &&
                                    (!ModManager.Expedition || !__instance.room.game.rainWorld.ExpeditionMode))
                                {
                                    if (session.saveState.deathPersistentSaveData.karma == 0 || session.saveState.deathPersistentSaveData.karma == 10)
                                    {
                                        gameInstance.GoToRedsGameOver();
                                        return;
                                    }
                                    else
                                    {
                                        gameInstance.GoToStarveScreen();
                                        return;
                                    }
                                }


                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public class PreventMalnourishmentPatch
        {
            public static void Postfix(Player __instance)
            {
                // Получаем экземпляр RainWorldGame
                var gameInstance = __instance.room?.game as RainWorldGame;

                if (gameInstance == null) return;

                // Проверка каждого игрока среди игроков в игре.
                if (gameInstance.Players.Count > 0)
                {
                    for (int i = 0; i < gameInstance.Players.Count; i++)
                    {
                        var abstractPlayer = gameInstance.Players[i];

                        if (abstractPlayer.realizedCreature is Player slugcat &&
                            slugcat.slugcatStats.name == TheVoid &&
                            slugcat.room != null &&
                            slugcat.room == __instance.room)

                        {
                            // Если слизнекот "TheVoid", снимаем любую истощенность только для него
                            if (slugcat.Malnourished)
                            {
                                slugcat.Die();
                            }
                        }
                    }
                }
            }
        }

        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.Creature.Violence += Creature_Violence;
            On.Leech.Attached += OnLeechAttached;
            On.Player.Update += Player_Update;
            On.StoryGameSession.AddPlayer += StoryGameSession_AddPlayer;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (self.slugcatStats.name == TheVoid)
            {
                Lung.UpdateLungCapacity(self);
            }
        }

        // Добавление игрока в сессию.
        private void StoryGameSession_AddPlayer(On.StoryGameSession.orig_AddPlayer orig, StoryGameSession self, AbstractCreature abstractCreature)
        {
            orig(self, abstractCreature);

            if (abstractCreature.realizedCreature is Player player && player.slugcatStats.name == TheVoid)
            {
                Lung.UpdateLungCapacity(player);
            }
        }

        private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus)
        {

            // Проверка, что существо — игрок с ID "TheVoid"
            if (self is Player player && player.slugcatStats.name == TheVoid && type == DamageType.Stab)
            {
                int KarmaCap = player.KarmaCap;// Уменьшаем эффект оглушения
                float StunResistance = 1f - 0.09f * KarmaCap; // уменьшение оглушения на 9% за каждый уровень KarmaCap
                float DamageResistance = 1f - 0.09f * KarmaCap;  // уменьшение урона на 9% за каждый уровень KarmaCap
                stunBonus *= StunResistance;
                damage *= DamageResistance;
                Logger.LogInfo($"Creature: {self} | DamageType: {type} | Damage: {damage} | StunBonus: {stunBonus}");
            }

            // Вызов оригинального метода с изменёнными значениями
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static async void OnDaddyLongLegsEat(orig_Eat orig, DaddyLongLegs self, bool eu)
        {

            // Проверим список eatObjects
            foreach (var eatObject in self.eatObjects)
            {
                if (eatObject.chunk?.owner is Player player)
                {
                    //Debug.Log($"Daddy Long Legs is eating a Player with slugcatStats.name: {player.slugcatStats.name}");

                    if (player.slugcatStats.name == TheVoid && player.dead)
                    {
                        //Debug.Log("Daddy Long Legs has eaten a TheVoid slugcat! Initiating death sequence.");
                        await Task.Delay(3000);
                        DestroyBody(player);
                        self.Die();
                        FinishEating(self);
                        return;  // Прерываем выполнение оригинального метода, если объект найден съеденным
                    }
                }
            }

            // Выполним оригинальный метод после всех проверок
            orig(self, eu);
        }

        private static void DestroyBody(Player player)
        {
            if (player != null && player.room != null)
            {
                player.room.RemoveObject(player);
            }
            player.dead = true;
        }

        private static void FinishEating(DaddyLongLegs self)
        {
            self.eatObjects.Clear();
            self.digestingCounter = 0;
            self.Update(false);
            self.moving = false;
            self.tentaclesHoldOn = false;
        }

        // Новый метод-обработчик для события съедения мяса
        [HarmonyPatch(typeof(Player), nameof(Player.EatMeatUpdate))]
        class Player_EatMeatUpdate_Patch
        {
            static void Postfix(Player __instance)
            {
                    if (__instance.eatMeat != 50)
                    {
                        return;
                    }

                    // Проверка текущего игрока на ID TheVoid
                    if (__instance.slugcatStats.name == Plugin.TheVoid)
                    {
                        //Debug.Log($"[TheVoid] Player {__instance.abstractCreature.ID} has ID TheVoid and will not die.");
                        return; // Слизнекот с ID TheVoid не умирает при поедании трупа с ID TheVoid
                    }

                    // Проверяем каждый захват слота Player
                    for (int i = 0; i < __instance.grasps.Length; i++)
                    {
                        var grasp = __instance.grasps[i];
                        if (grasp != null)
                        {
                            //Debug.Log($"[TheVoid] Grasp at index {i} is not null and grasped object is {grasp.grabbed?.GetType()}.");

                            if (grasp.grabbed is Player prey)
                            {
                                //Debug.Log($"[TheVoid] Player {__instance.abstractCreature.ID} is holding player {prey.abstractCreature.ID} with slugcatStats.name: {prey.slugcatStats.name}");

                                // Проверяем, удерживается ли труп с ID TheVoid
                                if (prey.slugcatStats.name == Plugin.TheVoid)
                                {
                                    //Debug.Log($"[TheVoid] Player {__instance.abstractCreature.ID} is holding Player {prey.abstractCreature.ID} with ID TheVoid.");

                                    // Условие смерти для слизнекота, который захватил труп с ID TheVoid
                                    __instance.Die();
                                    return;  // Прекращаем выполнение, чтобы избежать лишних проверок
                                }
                            }
                        }
                    }

                    // Дополнительный лог для отладки текущих захватов
                    //Debug.Log("[TheVoid] Dumping current grasps:");
                }
        }
        


        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                if (!isLoaded)
                {
                    //Create file named void.dev at RainWorld_Data\StreamingAssets to enabled
                    if (File.Exists(AssetManager.ResolveFilePath("void.dev")))
                    {
                        DevEnabled = true;
                    }
                    Nutils.hook.DeathSaveDataHook.Register<VoidSave>(SaveName);

                    On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

                    DeathHooks.Hook();
                    PlayerHooks.Hook();
                    OracleHooks.Hook();
                    KarmaHooks.Hook();
                    RoomHooks.Hook();
                    CreatureHooks.Hook();
                    if (DevEnabled)
                    {
                        // On.RainWorldGame.Update += RainWorldGame_TestUpdate;
                    }
                    LoadResources(self);
                    isLoaded = true;

                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }

        private static bool isLoaded;
        public static bool DevEnabled = false;

        public const string SaveName = "THEVOID";


        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.player.slugcatStats.name != TheVoid) return;
            foreach (var sprite in sLeaser.sprites)
            {
                if (sprite.element.name.StartsWith("PlayerArm") ||
                    sprite.element.name.StartsWith("Body") ||
                    sprite.element.name.StartsWith("Face") ||
                    sprite.element.name.StartsWith("Head") ||
                    sprite.element.name.StartsWith("Hips") ||
                    sprite.element.name.StartsWith("Leg") ||
                    sprite.element.name.StartsWith("OnTopOfTerrainHand"))
                {
                    string head =
                        self.player.abstractCreature.world.game.session is StoryGameSession session &&
                        session.saveState.deathPersistentSaveData.karma == 10
                            ? "TheVoid11-"
                            : "TheVoid-";
                    if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                        sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                }
            }
        }
        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {

            //load all sprites which name starts with "TheVoid" in folder "atlas-void" 
            DirectoryInfo folder = new DirectoryInfo(AssetManager.ResolveDirectory("atlas-void"));

            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                if (file.Name.StartsWith("TheVoid"))
                    Futile.atlasManager.LoadAtlas("atlas-void/" + file.Name.Split('.')[0]);
                Debug.Log("[The void] " + file.Name);
            }
            Futile.atlasManager.LoadImage("atlas-void/karma_blank");
        }

        /* private static void RainWorldGame_TestUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.session is StoryGameSession session &&
                session.saveStateNumber == TheVoid)
            {
                if (Input.GetKey(KeyCode.LeftControl) &&
                    session.saveState.deathPersistentSaveData.karmaCap != 10)
                {
                    session.saveState.deathPersistentSaveData.karmaCap = 10;
                    session.saveState.deathPersistentSaveData.karma = 10;
                    session.characterStats.foodToHibernate = 6;
                    self.cameras[0].hud.karmaMeter.UpdateGraphic(10, 10);
                    self.cameras[0].hud.foodMeter.MoveSurvivalLimit(6, true);
                }

                if (Input.GetKey(KeyCode.N) &&
                    session.saveState.deathPersistentSaveData.ghostsTalkedTo.Count(i => i.Value > 1) < 4)
                {
                    if (session.saveState.deathPersistentSaveData.ghostsTalkedTo.Count(i => i.Value > 1) < 4)
                        session.saveState.deathPersistentSaveData.ghostsTalkedTo.Add(GhostWorldPresence.GhostID.CC, 2);
                    if (session.saveState.deathPersistentSaveData.ghostsTalkedTo.Count(i => i.Value > 1) < 4)
                        session.saveState.deathPersistentSaveData.ghostsTalkedTo.Add(GhostWorldPresence.GhostID.LF, 2);
                    if (session.saveState.deathPersistentSaveData.ghostsTalkedTo.Count(i => i.Value > 1) < 4)
                        session.saveState.deathPersistentSaveData.ghostsTalkedTo.Add(GhostWorldPresence.GhostID.SH, 2);
                    if (session.saveState.deathPersistentSaveData.ghostsTalkedTo.Count(i => i.Value > 1) < 4)
                        session.saveState.deathPersistentSaveData.ghostsTalkedTo.Add(GhostWorldPresence.GhostID.SI, 2);
                    Debug.Log("[The Void] Add four Ghost");

                }

                if (Input.GetKey(KeyCode.J))
                {
                    session.saveState.miscWorldSaveData.SSaiConversationsHad = 5;
                    Debug.Log("[The Void] Set SSaiConversationsHad  6");

                }
                if (Input.GetKey(KeyCode.L))
                {
                    session.saveState.miscWorldSaveData.SSaiConversationsHad = 10;
                    Debug.Log("[The Void] Set SSaiConversationsHad 11");
                }

                if (Input.GetKey(KeyCode.M))
                {
                    session.saveState.miscWorldSaveData.SSaiConversationsHad = 2;
                    Debug.Log("[The Void] Set SSaiConversationsHad 3");

                }
            }
        } */

    }


    public class VoidSave
    {
        public int lastMeetCycles = 0;
        public int eatCounter = 0;
    }

}