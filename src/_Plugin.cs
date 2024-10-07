using BepInEx;
using BepInEx.Logging;
using System;
using System.IO;
using System.Security.Permissions;
using UnityEngine;
using VoidTemplate.Misc;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.GhostFeatures;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.PlayerMechanics.Karma11Foundation;
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

				CycleEnd.Hook();
				DrawSprites.Hook();
				PlayerSpawnManager.ApplyHooks();
				PermadeathConditions.Hook();
				Oracles.OracleHooks.Hook();
				KarmaHooks.Hook();
				RoomHooks.Hook();
				MenuTinkery._MenuMeta.Startup();
				CreatureInteractions._CreatureInteractionsMeta.Hook();
				PersistCycleLengthForGracePeriodRestarts.Hook();
				_GhostFeaturesMeta.Hook();
				_Karma11FeaturesMeta.Hook();
				_Karma11FoundationMeta.Hook();
				PlayerMechanics._PlayerMechanicsMeta.Hook();
				_MiscMeta.Hook();
				OptionInterface._OIMeta.Initialize();

				RegisterPOMObjects();
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

	private static void RegisterPOMObjects()
	{
		Objects.PomObjects.TheVoidRoomWideStaggerByGhost.Register();
	}

	// Load any resources, such as sprites or sounds
	private void LoadResources()
	{

		//load all sprites which name starts with "TheVoid" in folder "atlas-void" 
		DirectoryInfo folder = new DirectoryInfo(AssetManager.ResolveDirectory("atlas-void"));

		var listOfFiles = folder.GetFiles();
		foreach (FileInfo file in listOfFiles)
		{
			if (file.Extension == ".png")
			{
				if (Array.Exists(listOfFiles, file2 => NameWithoutExtension(file2) == NameWithoutExtension(file) && file2.Extension == ".txt"))
				{
					Futile.atlasManager.LoadAtlas("atlas-void/" + NameWithoutExtension(file));
				}
				else
				{
					Futile.atlasManager.LoadImage("atlas-void/" + NameWithoutExtension(file));
				}
			}
		}

		static string NameWithoutExtension(FileInfo f) => f.Name.Split('.')[0];
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