using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using VoidTemplate.Useful;
using static Pom.Pom;
using static VoidTemplate.Useful.POMUtils;

namespace VoidTemplate.Objects.PomObjects;

internal class Warp : UpdatableAndDeletable
{
	public static void Register()
	{
		ManagedField[] exposedFields = [
			defaultVectorField,
			new StringField(targetRoomName, "SS_D08"),
			new FloatField(timeToFadeIn, 0.1f, 200f, 60f, displayName: "fadein time"),
			new FloatField(timeToFadeOut, 0.1f, 200f, 60f, displayName: "fadeout time"),
			new BooleanField(forceSpawningAtTarget, false, displayName: "force new den"),
			new FloatField(cycleTime, 0f, 1f, 0f, 0.05f, displayName: "subtract time"),
			new IntegerField(spendCycles, 0, 15, 0, displayName: "spend cycles")
			];
		RegisterFullyManagedObjectType(exposedFields, typeof(Warp), category: "The Void");
		WarpDestination.Register();
		//since cycling through UAD is index based,
		//removing warp from UAD while room gives it update tick means
		//game assumes it has finished updating *next* object
		//or error out of range while doing so
		On.OverWorld.Update += OverWorld_Update;
		//this is the method that is only used in spawning to realize players for the first time in the cycle
		On.Room.ShortCutsReady += Room_ShortCutsReady;
	}

	private static void Room_ShortCutsReady(On.Room.orig_ShortCutsReady orig, Room self)
	{
		bool playerRealization = self.game is not null && self.game.Players.Count > 0 && self.game.Players[0].realizedCreature is null;
		orig(self);
		if (playerRealization
			&& self.game.IsStorySession
			&& self.abstractRoom.name == self.game.GetStorySession.saveState.denPosition)
		{
			foreach (UpdatableAndDeletable uad in self.updateList)
			{
				if (uad is WarpDestination destination)
				{
					for (int i = 0; i < self.game.Players.Count; i++)
					{
						Player p = self.game.Players[i].realizedCreature as Player;
						p.SuperHardSetPosition(destination.Pos + new Vector2(20f * i, 0f));
						p.standing = true;
					}
					break;
				}
			}
		}
	}

