using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

static class EdibleChanges
{
	public static void Hook()
	{
		On.Mushroom.BitByPlayer += Mushroom_EatenByPlayer;
	}


	private static void Mushroom_EatenByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
	{
		if (grasp.grabber is Player player && (player.IsVoid() || player.IsViy()))
		{
			self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
			grasp.Release();
			self.Destroy();
			return;
		}
		orig(self, grasp, eu);
	}
}
