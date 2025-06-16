using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

internal static class ViyBitByPlayer
{
    public static void Hook()
    {
        On.DangleFruit.BitByPlayer += DangleFruit_BitByPlayer;
        On.JellyFish.BitByPlayer += JellyFish_BitByPlayer;
        On.VultureGrub.BitByPlayer += VultureGrub_BitByPlayer;
        On.EggBugEgg.BitByPlayer += EggBugEgg_BitByPlayer;
        On.Fly.BitByPlayer += Fly_BitByPlayer;
        On.OracleSwarmer.BitByPlayer += OracleSwarmer_BitByPlayer;
        On.SwollenWaterNut.BitByPlayer += SwollenWaterNut_BitByPlayer;
        On.SlimeMold.BitByPlayer += SlimeMold_BitByPlayer;
        On.Hazer.BitByPlayer += Hazer_BitByPlayer;
        On.Centipede.BitByPlayer += Centipede_BitByPlayer;
        On.MoreSlugcats.DandelionPeach.BitByPlayer += DandelionPeach_BitByPlayer;
        On.MoreSlugcats.GlowWeed.BitByPlayer += GlowWeed_BitByPlayer;
        On.MoreSlugcats.LillyPuck.BitByPlayer += LillyPuck_BitByPlayer;
        On.MoreSlugcats.GooieDuck.BitByPlayer += GooieDuck_BitByPlayer;
        On.MoreSlugcats.FireEgg.BitByPlayer += FireEgg_BitByPlayer;
    }

    private static void DangleFruit_BitByPlayer(On.DangleFruit.orig_BitByPlayer orig, DangleFruit self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void JellyFish_BitByPlayer(On.JellyFish.orig_BitByPlayer orig, JellyFish self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Jelly_Fish, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void VultureGrub_BitByPlayer(On.VultureGrub.orig_BitByPlayer orig, VultureGrub self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void EggBugEgg_BitByPlayer(On.EggBugEgg.orig_BitByPlayer orig, EggBugEgg self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void Fly_BitByPlayer(On.Fly.orig_BitByPlayer orig, Fly self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void OracleSwarmer_BitByPlayer(On.OracleSwarmer.orig_BitByPlayer orig, OracleSwarmer self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Swarmer, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            if (!ModManager.MSC || !(grasp.grabber as Player).isNPC)
            {
                if (self.room.game.session is StoryGameSession)
                {
                    (self.room.game.session as StoryGameSession).saveState.theGlow = true;
                }
            }
            else
            {
                ((grasp.grabber as Player).State as PlayerNPCState).Glowing = true;
            }
            (grasp.grabber as Player).glowing = true;
            grasp.Release();
            self.Destroy();
        }
    }

    private static void SwollenWaterNut_BitByPlayer(On.SwollenWaterNut.orig_BitByPlayer orig, SwollenWaterNut self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Water_Nut, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void SlimeMold_BitByPlayer(On.SlimeMold.orig_BitByPlayer orig, SlimeMold self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Slime_Mold, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void Hazer_BitByPlayer(On.Hazer.orig_BitByPlayer orig, Hazer self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void Centipede_BitByPlayer(On.Centipede.orig_BitByPlayer orig, Centipede self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.bites -= 2;
        }
        orig(self, grasp, eu);
    }

    private static void DandelionPeach_BitByPlayer(On.MoreSlugcats.DandelionPeach.orig_BitByPlayer orig, DandelionPeach self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Water_Nut, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void GlowWeed_BitByPlayer(On.MoreSlugcats.GlowWeed.orig_BitByPlayer orig, GlowWeed self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void LillyPuck_BitByPlayer(On.MoreSlugcats.LillyPuck.orig_BitByPlayer orig, LillyPuck self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }

    private static void GooieDuck_BitByPlayer(On.MoreSlugcats.GooieDuck.orig_BitByPlayer orig, GooieDuck self, Creature.Grasp grasp, bool eu)
    {
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
            return;
        }
        orig(self, grasp, eu);
    }

    private static void FireEgg_BitByPlayer(On.MoreSlugcats.FireEgg.orig_BitByPlayer orig, FireEgg self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (grasp.grabber is Player player && player.IsViy())
        {
            self.room.PlaySound(SoundID.Slugcat_Eat_Dangle_Fruit, self.firstChunk.pos);
            (grasp.grabber as Player).ObjectEaten(self);
            grasp.Release();
            self.Destroy();
        }
    }
}
