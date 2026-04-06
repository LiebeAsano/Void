using System.Runtime.CompilerServices;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions
{
    public static class LoachEaten
    {
        private sealed class LoachState
        {
            public int killTimer;
        }

        private static readonly ConditionalWeakTable<Watcher.Loach, LoachState> loachStates = new();

        private const int KillDelay = 6 * 40;

        public static void Hook()
        {
            On.Watcher.Loach.Eat += Loach_Eat;
            On.Watcher.Loach.Update += Loach_Update;
        }

        private static void Loach_Eat(On.Watcher.Loach.orig_Eat orig, Watcher.Loach self, bool eu)
        {
            orig(self, eu);

            if (self.eatObjects == null)
                return;

            foreach (var eatObject in self.eatObjects)
            {
                if (eatObject.progression > 1f &&
                    eatObject.chunk?.owner is Player player &&
                    player.IsVoid())
                {
                    player.Destroy();
                    loachStates.GetOrCreateValue(self).killTimer = KillDelay;
                    break;
                }
            }
        }

        private static void Loach_Update(On.Watcher.Loach.orig_Update orig, Watcher.Loach self, bool eu)
        {
            orig(self, eu);

            if (!loachStates.TryGetValue(self, out var state) || state.killTimer <= 0)
                return;

            if (self.slatedForDeletetion || self.room == null || self.dead)
            {
                state.killTimer = 0;
                return;
            }

            state.killTimer--;

            if (state.killTimer <= 0)
            {
                self.Die();
            }
        }
    }
}