	//Swapping the world directly from UAD yields a side effect:
	//RWG
	//for (int m = this.world.activeRooms.Count - 1; m >= 0; m--)
	//{
	//		this.world.activeRooms[m].Update();
	//		this.world.activeRooms[m].PausedUpdate();
	//}
	//
	//where world => this.overworld.world
	//so when one swaps worlds from within UAD, update index stays
	//but the number of active rooms drops
	//which leads to index out of range
	static ConditionalWeakTable<OverWorld, StrongBox<(WorldLoader, Warp, string)>> customLoader = new();
private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
	{
		orig(self);
		if(customLoader.TryGetValue(self, out var loader))
		{

			AbstractRoom absRoom = WorldLoaded(self, loader.Value);
			loader.Value.Item2.OnRoomChange(absRoom);
			customLoader.Remove(self);
			GC.Collect();
		}


		//if you dare to use this method, be aware that players realize at (10;10) coordinate
		//it used to be (0;0), which really threw off camera logic
		//after using this, change slugcat positions and apply camera change
		AbstractRoom WorldLoaded(OverWorld overWorld, (WorldLoader, Warp, string) args)
		{
			World world = overWorld.activeWorld;
			World world2 = args.Item1.ReturnWorld();
			AbstractRoom abstractRoom2 = null;
			abstractRoom2 = world2.GetAbstractRoom(args.Item3);
			overWorld.activeWorld = world2;
			if (overWorld.game.roomRealizer != null)
			{
				overWorld.game.roomRealizer = new RoomRealizer(overWorld.game.roomRealizer.followCreature, world2);
			}

			abstractRoom2.RealizeRoom(world2, overWorld.game);

			foreach (AbstractCreature absPly in overWorld.game.Players)
			{
				WorldCoordinate worldCoordinate = new(abstractRoom2.index, 10, 10, -1);

				absPly.world.GetAbstractRoom(absPly.pos).RemoveEntity(absPly);
				absPly.world = world2;
				absPly.pos = worldCoordinate;
				absPly.world.GetAbstractRoom(worldCoordinate).AddEntity(absPly);


				if (absPly.realizedCreature is Player p
					&& p.room is not null)
				{
					p.room.RemoveObject(p);
					p.PlaceInRoom(abstractRoom2.realizedRoom);
					p.standing = true;
					
					if (p.objectInStomach is not null) p.objectInStomach.world = world2;

					foreach (Creature.Grasp grasp in p.grasps)
					{
						if(grasp is not null)
						{
							var APO = grasp.grabbed.abstractPhysicalObject;
							APO.world.GetAbstractRoom(APO.pos).RemoveEntity(APO);
							APO.world = world2;
							APO.pos = worldCoordinate;
							APO.world.GetAbstractRoom (worldCoordinate).AddEntity(APO);
						}
					}
				}
			}
			Room realizedDestination = abstractRoom2.realizedRoom;
			WarpDestination warpDestination = null;
			if (realizedDestination.updateList.Exists(UAD =>
			{
				if (UAD is WarpDestination wDest)
				{
					warpDestination = wDest;
					return true;
				}
				return false;
			}))
			{
				List<AbstractCreature> players = realizedDestination.game.Players;
				for (short i = 0; i < players.Count; i++)
				{
					if (players[i].realizedCreature is not null)
					{
						foreach (var bodyChunk in players[i].realizedCreature.bodyChunks)
						{
							bodyChunk.pos = warpDestination.Pos + new Vector2(i * 20f, 0f);
						}
					}
				}
			}

			foreach (var camera in overWorld.game.cameras)
			{
				camera.virtualMicrophone.AllQuiet();

				camera.MoveCamera(abstractRoom2.realizedRoom, 0);
				camera.ApplyPositionChange();
				camera.GetCameraBestIndex();

				var hud = camera.hud;
				hud.ResetMap(new(world2, overWorld.game.rainWorld));
				camera.dayNightNeedsRefresh = true;
				if (hud.textPrompt.subregionTracker is not null) hud.textPrompt.subregionTracker.lastShownRegion = 0;

			}

			overWorld.worldLoader = null;

			if (world.regionState is not null) world.regionState.world = null;

			world2.rainCycle.baseCycleLength = world.rainCycle.baseCycleLength;
			world2.rainCycle.cycleLength = world.rainCycle.cycleLength;
			world2.rainCycle.timer = world.rainCycle.timer;
			world2.rainCycle.duskPalette = world.rainCycle.duskPalette;
			world2.rainCycle.nightPalette = world.rainCycle.nightPalette;
			world2.rainCycle.dayNightCounter = world.rainCycle.dayNightCounter;

			if (ModManager.MSC)
			{
				if (world.rainCycle.timer == 0)
				{
					world2.rainCycle.preTimer = world.rainCycle.preTimer;
					world2.rainCycle.maxPreTimer = world.rainCycle.maxPreTimer;
				}
				else
				{
					world2.rainCycle.preTimer = 0;
					world2.rainCycle.maxPreTimer = 0;
				}
			}

			if (!world2.activeRooms.Contains(abstractRoom2.realizedRoom)) world2.activeRooms.Add(abstractRoom2.realizedRoom);

			if (ModManager.MMF)
			{
				GC.Collect();
			}
			return abstractRoom2;
		}
	}

	#region exposed data IDs
	const string triggerZone = "trigger zone";
	const string targetRoomName = "TargetRoomName";
	const string timeToFadeIn = "timetofadein";
	const string timeToFadeOut = "timetofadeout";
	const string forceSpawningAtTarget = "forceSpawning";
	const string cycleTime = "cycleTime";
	const string spendCycles = "spendCycles";
	#endregion

	public Warp(Room room, PlacedObject pobj)
	{
		this.room = room;
		placedObject = pobj;
		data = placedObject.data as ManagedData;
	}

	#region exposed fields
	Vector2[] TriggerZone => POMUtils.AddRealPosition(data.GetValue<Vector2[]>(triggerZone), placedObject.pos);
	string TargetRoom => data.GetValue<string>(targetRoomName);
	string Acronym => TargetRoom.Split('_')[0];
	float TimeToFadeIn => data.GetValue<float>(timeToFadeIn);
	float TimeToFadeOut => data.GetValue<float>(timeToFadeOut);
	bool ForceDenSwitch => data.GetValue<bool>(forceSpawningAtTarget);
	float SubtractCycleTime => data.GetValue<float>(cycleTime);
	int SubtractCycles => data.GetValue<int>(spendCycles);
	#endregion


