using RWCustom;
using UnityEngine;

namespace VoidTemplate.Creatures.VoidDaddyAndProtoViy

{
    public static class ProtoViyAbstractAI
    {
        public static void Hook()
        {
            On.AbstractCreatureAI.Update += AbstractCreatureAI_Update;
        }

        private static void AbstractCreatureAI_Update(On.AbstractCreatureAI.orig_Update orig, AbstractCreatureAI self, int time)
        {
            if (self.world?.game?.overWorld?.worldLoader != null
                && !self.world.game.overWorld.worldLoader.Finished
                && self.world.game.overWorld.worldLoader.world == self.world)
            {
                return;
            }

            bool isProtoViy = false;
            AbstractCreature followTarget = null;

            if (self.parent?.state is DaddyLongLegs.DaddyState dState)
            {
                var ext = dState.GetDaddyExt();
                if (ext.IsProtoViy)
                {
                    isProtoViy = true;

                    var game = self.world.game;
                    if (game != null && game.Players != null)
                    {
                        for (int i = 0; i < game.Players.Count; i++)
                        {
                            var ac = game.Players[i];
                            if (ac != null && ac.state != null && !ac.state.dead)
                            {
                                followTarget = ac;
                                break;
                            }
                        }
                    }

                    if (followTarget != null)
                    {
                        self.followCreature = followTarget;

                        if (!self.MigrationDestination.CompareDisregardingTile(followTarget.pos))
                        {
                            self.SetDestination(followTarget.pos);
                        }
                    }
                }
            }

            orig(self, time);

            if (isProtoViy && self.parent.Room.realizedRoom == null && followTarget != null)
            {
                const int EXTRA_ABSTRACT_STEPS = 3;

                for (int i = 0; i < EXTRA_ABSTRACT_STEPS; i++)
                {
                    self.followCreature = followTarget;
                    self.AbstractBehavior(time);
                }
            }
        }
    }
}
