using System;
using BepInEx;
using System.IO;
using UnityEngine;
using VoidTemplate;
using System.Security.Permissions;
using System.Linq;
using BepInEx.Logging;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TheVoid;

[BepInPlugin(MOD_ID, "TheVoid", "0.0.1")]
class Plugin : BaseUnityPlugin
{
    private const string MOD_ID = "liebeasano.thevoid";
    
    /// <summary>
    /// this logger will automatically prepend all logs with mod name. Logs into bepinex logs rather than console logs
    /// </summary>
    public static ManualLogSource logger;
    

    public const string SaveName = "THEVOID";
    public static bool DevEnabled = false;

    public void OnEnable()
    {
        logger = Logger;
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;
    }

    private static bool ModLoaded;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (!ModLoaded)
            {
                StaticStuff.RegisterEnums();
                Dreams.RegisterMaps();
                if (File.Exists(AssetManager.ResolveFilePath("void.dev")))
                {
                    DevEnabled = true;
                }
                On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
                On.Player.Update += PlayerLungLogic;
                On.StoryGameSession.AddPlayer += StoryGameSession_AddPlayer;
                On.ShelterDoor.Close += CycleEndLogic;
                On.Player.Update += MalnourishmentDeath;
                On.Player.EatMeatUpdate += DontEatVoid;
                PlayerSpawnManager.ApplyHooks();
                ColdImmunityPatch.Hook();
                DeathHooks.Hook();
                PlayerHooks.Hook();
                OracleHooks.Hook();
                KarmaHooks.Hook();
                RoomHooks.Hook();
                CreatureHooks.Hook();
                MenuHooks.Hook();
                Dreams.Hook();
                if (DevEnabled)
                {
                    On.RainWorldGame.Update += RainWorldGame_TestUpdate;
                }
                LoadResources();
                ModLoaded = true;

            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

    }




    private void CycleEndLogic(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        orig(self);
        RainWorldGame game = self.room.game;
        game.Players.ForEach(absPlayer =>
        {
            if (absPlayer.realizedCreature is Player player
            && player.slugcatStats.name == StaticStuff.TheVoid
            && player.room != null
            && player.room == self.room
            && player.FoodInStomach < player.slugcatStats.foodToHibernate
            && self.room.game.session is StoryGameSession session
            && session.characterStats.name == StaticStuff.TheVoid
            && (!ModManager.Expedition || !self.room.game.rainWorld.ExpeditionMode))
            {
                if (session.saveState.deathPersistentSaveData.karma == 0 || session.saveState.deathPersistentSaveData.karma == 10) game.GoToRedsGameOver();
                else game.GoToStarveScreen();
            }
        });
    }

    private void MalnourishmentDeath(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.room == null) return;
        RainWorldGame game = self.room.game;
        game.Players.ForEach(absPlayer =>
        {
            if (absPlayer.realizedCreature is Player player
            && player.slugcatStats.name == StaticStuff.TheVoid
            && player.room != null
            && player.room == self.room
            && player.Malnourished) player.Die();
        });

    }

    private void PlayerLungLogic(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.slugcatStats.name == StaticStuff.TheVoid) Lung.UpdateLungCapacity(self);
    }

    private void StoryGameSession_AddPlayer(On.StoryGameSession.orig_AddPlayer orig, StoryGameSession self, AbstractCreature abstractCreature)
    {
        orig(self, abstractCreature);

        if (abstractCreature.realizedCreature is Player player
            && player.slugcatStats.name == StaticStuff.TheVoid)
        {
            Lung.UpdateLungCapacity(player);
        }
    }
    // Новый метод-обработчик для события съедения мяса
    private void DontEatVoid(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig(self, graspIndex);
        if (self.eatMeat != 50 || self.slugcatStats.name == StaticStuff.TheVoid) return;
        Array.ForEach(self.grasps, grasp =>
        {
            if (grasp != null
            && grasp.grabbed is Player prey
            && prey.slugcatStats.name == StaticStuff.TheVoid)
                self.Die();

        });
    }




    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.player.slugcatStats.name != StaticStuff.TheVoid) return;
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
    private void LoadResources()
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


    private static void RainWorldGame_TestUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (self.session is StoryGameSession session &&
            session.saveStateNumber == StaticStuff.TheVoid)
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
                logger.LogMessage("Add four Ghost");

            }

            if (Input.GetKey(KeyCode.J))
            {
                session.saveState.miscWorldSaveData.SSaiConversationsHad = 5;
                logger.LogMessage("Set SSaiConversationsHad  6");

            }
            if (Input.GetKey(KeyCode.L))
            {
                session.saveState.miscWorldSaveData.SSaiConversationsHad = 10;
                logger.LogMessage("Set SSaiConversationsHad 11");
            }

            if (Input.GetKey(KeyCode.M))
            {
                session.saveState.miscWorldSaveData.SSaiConversationsHad = 2;
                logger.LogMessage("Set SSaiConversationsHad 3");

            }
        }
    }

}