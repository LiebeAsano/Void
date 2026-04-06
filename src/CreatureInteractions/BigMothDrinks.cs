using System.Runtime.CompilerServices;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class BigMothDrinks
{
    private sealed class MothState
    {
        public int killTimer;
    }

    private static readonly ConditionalWeakTable<Watcher.BigMoth, MothState> states = new();

    public static void Hook()
    {
        On.Watcher.BigMoth.DrinkChunk += BigMoth_DrinkChunk;
        On.Watcher.BigMoth.Update += BigMoth_Update;
    }

    private static void BigMoth_DrinkChunk(On.Watcher.BigMoth.orig_DrinkChunk orig, Watcher.BigMoth self)
    {
        orig(self);

        if (self.drinkChunk?.owner is Player player && player.IsVoid())
        {
            states.GetOrCreateValue(self).killTimer = 6 * 40;
        }
    }

    private static void BigMoth_Update(On.Watcher.BigMoth.orig_Update orig, Watcher.BigMoth self, bool eu)
    {
        orig(self, eu);

        if (!states.TryGetValue(self, out var state) || state.killTimer <= 0)
            return;

        if (self.slatedForDeletetion || self.room == null)
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