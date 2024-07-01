using System;
using BepInEx;
// using Mono.Cecil.Cil;
// using MonoMod.Cil;
using System.IO;
// using System;
// using System.Runtime.CompilerServices;
using UnityEngine;


using VoidTemplate;
using System.Security.Permissions;
using System.Linq;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace TheVoid
{
    [BepInPlugin(MOD_ID, "TheVoid", "0.0.1")]
    class Plugin : BaseUnityPlugin {
        private const string MOD_ID = "liebeasano.thevoid";


        public static readonly SlugcatStats.Name TheVoid = new("TheVoid");

        // Add hooks
        public void OnEnable() { 
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
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
            if(self.player.slugcatStats.name != TheVoid) return;
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
                    if(Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
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
