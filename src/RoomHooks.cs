using Kittehface.Build;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System.Linq;
using VoidTemplate.Objects;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate
{
    static class RoomHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Room.Loaded += RoomSpeficScript;
            On.TempleGuardAI.Update += TempleGuardAI_Update;
            On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
            On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
            On.World.ctor += World_ctor;
            IL.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OE_GourmandEnding_Update;
        }

        private static void OE_GourmandEnding_Update(ILContext il)
        {
            ILCursor c = new(il);
            for (int i = 0; i < 3; i++)
            {
                if (c.TryGotoNext(MoveType.After, x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((bool orig, MSCRoomSpecificScript.OE_GourmandEnding self) =>
                    {
                        return orig || self.room.game.IsVoidStoryCampaign();
                    });
                    LogExInf(il.ToString());
                }
                else logerr($"{nameof(VoidTemplate)}.{nameof(RoomHooks)}.{nameof(OE_GourmandEnding_Update)}: {i} match error");
            }
            if (c.TryGotoNext(MoveType.Before, x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToRedsGameOver))))
            {
                c.Emit(OpCodes.Dup);
                c.EmitDelegate((RainWorldGame game) =>
                {
                    if (game.IsVoidStoryCampaign() && GameFeatures.OutroScene.TryGet(game, out var outro))
                    {
                        game.manager.nextSlideshow = outro;
                    }
                });
            }
            else logerr($"{nameof(VoidTemplate)}.{nameof(RoomHooks)}.{nameof(OE_GourmandEnding_Update)}: 4 match error");
        }

        private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
        {
            orig(self, game, region, name, singleRoomWorld);
            if (name == "OE" && game != null && game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void && !session.saveState.GetOEUnlockForVoid())
            {
                session.saveState.SetOEUnlockForVoid(true);
            }
        }

        private static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
        {
            return orig(self) || (self.room.game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void && session.saveState.GetOEUnlockForVoid());
        }

        private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (crit.representedCreature.realizedCreature is Player player && player.IsVoid())
            {
                return 500f;
            }
            return orig(self, crit);
        }

        private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
        {
            orig(self);
            if (self.guard.room.PlayersInRoom.Any(i => i.IsVoid()))
            {
                self.patience = 9999;
            }
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
        }

        private static void RoomSpeficScript(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.game != null && self.game.session.characterStats.name == VoidEnums.SlugcatID.Void)
            {
                SaveState saveState = self.game.GetStorySession.saveState;
                if (!saveState.GetCeilClimbMessageShown() && self.game.Players.Exists(x => x.realizedCreature is Player p && p.IsVoid() && p.KarmaCap == 4))
                {
                    self.AddObject(new Tutorial(self,
                    [
                        new("Your body is strong enough to climb on any surface.", 33, 333),
                        new("Your abilities also continue to improve.", 0, 333)
                    ]));
                    saveState.SetCeilClimbMessageShown(true);
                }
                switch (self.abstractRoom.name)
                {
                    case "SB_E05" when !saveState.GetStartClimbingMessageShown():
                        self.AddObject(new ClimbTutorial(self));
                        saveState.SetStartClimbingMessageShown(true);
                        break;
                    case VoidEnums.RoomNames.EndingRoomName:
                        self.AddObject(new Ending(self));
                        break;
                    case "SL_AI" when !saveState.GetDreamData(SaveManager.Dream.Moon).WasShown:
                        self.AddObject(new EnqueueMoonDream(self));
                        break;
                }

            }
        }

    }
}
