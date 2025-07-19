using System;
using UnityEngine;
using VoidTemplate;
using VoidTemplate.Objects;
using VoidTemplate.Useful;

public static class PlayerSpawnManager
{
	public static void ApplyHooks()
	{
		On.Player.Update += Player_Update;
		On.RainCycle.ctor += RainCycle_ctor;
        //On.RainWorldGame.Update += RainWorldGame_Update;
        //On.RainWorldGame.Update += RainWorldGame_Update2;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
	{
		orig(self, eu);
		if (self.room is Room playerRoom
			&& playerRoom.game.IsStorySession
			&& playerRoom.game.GetStorySession.saveStateNumber == VoidEnums.SlugcatID.Void
			&& playerRoom.game.GetStorySession.saveState is SaveState save
			&& !save.GetTeleportationDone())
		{
			if (playerRoom.game.IsVoidStoryCampaign())
			{
                InitializeTargetRoomID(playerRoom);
            }
			
            int currentRoomIndex = self.abstractCreature.pos.room;

			if (currentRoomIndex == NewSpawnPoint.room)
			{
				save.SetTeleportationDone(true);
				self.abstractCreature.pos = NewSpawnPoint;
				Vector2 newPosition = self.room.MiddleOfTile(NewSpawnPoint.x, NewSpawnPoint.y);
				Array.ForEach(self.bodyChunks, x => x.pos = newPosition);
				self.standing = true;
				self.animation = Player.AnimationIndex.StandUp;
			}
		}
    }

    private static void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
    {
        orig(self, world, minutes);
		if (world.game != null)
		{
			if (world.game.GetStorySession != null)
			{
				if (world.game.GetStorySession.saveState.cycleNumber == 0 && world.game.IsVoidWorld())
				{
					self.cycleLength = 11 * 60 * 40;
				}
				if (world.name == "MS" && world.game.IsVoidWorld())
				{
					int minute = UnityEngine.Random.Range(11, 16);
					self.cycleLength = minute * 60 * 40;
				}
			}
		}
    }

    #region minor helper functions

    private static int targetRoomID = -1;
	static WorldCoordinate NewSpawnPoint
	{
		get
		{
			if (targetRoomID == -1) throw new Exception("Target room ID is not initialized!");
			return new WorldCoordinate(targetRoomID, originalSpawnPoint.x, originalSpawnPoint.y, originalSpawnPoint.abstractNode);
		}
	}

	private static readonly WorldCoordinate originalSpawnPoint = new WorldCoordinate(-1, 27, 13, 0);

	static void InitializeTargetRoomID(Room room)
	{
		if (targetRoomID == -1)
		{
			AbstractRoom targetRoom = room.world.GetAbstractRoom("SB_A14") ?? throw new Exception($"Room 'SB_A14' does not exist.");
			targetRoomID = targetRoom.index;
		}
	}

    static bool prevPressed = false;
    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (Input.GetKey(KeyCode.H) && !prevPressed)
        {
            _ = new VoidTemplate.Objects.KarmaRotator(self.Players[0].Room.realizedRoom);
        }
        prevPressed = Input.GetKey(KeyCode.H);
    }

    private static void RainWorldGame_Update2(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (Input.GetKey(KeyCode.H) && !prevPressed)
        {
			if (self.Players[1].realizedCreature is Player player)
            HunterSpasms.Spasm(player, 5f, 1f);
        }
        prevPressed = Input.GetKey(KeyCode.H);
    }
    #endregion
}