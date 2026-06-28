using System.Runtime.CompilerServices;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class DLLindigestion
{
    private sealed class DLLState
    {
        public int killTimer;
        public bool voidPoisoned;
    }

    private sealed class StowawayState
    {
        public int killTimer;
        public bool voidPoisoned;
    }

    private static readonly ConditionalWeakTable<DaddyLongLegs, DLLState> dllStates = new();
    private static readonly ConditionalWeakTable<MoreSlugcats.StowawayBug, StowawayState> stowawayStates = new();

    public static void Hook()
    {
        On.DaddyLongLegs.Eat += OnDaddyLongLegsEat;
        On.DaddyLongLegs.Update += DaddyLongLegs_Update;

        On.MoreSlugcats.StowawayBug.Eat += StowawayBugEat;
        On.MoreSlugcats.StowawayBug.Update += StowawayBug_Update;
    }

    private static void StowawayBugEat(On.MoreSlugcats.StowawayBug.orig_Eat orig, MoreSlugcats.StowawayBug self, bool eu)
    {
        var state = stowawayStates.GetOrCreateValue(self);

        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player && player.AreVoidViy() && player.dead && !state.voidPoisoned)
            {
                DestroyBody(player);
                state.killTimer = UnityEngine.Random.Range(160, 321);
                state.voidPoisoned = true;
                break;
            }
        }

        if (!state.voidPoisoned)
            orig(self, eu);
    }

    private static void StowawayBug_Update(On.MoreSlugcats.StowawayBug.orig_Update orig, MoreSlugcats.StowawayBug self, bool eu)
    {
        orig(self, eu);

        if (!stowawayStates.TryGetValue(self, out var state) || state.killTimer <= 0)
            return;

        if (self.slatedForDeletetion || self.room == null)
        {
            state.killTimer = 0;
            return;
        }

        state.killTimer--;
        if (state.killTimer <= 0 && state.voidPoisoned)
        {
            self.eatObjects.Clear();
            self.Die();
        }
    }

    private static void OnDaddyLongLegsEat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
    {
        var state = dllStates.GetOrCreateValue(self);

        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player && player.IsVoid() && player.dead && !self.HDmode && !state.voidPoisoned)
            {
                DestroyBody(player);
                state.killTimer = UnityEngine.Random.Range(120, 201);
                state.voidPoisoned = true;
                break;
            }
        }

        if (!state.voidPoisoned)
            orig(self, eu);
    }

    private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
    {
        orig(self, eu);

        if (!dllStates.TryGetValue(self, out var state) || state.killTimer <= 0)
            return;

        if (self.slatedForDeletetion || self.room == null)
        {
            state.killTimer = 0;
            return;
        }

        state.killTimer--;
        if (state.killTimer <= 0 && state.voidPoisoned)
        {
            self.eatObjects.Clear();
            self.digestingCounter = 0;
            self.moving = false;
            self.tentaclesHoldOn = false;
            self.Die();
        }
    }

    private static void DestroyBody(Player player)
    {
        if (player?.room != null)
        {
            player.room.RemoveObject(player);
        }

        player?.dead = true;
    }
}