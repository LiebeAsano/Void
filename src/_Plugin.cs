using System;
using BepInEx;
using System.IO;
using UnityEngine;
using System.Security.Permissions;
using System.Linq;
using BepInEx.Logging;
using static VoidTemplate.Useful.Utils;
using RWCustom;
using static Room;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace VoidTemplate;

[BepInPlugin(MOD_ID, "TheVoid", "0.0.1")]
class _Plugin : BaseUnityPlugin
{
	private const string MOD_ID = "liebeasano.thevoid";
	
	/// <summary>
	/// this logger will automatically prepend all logs with mod name. Logs into bepinex logs rather than console logs
	/// </summary>
	public static ManualLogSource logger;
	
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
				VoidEnums.RegisterEnums();

				if (File.Exists(AssetManager.ResolveFilePath("void.dev")))
				{
					DevEnabled = true;
				}
				On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

				CycleEnd.Hook();
				PlayerSpawnManager.ApplyHooks();
				PermadeathConditions.Hook();
				PlayerHooks.Hook();
				Oracles.OracleHooks.Hook();
				KarmaHooks.Hook();
				RoomHooks.Hook();
				EdibleChanges.Hook();
				MenuTinkery._MenuMeta.Startup();
				CreatureInteractions._CreatureInteractionsMeta.Hook();
				PlayerMechanics._PlayerMechanicsMeta.Hook();
				OptionInterface._OIMeta.Initialize();
				if (DevEnabled)
				{
					//On.RainWorldGame.Update += RainWorldGame_TestUpdate;
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

    //Atlas

    private static bool IsTouchingCeiling(Player player)
    {
        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        Vector2 upperPosition_0 = body_chunk_0.pos + new Vector2(0, body_chunk_0.rad + 5);
        Vector2 upperPosition_1 = body_chunk_1.pos + new Vector2(0, body_chunk_1.rad + 5);

        IntVector2 tileAbove_0 = player.room.GetTilePosition(upperPosition_0);
        IntVector2 tileAbove_1 = player.room.GetTilePosition(upperPosition_1);

        bool isSolid_0 = player.room.GetTile(tileAbove_0).Solid;
        bool isSolid_1 = player.room.GetTile(tileAbove_1).Solid;

        return isSolid_0 || isSolid_1;
    }

    private static bool IsTouchingDiagonalCeiling(Player player)
    {
        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        Vector2[] directions = {
            new Vector2(0, 1)
            };

        foreach (var direction in directions)
        {
            Vector2 checkPosition_0 = body_chunk_0.pos + direction * (body_chunk_0.rad + 10);
            Vector2 checkPosition_1 = body_chunk_1.pos + direction * (body_chunk_1.rad + 10);

            IntVector2 tileDiagonal_0 = player.room.GetTilePosition(checkPosition_0);
            IntVector2 tileDiagonal_1 = player.room.GetTilePosition(checkPosition_1);

            // Использование IdentifySlope для определения диагонального тайла
            SlopeDirection slopeDirection_0 = player.room.IdentifySlope(tileDiagonal_0);
            SlopeDirection slopeDirection_1 = player.room.IdentifySlope(tileDiagonal_1);

            bool isDiagonal = (slopeDirection_0 == SlopeDirection.UpLeft ||
                       slopeDirection_0 == SlopeDirection.UpRight ||
                       slopeDirection_0 == SlopeDirection.DownLeft ||
                       slopeDirection_0 == SlopeDirection.DownRight ||
                       slopeDirection_1 == SlopeDirection.UpLeft ||
                       slopeDirection_1 == SlopeDirection.UpRight ||
                       slopeDirection_1 == SlopeDirection.DownLeft ||
                       slopeDirection_1 == SlopeDirection.DownRight);

            if (isDiagonal)
            {
                return true;
            }
        }

        return false;
    }

    const int tailSpriteIndex = 2;
	private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (!self.player.IsVoid()) return;
		foreach (var sprite in sLeaser.sprites)
		{
			if (sLeaser.sprites[tailSpriteIndex] is TriangleMesh tail &&
			self.player.abstractCreature.world.game.session is StoryGameSession session &&
			session.saveState.deathPersistentSaveData.karma == 10)
			{
				tail.element = Futile.atlasManager.GetElementWithName("TheVoid-StuntTail");
				tail.color = new(1f, 0.86f, 0f);
				for (var i = tail.vertices.Length - 1; i >= 0; i--)
				{
					var perc = i / 2 / (float)(tail.vertices.Length / 2);

					Vector2 uv;
					if (i % 2 == 0)
						uv = new Vector2(perc, 0f);
					else if (i < tail.vertices.Length - 1)
						uv = new Vector2(perc, 1f);
					else
						uv = new Vector2(1f, 0f);

					// Map UV values to the element
					uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
					uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

					tail.UVvertices[i] = uv;
				}
			}
            if (sprite.element.name.StartsWith("Head"))
			{

                if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string head = "TheVoidDCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                    else
                    {
                        string head = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                }
                else if (IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
				{
                    if (!self.player.input[0].jmp)
                    {
                        string head = "TheVoidCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                    else
                    {
                        string head = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                }
            }
            if (sprite.element.name.StartsWith("Face"))
			{

                BodyChunk body_chunk_0 = self.player.bodyChunks[0];
                BodyChunk body_chunk_1 = self.player.bodyChunks[1];

                if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string face = "TheVoidDCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                    else
                    {
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                }

                else if ((IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl))
				{
					if (!self.player.input[0].jmp)
					{
						string face = "TheVoidCeil-";
						if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
					}
					else
					{
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
				}

                else
                {
                    if (body_chunk_0.pos.y + 10f > body_chunk_1.pos.y)
                    {
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                    else
                    {
                        string face = "TheVoidDown-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                }
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


	/*private static void RainWorldGame_TestUpdate(On.RainWorldGame.orig_Update orig, RainWorldGame self)
	{
		orig(self);
		if (self.session is StoryGameSession session &&
			session.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
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
	}*/

}