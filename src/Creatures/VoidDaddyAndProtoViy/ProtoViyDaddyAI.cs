using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace VoidTemplate.Creatures.VoidDaddyAndProtoViy

{
    public static class ProtoViyDaddyAI
    {
        private static readonly Dictionary<DaddyTentacle, bool> WasGrabbing = new();

        public static void Hook()
        {
            On.DaddyAI.Update += DaddyAI_Update;
            On.DaddyLongLegs.Update += DaddyLongLegs_Update;
            On.DaddyTentacle.Update += DaddyTentacle_Update;
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

            if (self.pathFinder != null)
            {
                if (self.pathFinder.stepsPerFrame < 80)
                    self.pathFinder.stepsPerFrame = 80;

                if (self.pathFinder.accessibilityStepsPerFrame < 40)
                    self.pathFinder.accessibilityStepsPerFrame = 40;
            }

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

        private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);

            if (self == null || self.room == null || !self.GetDaddyExt().IsProtoViy)
                return;

            var tentacles = self.tentacles;
            if (tentacles == null || tentacles.Length == 0)
                return;

            int legsGrabbing = 0;
            for (int i = 0; i < tentacles.Length; i++)
            {
                var t = tentacles[i];
                if (t != null && t.atGrabDest && t.grabDest.HasValue)
                    legsGrabbing++;
            }

            if (legsGrabbing > 0)
            {
                const int extraSteps = 6;

                for (int step = 0; step < extraSteps; step++)
                {
                    float bestScore = float.MinValue;
                    int bestIndex = -1;

                    for (int i = 0; i < tentacles.Length; i++)
                    {
                        var t = tentacles[i];
                        if (t == null)
                            continue;

                        if (!t.atGrabDest || t.huntCreature != null)
                            continue;

                        float score = t.ReleaseScore();

                        if (t.neededForLocomotion)
                            score *= 1.5f;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestIndex = i;
                        }
                    }

                    if (bestIndex < 0)
                        break;

                    List<IntVector2> path = null;
                    tentacles[bestIndex].UpdateClimbGrabPos(ref path);
                }
            }

            SpeedUpLocomotionTentacles(self);
        }

        private static void SpeedUpLocomotionTentacles(DaddyLongLegs daddy)
        {
            if (daddy.tentacles == null)
                return;

            for (int i = 0; i < daddy.tentacles.Length; i++)
            {
                var t = daddy.tentacles[i];
                if (t == null || t.tChunks == null || t.tChunks.Length == 0)
                    continue;

                if (!t.grabDest.HasValue)
                    continue;

                Vector2 target;
                if (t.floatGrabDest.HasValue)
                    target = t.floatGrabDest.Value;
                else
                    target = daddy.mainBodyChunk.pos;

                float force = t.neededForLocomotion ? 2.5f : 1.5f;
                float maxPullDist = 40f;

                for (int c = 0; c < t.tChunks.Length; c++)
                {
                    var chunk = t.tChunks[c];
                    Vector2 toDest = target - chunk.pos;
                    if (toDest.sqrMagnitude < 1f)
                        continue;

                    Vector2 pull = Vector2.ClampMagnitude(toDest, maxPullDist) / maxPullDist * force;
                    chunk.vel += pull;
                }
            }
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
                if (t == null)
                    continue;

                if (!t.neededForLocomotion && t.huntCreature == null)
                {
                    t.huntCreature = prey;
                    t.idealLength = Mathf.Min(t.idealLength * 1.25f, 240f);
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

                    float huntForce = 1f;
                    tip.vel += dir * huntForce;
                }
            }
        }

        private static void DaddyTentacle_Update(On.DaddyTentacle.orig_Update orig, DaddyTentacle self)
        {
            orig(self);

            if (self?.owner is not DaddyLongLegs daddy || daddy.room == null)
                return;

            if (!daddy.GetDaddyExt().IsProtoViy)
                return;

            BodyChunk grabbedChunk = self.grabChunk;
            bool currentlyGrabbing = grabbedChunk != null && grabbedChunk.owner is Creature;

            bool wasGrabbing = WasGrabbing.TryGetValue(self, out var prev) && prev;

            if (currentlyGrabbing)
            {
                var grabbedCreature = grabbedChunk.owner as Creature;

                if (grabbedCreature != null && !grabbedCreature.dead)
                {
                    if (!wasGrabbing)
                    {
                        Vector2 dir = (grabbedChunk.pos - daddy.mainBodyChunk.pos).normalized;

                        grabbedCreature.Violence(
                            daddy.mainBodyChunk,             
                            dir * 5f,                        
                            grabbedChunk,                   
                            null,
                            Creature.DamageType.Stab,        
                            0.75f,                            
                            0f                               
                        );

                        daddy.room.PlaySound(
                            SoundID.Spear_Stick_In_Creature,
                            grabbedChunk
                        );
                    }

                    grabbedCreature.stun = Mathf.Max(grabbedCreature.stun, 15);
                }

                if (grabbedCreature != null && grabbedCreature.dead)
                {
                    self.grabChunk = null;
                    grabbedChunk = null;
                    currentlyGrabbing = false;
                }
            }

            WasGrabbing[self] = currentlyGrabbing;
        }
    }
}
