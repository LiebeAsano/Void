using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions
{
    internal static class LoachEaten
    {
        public static void Hook()
        {
            On.Watcher.Loach.Eat += Loach_Eat;
        } 
        private static async void Loach_Eat(On.Watcher.Loach.orig_Eat orig, Watcher.Loach self, bool eu)
        {
            orig(self, eu);
            foreach (var eatObject in self.eatObjects)
            {
                if (eatObject.progression > 1f && eatObject.chunk.owner is Player player && player.IsVoid())
                {
                    player.Destroy();
                    await Task.Delay(6000);
                    self.Die();
                }
            }
        }
    }
}