	#region runtime variables
	readonly PlacedObject placedObject;
	readonly ManagedData data;
	private FadeOut fadeOut;
	private State state = State.awaiting;
	//vanilla worldloading happens during a few ticks, and then immediately changes world
	//so i want to load world prematurely and only utilize it afterwards
	private WorldLoader worldLoader;
	private Thread thread;
	private ThreadedLoading threadedLoading;

	enum State
	{
		awaiting,
		fadein,
		awaitingWorld,
		awaitingTransition
	}
    #endregion
    public override void Update(bool eu)
	{
		base.Update(eu);
		switch (state)
		{
			case State.awaiting:
				if (room.PlayersInRoom.Any(realizedPlayerInRoom => realizedPlayerInRoom is not null
				&& PositionWithinPoly(TriggerZone, realizedPlayerInRoom.mainBodyChunk.pos)))
				{
					room.game.cameras[0].EnterCutsceneMode(room.PlayersInRoom[0].abstractCreature, RoomCamera.CameraCutsceneType.EndingOE);
					fadeOut = new FadeOut(room, Color.black, duration: TimeToFadeIn, fadeIn: false);
					room.AddObject(fadeOut);
					threadedLoading = new(this, room, Acronym);
					thread = new Thread(new ThreadStart(threadedLoading.Load));
					thread.Start();
					state = State.fadein;
				}
				break;

			case State.fadein:
				if (fadeOut.fade >= 1f)
				{
					state = State.awaitingWorld;
				}
				break;

			case State.awaitingWorld:
				if (worldLoader is not null
					&& worldLoader.ReturnWorld() is not null)
				{
					OverWorld overWorld = room.game.overWorld;
					customLoader.Add(overWorld, new((worldLoader, this, TargetRoom)));
					worldLoader = null;
					state = State.awaitingTransition;
				}
				break;

			case State.awaitingTransition:
				break;
		}
		
		
	}
	public void OnRoomChange(AbstractRoom destinationRoom)
	{
		room.updateList.Remove(this);
		destinationRoom.realizedRoom.AddObject(this);
		room = destinationRoom.realizedRoom;

		fadeOut = new FadeOut(room, Color.black, TimeToFadeOut, true);
		room.AddObject(fadeOut);
		OnTeleportationEnd();
	}
	public void OnTeleportationEnd()
	{
		room.game.GetStorySession.saveState.cycleNumber += SubtractCycles;
		if (ForceDenSwitch)
		{
			RainWorldGame.ForceSaveNewDenLocation(room.game, room.abstractRoom.name, true);
		}
		else if (SubtractCycles > 0)
		{
			room.game.GetStorySession.saveState.progression.SaveWorldStateAndProgression(false);
		}
		room.world.rainCycle.timer += (int)(SubtractCycleTime * room.world.rainCycle.cycleLength);

		slatedForDeletetion = true;
	}

	class ThreadedLoading(Warp warp, Room room, string targetRegionAcronym)
	{
		private readonly Room room = room;
		private readonly Warp warp = warp;
		private readonly string acronym = targetRegionAcronym;

        public void Load()
		{
			WorldLoader worldLoader = new(game: room.game,
                        playerCharacter: room.game.GetStorySession.characterStats.name,
                        timelinePosition: SlugcatStats.SlugcatToTimeline(room.game.StoryCharacter),
						singleRoomWorld: false,
						//this may be wrong, maybe there is no need to wrap
						worldName: Region.GetProperRegionAcronym(SlugcatStats.SlugcatToTimeline(room.game.StoryCharacter), acronym),

                        region: room.game.overWorld.GetRegion(acronym),
						setupValues: room.game.setupValues);
			worldLoader.NextActivity();
			while (!worldLoader.Finished)
			{
				worldLoader.Update();
			}
			warp.worldLoader = worldLoader;
		}
	}

	class WarpDestination : UpdatableAndDeletable
	{
		public static void Register()
		{
			RegisterFullyManagedObjectType([], typeof(WarpDestination), "Warp Destination", "The Void");
		}
		public WarpDestination(Room room, PlacedObject pObj)
		{
			pobj = pObj;
		}
		readonly PlacedObject pobj;
		public Vector2 Pos => pobj.pos;
	}
}