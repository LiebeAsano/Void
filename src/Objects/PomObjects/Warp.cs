using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
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
			new StringField(targetRoomName, "SS_D08")
			];
		RegisterFullyManagedObjectType(exposedFields, typeof(Warp), category: "The Void");
		WarpDestination.Register();
	}

	#region exposed data IDs
	const string triggerZone = "trigger zone";
	const string targetRoomName = "TargetRoomName";
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
		awaitingPOMLoad,
		over
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
					state = State.fadein;
					room.game.cameras[0].EnterCutsceneMode(room.PlayersInRoom[0].abstractCreature, RoomCamera.CameraCutsceneType.EndingOE);
					fadeOut = new FadeOut(room, Color.black, duration: 60f, fadeIn: false);
					room.AddObject(fadeOut);

					threadedLoading = new(this, room, Acronym);
					thread = new Thread(new ThreadStart(threadedLoading.Load));
					thread.Start();
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
					overWorld.worldLoader = worldLoader;
					worldLoader = null;
					AbstractRoom destinationRoom = WorldLoaded(overWorld);

					destinationRoom.realizedRoom.AddObject(this);
					room = destinationRoom.realizedRoom;

					fadeOut = new FadeOut(room, Color.black, 60f, true);
					room.AddObject(fadeOut);
					state = State.awaitingPOMLoad;
				}
				break;

			case State.awaitingPOMLoad:

				WarpDestination warpDestination = null;
				if (room.updateList.Exists(UAD =>
				{
					if (UAD is WarpDestination wDest)
					{
						warpDestination = wDest;
						return true;
					}
					return false;
				}))
				{
					List<AbstractCreature> players = room.game.Players;
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

					foreach (var camera in room.game.cameras)
					{
						camera.GetCameraBestIndex();
					}

					state = State.over;
					slatedForDeletetion = true;
				}
				break;
		}
		//if you dare to use this method, be aware that players realize at (10;10) coordinate
		//it used to be (0;0), which really threw off camera logic
		//after using this, change slugcat positions and apply camera change
		AbstractRoom WorldLoaded(OverWorld overWorld)
		{
			World world = overWorld.activeWorld;
			World world2 = overWorld.worldLoader.ReturnWorld();
			AbstractRoom abstractRoom2 = null;
			abstractRoom2 = world2.GetAbstractRoom(TargetRoom);
			overWorld.activeWorld = world2;
			if (overWorld.game.roomRealizer != null)
			{
				overWorld.game.roomRealizer = new RoomRealizer(overWorld.game.roomRealizer.followCreature, world2);
			}

			abstractRoom2.RealizeRoom(world2, overWorld.game);

			foreach (AbstractCreature absPly in overWorld.game.Players)
			{
				world.GetAbstractRoom(absPly.pos).RemoveEntity(absPly);
				absPly.world = world2;
				WorldCoordinate worldCoordinate = new(abstractRoom2.index, 10, 10, -1);
				absPly.pos = worldCoordinate;
				abstractRoom2.AddEntity(absPly);
				if (absPly.realizedCreature is Player p
					&& p.room is not null)
				{
					p.room.RemoveObject(p);
					p.PlaceInRoom(abstractRoom2.realizedRoom);
					if (p.objectInStomach is not null) p.objectInStomach.world = world2;
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
			if (ModManager.MMF)
			{
				GC.Collect();
			}
			return abstractRoom2;
		}
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
						singleRoomWorld: false,
						//this may be wrong, maybe there is no need to wrap
						worldName: Region.GetProperRegionAcronym(room.game.StoryCharacter, acronym),

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