using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using VoidTemplate.Useful;
using static Pom.Pom;
using static VoidTemplate.Useful.POMUtils;

namespace VoidTemplate.Objects.PomObjects;

public class Warp : UpdatableAndDeletable
{
	public static void Register()
	{
		ManagedField[] exposedFields = [
			defaultVectorField,
			new StringField(targetRoomName, "SS_D08"),
			new FloatField(timeToFadeIn, 1f, 200f, 60f, increment: 1f, displayName: "fadein time"),
			new FloatField(timeToFadeOut, 1f, 200f, 60f, increment: 1f, displayName: "fadeout time"),
			new BooleanField(forceSpawningAtTarget, false, displayName: "force new den"),
			new FloatField(cycleTime, 0f, 1f, 0f, 0.05f, displayName: "subtract time"),
			new IntegerField(spendCycles, 0, 15, 0, displayName: "spend cycles"),
			new BooleanField(exitInShortcut, false, displayName: "exit in shortcut"),
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
		    && self.abstractRoom.name == self.game.GetStorySession.saveState.denPosition
		    && self.updateList.OfType<WarpDestination>().FirstOrDefault() is WarpDestination destination)
		{
			for (int i = 0; i < self.game.Players.Count; i++)
			{
				Player p = self.game.Players[i].realizedCreature as Player;
				p.SuperHardSetPosition(destination.Pos + new Vector2(20f * i, 0f));
				p.standing = true;
			}
		}
	}

