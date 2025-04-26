using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

internal static class BigMothDrinks
{
    public static void Hook()
    {
        On.Watcher.BigMoth.DrinkChunk += BigMoth_DrinkChunk;
    }

    private async static void BigMoth_DrinkChunk(On.Watcher.BigMoth.orig_DrinkChunk orig, Watcher.BigMoth self)
    {
        orig(self);
        if (self.drinkChunk.owner is Player player && player.IsVoid())
        {
            await Task.Delay(6000);
            self.Die();
        }
    }
}
