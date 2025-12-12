using RWCustom;
using System.Linq;
using UnityEngine;

namespace VoidTemplate.Creatures.VoidDaddyAdnProtoViy
{
    public static class ProtoViyDaddyAI
    {
        public static void Hook()
        {
            On.DaddyAI.Update += DaddyAI_Update;
        }

        private static void DaddyAI_Update(On.DaddyAI.orig_Update orig, DaddyAI self)
        {
            orig(self);

            if (self.daddy == null || self.daddy.room == null)
                return;

            if (!self.daddy.GetDaddyExt().IsProtoViy)
                return;

            var room = self.daddy.room;
            var game = room.game;
            if (game == null)
                return;

            for (int i = 0; i < game.Players.Count; i++)
            {
                AbstractCreature absPlayer = game.Players[i];
                if (absPlayer == null)
                    continue;

                if (absPlayer.state != null && absPlayer.state.dead)
                    continue;

                self.tracker.SeeCreature(absPlayer);
            }

            if (self.preyTracker != null)
            {
                self.preyTracker.giveUpOnUnreachablePrey = -1;
            }

            var prey = self.preyTracker?.MostAttractivePrey;
            if (prey != null)
            {
                var dest = prey.lastSeenCoord;

                if (self.pathFinder != null && self.pathFinder.CoordinateReachableAndGetbackable(dest))
                {
                    self.creature.abstractAI.SetDestination(dest);
                }
            }

            BoostTentacles(self);
        }

        private static void BoostTentacles(DaddyAI ai)
        {
            var daddy = ai.daddy;
            if (daddy?.tentacles == null || daddy.tentacles.Length == 0)
                return;

            var prey = ai.preyTracker?.MostAttractivePrey;
            if (prey == null)
                return;

            var guess = prey.BestGuessForPosition();
            if (guess.room != ai.creature.pos.room)
                return;

            Vector2 guessPos = daddy.room.MiddleOfTile(guess);

            for (int i = 0; i < daddy.tentacles.Length; i++)
            {
                var t = daddy.tentacles[i];

                if (!t.neededForLocomotion && t.huntCreature == null)
                {
                    t.huntCreature = prey;
                    t.idealLength = Mathf.Min(t.idealLength * 1.15f, 220f);
                }

                if (t.huntCreature == prey && t.tChunks != null && t.tChunks.Length > 0)
                {
                    var tip = t.tChunks[t.tChunks.Length - 1];

                    Vector2 targetPos;

                    if (prey.representedCreature?.realizedCreature != null &&
                        prey.representedCreature.realizedCreature.room == daddy.room)
                    {
                        targetPos = prey.representedCreature.realizedCreature.mainBodyChunk.pos;
                    }
                    else
                    {
                        targetPos = guessPos;
                    }

                    var dir = (targetPos - tip.pos).normalized;
                    tip.vel += dir * 0.8f;
                }
            }
        }
    }
}
