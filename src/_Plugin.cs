using BepInEx;
using BepInEx.Logging;
using Fisobs.Core;
using System;
using System.IO;
using System.Security.Permissions;
using UnityEngine;
using VoidTemplate.Creatures;
using VoidTemplate.Misc;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.GhostFeatures;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.PlayerMechanics.Karma11Foundation;
using VoidTemplate.PlayerMechanics.ViyMechanics;
using VoidTemplate.Useful;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace VoidTemplate;

[BepInPlugin(MOD_ID, "Void", "0.17.2")]
class _Plugin : BaseUnityPlugin
{
	private const string MOD_ID = "void.lwteam";

	/// <summary>
	/// this logger will automatically prepend all logs with mod name. Logs into bepinex logs rather than console logs
	/// </summary>
	public static ManualLogSource logger;

	public static bool DevEnabled = false;
	public void OnEnable()
	{
		logger = Logger;
		On.RainWorld.OnModsInit += RainWorld_OnModsInit;
		On.RainWorld.OnModsDisabled += delegate (On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
	{
    	orig.Invoke(self, newlyDisabledMods);
    for (int i = 0; i < newlyDisabledMods.Length; i++)
    {
        
            if (MultiplayerUnlocks.CreatureUnlockList.Contains(SandboxUnlockID.Mimicstarfish))
            {
                MultiplayerUnlocks.CreatureUnlockList.Remove(SandboxUnlockID.Mimicstarfish);
            }
			if (MultiplayerUnlocks.CreatureUnlockList.Contains(SandboxUnlockID.Outspector))
			{
				MultiplayerUnlocks.CreatureUnlockList.Remove(SandboxUnlockID.Outspector);
			}
			if (MultiplayerUnlocks.CreatureUnlockList.Contains(SandboxUnlockID.OutspectorB))
			{
				MultiplayerUnlocks.CreatureUnlockList.Remove(SandboxUnlockID.OutspectorB);
			}
			CreatureTemplateType.UnregisterValues();
            SandboxUnlockID.UnregisterValues();
    }
	};

	Content.Register(
   [
		new MimicstarfishCritob(),
		new OutspectorCritob(),
		new OutspectorBCritob()
   ]);
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
				Oracles._OracleMeta.Hook();
				KarmaHooks.Hook();
				RoomHooks.Hook();
				MenuTinkery._MenuMeta.Startup();
				CreatureInteractions._CreatureInteractionsMeta.Hook();
				PersistCycleLengthForGracePeriodRestarts.Hook();
				_GhostFeaturesMeta.Hook();
				_Karma11FeaturesMeta.Hook();
				_Karma11FoundationMeta.Hook();
				_PlayerMechanicsMeta.Hook();
				_MiscMeta.Hook();
				_ViyMechanicsMeta.Hook();
				VoidCycleLimit.Hook();
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
		Objects.PomObjects.LizardCorpse.Register();
		Objects.PomObjects.AlbinoVultureTriggerSpawner.Register();
        Objects.PomObjects.TriggeredSpasm.Register();
        Objects.PomObjects.VultureTriggerSpawner.Register();
		Objects.PomObjects.Warp.Register();
		Objects.PomObjects.TriggeredSpasm.Register();
    }

	// Load any resources, such as sprites or sounds
	private void LoadResources()
	{

		//load all sprites which name starts with "Void" in folder "atlas-void" 
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

}