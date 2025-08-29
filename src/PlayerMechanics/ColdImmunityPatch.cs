using MoreSlugcats;
using UnityEngine;
using static VoidTemplate.Useful.Utils;
namespace VoidTemplate.PlayerMechanics;

public static class ColdImmunityPatch
{
	public static void Hook()
	{
		On.Creature.HypothermiaUpdate += static (orig, self) =>
		{
            if (self is Player p && p.AreVoidViy())
            {
                self.HypothermiaGain = 0f;
                int karma = p.KarmaCap;
                if (p.KarmaCap == 10)
                    karma = 9;
                if (ModManager.DLCShared && self.room.blizzardGraphics != null && self.room.roomSettings.DangerType == DLCSharedEnums.RoomRainDangerType.Blizzard && self.room.world.rainCycle.CycleProgression > 0f)
                {
                    foreach (IProvideWarmth provideWarmth in self.room.blizzardHeatSources)
                    {
                        float num = Vector2.Distance(self.firstChunk.pos, provideWarmth.Position());
                        if (self.abstractCreature.Hypothermia > 0.001f && provideWarmth.loadedRoom == self.room && num < provideWarmth.range)
                        {
                            float num2 = Mathf.InverseLerp(provideWarmth.range, provideWarmth.range * 0.2f, num);
                            self.abstractCreature.Hypothermia -= Mathf.Lerp(provideWarmth.warmth * num2, 0f, self.HypothermiaExposure);
                            if (self.abstractCreature.Hypothermia < 0f)
                            {
                                self.abstractCreature.Hypothermia = 0f;
                            }
                        }
                    }
                    if (!self.dead)
                    {
                        self.HypothermiaGain = Mathf.Lerp(0f, RainWorldGame.DefaultHeatSourceWarmth * 0.1f, Mathf.InverseLerp(0.1f, 0.95f, self.room.world.rainCycle.CycleProgression));
                        if (!self.abstractCreature.HypothermiaImmune)
                        {
                            float num3 = (float)self.room.world.rainCycle.cycleLength + (float)RainWorldGame.BlizzardHardEndTimer(self.room.game.IsStorySession);
                            self.HypothermiaGain += Mathf.Lerp(0f, RainWorldGame.BlizzardMaxColdness, Mathf.InverseLerp(0f, num3, (float)self.room.world.rainCycle.timer));
                            self.HypothermiaGain += Mathf.Lerp(0f, 50f, Mathf.InverseLerp(num3, num3 * 5f, (float)self.room.world.rainCycle.timer));
                        }
                        Color blizzardPixel = self.room.blizzardGraphics.GetBlizzardPixel((int)(self.mainBodyChunk.pos.x / 20f), (int)(self.mainBodyChunk.pos.y / 20f));
                        self.HypothermiaGain += blizzardPixel.g / Mathf.Lerp(9100f, 5350f, Mathf.InverseLerp(0f, (float)self.room.world.rainCycle.cycleLength + 4300f, (float)self.room.world.rainCycle.timer));
                        self.HypothermiaGain += blizzardPixel.b / 8200f;
                        self.HypothermiaExposure = blizzardPixel.g;
                        if (self.Submersion >= 0.1f)
                        {
                            self.HypothermiaExposure = 1f * (1f - 0.5f * karma + 1);
                        }
                        self.HypothermiaGain += self.Submersion / 7000f;
                        self.HypothermiaGain = Mathf.Lerp(0f, self.HypothermiaGain, Mathf.InverseLerp(-0.5f, self.room.game.IsStorySession ? 1f : 3.6f, self.room.world.rainCycle.CycleProgression));
                        self.HypothermiaGain *= Mathf.InverseLerp(50f, -10f, self.TotalMass);
                    }
                    else
                    {
                        self.HypothermiaExposure = 1f * (1f - 0.5f * karma + 1);
                        self.HypothermiaGain = Mathf.Lerp(0f, 4E-05f, Mathf.InverseLerp(0.8f, 1f, self.room.world.rainCycle.CycleProgression));
                        self.HypothermiaGain += self.Submersion / 6000f;
                        self.HypothermiaGain += Mathf.InverseLerp(50f, -10f, self.TotalMass) / 1000f;
                    }
                    if (self.Hypothermia > 1.5f)
                    {
                        self.HypothermiaGain *= 2.3f;
                    }
                    else if (self.Hypothermia > 0.8f)
                    {
                        self.HypothermiaGain *= 0.5f;
                    }
                    if (self.abstractCreature.HypothermiaImmune)
                    {
                        self.HypothermiaGain /= 80f;
                    }
                    self.HypothermiaGain = Mathf.Clamp(self.HypothermiaGain, -1f, 0.0055f);
                    self.Hypothermia += self.HypothermiaGain * (1f - 0.5f * karma + 1);
                    if (self.Hypothermia >= 0.8f && self.Consious && self.room != null && !self.room.abstractRoom.shelter)
                    {
                        if (self.HypothermiaGain > 0.0003f)
                        {
                            if (self.HypothermiaStunDelayCounter < 0)
                            {
                                int st = (int)Mathf.Lerp(5f, 60f, Mathf.Pow(self.Hypothermia / 2f, 8f));
                                self.HypothermiaStunDelayCounter = (int)Random.Range(300f - self.Hypothermia * 120f, 500f - self.Hypothermia * 100f);
                                self.Stun(st);
                            }
                        }
                        else
                        {
                            self.HypothermiaStunDelayCounter = Random.Range(200, 500);
                        }
                    }
                }
                else
                {
                    if (self.Hypothermia > 2f)
                    {
                        self.Hypothermia = 2f;
                    }
                    self.Hypothermia = Mathf.Lerp(self.Hypothermia, 0f, 0.001f);
                    self.HypothermiaExposure = 0f;
                }
                if (self.room != null && !self.room.abstractRoom.shelter)
                {
                    self.HypothermiaStunDelayCounter--;
                }
                return;
            }
            orig(self);
        };

        On.Player.Update += Player_Update;
		/*On.Creature.HypothermiaBodyContactWarmup += static (orig, self, otherself, other) =>
		{
			bool result = orig(self, otherself, other);
			if (self is Player player && player.AreVoidViy()) result = true;
			return result;
		};*/
	}

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.AreVoidViy() && self.Hypothermia > 0)
        {
            int karma = self.KarmaCap;
            if (self.KarmaCap == 10)
                karma = 9;
            self.Hypothermia -= 0.00025f * 40 * 0.1f * (karma + 1);
        }
        orig(self, eu);
    }
}