	class CustomLoader
	{
		public WorldLoader worldLoader;
		public Warp warp;
		public string targetRoom;
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
	static readonly ConditionalWeakTable<OverWorld, CustomLoader> customLoader = new();
	private static void OverWorld_Update(On.OverWorld.orig_Update orig, OverWorld self)
	{
		orig(self);
		if(customLoader.TryGetValue(self, out var loader))
		{

			AbstractRoom absRoom = WorldLoaded(self, loader);
			loader.warp.OnRoomChange(absRoom);
			customLoader.Remove(self);
			GC.Collect();
		}


		//if you dare to use this method, be aware that players realize at (10;10) coordinate
		//it used to be (0;0), which really threw off camera logic
		//after using this, change slugcat positions and apply camera change
		AbstractRoom WorldLoaded(OverWorld overWorld, CustomLoader targetingData)
		{
			World world = overWorld.activeWorld;
			World newWorld = targetingData.worldLoader.ReturnWorld();
			AbstractRoom targetAbstractRoom = null;
			targetAbstractRoom = newWorld.GetAbstractRoom(targetingData.targetRoom);
			overWorld.activeWorld = newWorld;
			if (overWorld.game.roomRealizer != null)
			{
				overWorld.game.roomRealizer = new RoomRealizer(overWorld.game.roomRealizer.followCreature, newWorld);
			}

			targetAbstractRoom.RealizeRoom(newWorld, overWorld.game);
			WorldCoordinate newWorldCoordinate = new(targetAbstractRoom.index, 10, 10, -1);

			foreach (AbstractCreature absPly in overWorld.game.Players)
			{
				absPly.world.GetAbstractRoom(absPly.pos).RemoveEntity(absPly);
				absPly.world = newWorld;
				absPly.pos = newWorldCoordinate;
				absPly.world.GetAbstractRoom(newWorldCoordinate).AddEntity(absPly);
#nullable enable
				ShortcutHandler.ShortCutVessel? containingVessel = overWorld.game.shortcuts.transportVessels.FirstOrDefault(x => x.creature == absPly.realizedCreature);
#nullable disable

				switch (targetingData.warp.ExitInShortcut, containingVessel)
				{
					case (false, null): //when player is not in shortcut, as intended
						break;
					case (false, not null): //when player is in shortcut and should not be
						containingVessel.room = targetAbstractRoom;
						containingVessel.pos = new(newWorldCoordinate.x, newWorldCoordinate.y);
						overWorld.game.shortcuts.SpitOutCreature(containingVessel);
						break;
					case (true, null): //when player is not in shortcut and should be
						overWorld.game.shortcuts.transportVessels.Add(
							new ShortcutHandler.ShortCutVessel(new(newWorldCoordinate.x, newWorldCoordinate.y), absPly.realizedCreature, targetAbstractRoom, 0)
							);
						absPly.realizedCreature.inShortcut = true;
						break;
					case (true, not null): //when player is in shortcut, as intended

						break;
				}
				switch (targetingData.warp.ExitInShortcut)
				{
					case false
						when absPly.realizedCreature is Player { room: not null } p:
					{
						p.room.RemoveObject(p);
						p.PlaceInRoom(targetAbstractRoom.realizedRoom);
						p.standing = true;

						if (p.objectInStomach is not null) p.objectInStomach.world = newWorld;

						foreach (Creature.Grasp grasp in p.grasps)
						{
							if (grasp is not null)
							{
								AbstractPhysicalObject grabbedAbstractPhysicalObject = grasp.grabbed.abstractPhysicalObject;
								grabbedAbstractPhysicalObject.world.GetAbstractRoom(grabbedAbstractPhysicalObject.pos).RemoveEntity(grabbedAbstractPhysicalObject);
								grabbedAbstractPhysicalObject.world = newWorld;
								grabbedAbstractPhysicalObject.pos = newWorldCoordinate;
								grabbedAbstractPhysicalObject.world.GetAbstractRoom(newWorldCoordinate).AddEntity(grabbedAbstractPhysicalObject);
							}
						}

						break;
					}
					case true:
						//the attempt to guess where to put the vessel is way too expensive. i'll just rely on wDestination
						break;
				}
			}
			Room realizedDestination = targetAbstractRoom.realizedRoom;
			if (realizedDestination.updateList.OfType<WarpDestination>().FirstOrDefault() is WarpDestination warpDestination)
			{
                List<AbstractCreature> players = realizedDestination.game.Players;
                if (targetingData.warp.ExitInShortcut)
				{
					RWCustom.IntVector2 pos = realizedDestination.GetTilePosition(warpDestination.Pos);
                    IEnumerable<ShortcutHandler.ShortCutVessel> playerVessels = overWorld.game.shortcuts.transportVessels.Where(vessel => realizedDestination.game.Players.Any(absply => vessel.creature == absply.realizedCreature));
                    RWCustom.IntVector2 lastPosOffset = warpDestination.LastRelativePositionForShortcutVessel;
					foreach(ShortcutHandler.ShortCutVessel playerVessel in playerVessels)
					{
						playerVessel.room = targetAbstractRoom;
						playerVessel.pos = pos;
						playerVessel.lastPositions[0] = pos + lastPosOffset;
					}
				}
				else
				{
                    
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
			}
			else if (targetingData.warp.ExitInShortcut) 
				throw new EntryPointNotFoundException($"failed to locate exit point for Last Wish warp to {targetingData.targetRoom} while requesting it to be in shortcut");



			foreach (var camera in overWorld.game.cameras)
			{
				camera.virtualMicrophone.AllQuiet();

				camera.MoveCamera(targetAbstractRoom.realizedRoom, 0);
				camera.ApplyPositionChange();
				camera.GetCameraBestIndex();

				var hud = camera.hud;
				hud.ResetMap(new(newWorld, overWorld.game.rainWorld));
				camera.dayNightNeedsRefresh = true;
				if (hud.textPrompt.subregionTracker is not null) hud.textPrompt.subregionTracker.lastShownRegion = 0;

			}

			overWorld.worldLoader = null;

			if (world.regionState is not null) world.regionState.world = null;

			newWorld.rainCycle.baseCycleLength = world.rainCycle.baseCycleLength;
			newWorld.rainCycle.cycleLength = world.rainCycle.cycleLength;
			newWorld.rainCycle.timer = world.rainCycle.timer;
			newWorld.rainCycle.duskPalette = world.rainCycle.duskPalette;
			newWorld.rainCycle.nightPalette = world.rainCycle.nightPalette;
			newWorld.rainCycle.dayNightCounter = world.rainCycle.dayNightCounter;

			if (ModManager.MSC)
			{
				if (world.rainCycle.timer == 0)
				{
					newWorld.rainCycle.preTimer = world.rainCycle.preTimer;
					newWorld.rainCycle.maxPreTimer = world.rainCycle.maxPreTimer;
				}
				else
				{
					newWorld.rainCycle.preTimer = 0;
					newWorld.rainCycle.maxPreTimer = 0;
				}
			}

			if (!newWorld.activeRooms.Contains(targetAbstractRoom.realizedRoom)) newWorld.activeRooms.Add(targetAbstractRoom.realizedRoom);

			if (ModManager.MMF)
			{
				GC.Collect();
			}
			return targetAbstractRoom;
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
	const string exitInShortcut = "exitInShortcut";
	#endregion

	public Warp(Room room, PlacedObject pobj)
	{
		this.room = room;
		placedObject = pobj;
		data = (placedObject.data as ManagedData)!;
	}

	#region exposed fields
	Vector2[] TriggerZone => POMUtils.AddRealPosition(data.GetValue<Vector2[]>(triggerZone), placedObject.pos);
	string TargetRoom => data.GetValue<string>(targetRoomName)!;
	string Acronym => TargetRoom.Split('_')[0];
	float TimeToFadeIn => data.GetValue<float>(timeToFadeIn);
	float TimeToFadeOut => data.GetValue<float>(timeToFadeOut);
	bool ForceDenSwitch => data.GetValue<bool>(forceSpawningAtTarget);
	float SubtractCycleTime => data.GetValue<float>(cycleTime);
	int SubtractCycles => data.GetValue<int>(spendCycles);
	bool ExitInShortcut => data.GetValue<bool>(exitInShortcut);
	#endregion


	#region runtime variables
#nullable enable
	readonly PlacedObject placedObject;
	readonly ManagedData data;
	private FadeOut? fadeOut;
	private State state = State.Awaiting;
	//vanilla worldloading happens during a few ticks, and then immediately changes world
	//so i want to load world prematurely and only utilize it afterwards
	private WorldLoader? worldLoader;
	private Thread? thread;
	private ThreadedLoading? threadedLoading;
#nullable disable

	enum State
	{
		Awaiting,
		Fadein,
		AwaitingWorld,
		AwaitingTransition
	}
	#endregion
	public override void Update(bool eu)
	{
		base.Update(eu);
		switch (state)
		{
			case State.Awaiting:
				if (room.PlayersInRoom.Any(
					realizedPlayerInRoom => 
						realizedPlayerInRoom is not null
						&& PositionWithinPoly(TriggerZone, realizedPlayerInRoom.mainBodyChunk.pos))
					|| room.game.shortcuts.transportVessels.Any( 
						vessel => 
						vessel.room == room.abstractRoom 
						&& PositionWithinPoly(TriggerZone, room.MiddleOfTile(vessel.pos))))
				{
					room.game.cameras[0].EnterCutsceneMode(room.PlayersInRoom[0].abstractCreature, RoomCamera.CameraCutsceneType.EndingOE);
					fadeOut = new FadeOut(room, Color.black, duration: TimeToFadeIn, fadeIn: false);
					room.AddObject(fadeOut);
					threadedLoading = new ThreadedLoading(this, room, Acronym);
					thread = new Thread(threadedLoading.Load);
					thread.Start();
					state = State.Fadein;
				}
				break;

			case State.Fadein:
				if (fadeOut!.fade >= 1f)
				{
					state = State.AwaitingWorld;
				}
				break;

			case State.AwaitingWorld:
				if (worldLoader is not null
					&& worldLoader.ReturnWorld() is not null)
				{
					OverWorld overWorld = room.game.overWorld;
					customLoader.Add(overWorld, new CustomLoader()
					{
						worldLoader = worldLoader,
						warp = this,
						targetRoom = TargetRoom
					});
					worldLoader = null;
					state = State.AwaitingTransition;
				}
				break;

			case State.AwaitingTransition:
				break;
		}
		
		
	}

	void OnRoomChange(AbstractRoom destinationRoom)
	{
		room.updateList.Remove(this);
		destinationRoom.realizedRoom.AddObject(this);
		room = destinationRoom.realizedRoom;

		fadeOut = new FadeOut(room, Color.black, TimeToFadeOut, true);
		room.AddObject(fadeOut);
		OnTeleportationEnd();
	}

	void OnTeleportationEnd()
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
			RegisterFullyManagedObjectType([
			new EnumField<Direction>(directionKey, Direction.Down, control: ManagedFieldWithPanel.ControlType.button, displayName: "direction"),
				], typeof(WarpDestination), "Warp Destination", "The Void");
		}

		#region keys
		const string directionKey = "direction";
		#endregion

		#region variables

		public RWCustom.IntVector2 LastRelativePositionForShortcutVessel =>
			Dir switch
			{
				Direction.Up => new RWCustom.IntVector2(0, -1),
				Direction.Down => new RWCustom.IntVector2(0, 1),
				Direction.Left => new RWCustom.IntVector2(1, 0),
				Direction.Right => new RWCustom.IntVector2(-1, 0),
				_ => throw new ArgumentOutOfRangeException($"the direction {Dir} is not a supported enum type"),
			};

		Direction Dir => data.GetValue<Direction>(directionKey); 
		#endregion
		
		public WarpDestination(Room _, PlacedObject pObj)
		{
			pobj = pObj;
			data = pobj.data as ManagedData;
		}

		readonly ManagedData data;
		readonly PlacedObject pobj;
		public Vector2 Pos => pobj.pos;

		enum Direction
		{
			Up,
			Down,
			Left,
			Right
		}
	}
}