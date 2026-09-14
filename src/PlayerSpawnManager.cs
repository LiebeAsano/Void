using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.Useful;

namespace VoidTemplate;

public static class PlayerSpawnManager
{
	public static void ApplyHooks()
	{
        On.Player.NewRoom += Player_NewRoom;
        On.RainCycle.ctor += RainCycle_ctor;
        //On.RainWorldGame.Update += RainWorldGame_Update;
        //On.RainWorldGame.Update += RainWorldGame_Update2;
    }
    private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
    {
        orig(self, newRoom);

        if (!newRoom.game.IsStorySession ||
            newRoom.game.GetStorySession.saveStateNumber != VoidEnums.SlugcatID.Void ||
            newRoom.game.rainWorld.ExpeditionMode)
            return;

        SaveState save = newRoom.game.GetStorySession.saveState;

        if (save.GetTeleportationDone() || !newRoom.game.IsVoidStoryCampaign())
            return;

        AbstractRoom targetRoom = newRoom.world.GetAbstractRoom("SB_A14");

        if (targetRoom == null || newRoom.abstractRoom.index != targetRoom.index)
            return;

        WorldCoordinate spawnPoint = new(targetRoom.index, originalSpawnPoint.x, originalSpawnPoint.y, originalSpawnPoint.abstractNode);

        save.SetTeleportationDone(true);
        SaveManager.ExternalSaveData.VoidPermaNightmare = 0;

        self.abstractCreature.pos = spawnPoint;
        self.SuperHardSetPosition(newRoom.MiddleOfTile(spawnPoint.x, spawnPoint.y));
        self.standing = true;
        self.animation = Player.AnimationIndex.StandUp;
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

	private static readonly WorldCoordinate originalSpawnPoint = new(-1, 27, 13, 0);

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
			if (self.Players[0].realizedCreature is Player player)
            HunterSpasms.Spasm(player, 10f, 1f);
        }
        prevPressed = Input.GetKey(KeyCode.H);
    }
    #endregion
}