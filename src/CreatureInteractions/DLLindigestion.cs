using System.Runtime.CompilerServices;
using VoidTemplate.Objects;
using VoidTemplate.Useful;

namespace VoidTemplate.CreatureInteractions;

public static class DLLindigestion
{
    private sealed class DLLState
    {
        public int killTimer;
        public bool finishEating;
    }

    private sealed class StowawayState
    {
        public int killTimer;
        public bool finishEating;
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
        bool triggered = false;

        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player && player.AreVoidViy() && player.dead)
            {
                DestroyBody(player);
                var state = stowawayStates.GetOrCreateValue(self);
                state.killTimer = 3 * 40;
                state.finishEating = true;
                triggered = true;
                break;
            }
        }

        if (!triggered)
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
        if (state.killTimer <= 0)
        {
            self.Die();
            if (state.finishEating)
            {
                self.eatObjects.Clear();
                state.finishEating = false;
            }
        }
    }

    private static void OnDaddyLongLegsEat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
    {
        bool triggered = false;

        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player && player.IsVoid() && player.dead && !self.HDmode)
            {
                DestroyBody(player);
                var state = dllStates.GetOrCreateValue(self);
                state.killTimer = 3 * 40;
                state.finishEating = true;
                triggered = true;
                break;
            }
        }

        if (!triggered)
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
        if (state.killTimer <= 0)
        {
            self.Die();
            if (state.finishEating)
            {
                self.eatObjects.Clear();
                self.digestingCounter = 0;
                self.moving = false;
                self.tentaclesHoldOn = false;
                state.finishEating = false;
            }
        }
    }

    private static void DestroyBody(Player player)
    {
        if (player?.room != null)
        {
            player.room.RemoveObject(player);
        }

        if (player != null)
        {
            player.dead = true;
        }
    }
}