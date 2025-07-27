using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

public static class CentipedeResist
{
    public static void Hook()
    {
        On.Centipede.Shock += Centipede_Shock;
    }

    private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
    {
        if (!(shockObj is Player player && player.slugcatStats.name == VoidEnums.SlugcatID.Void))
        {
            orig(self, shockObj);
            return;
        }

        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);

        if (self.graphicsModule != null)
        {
            (self.graphicsModule as CentipedeGraphics).lightFlash = 1f;
            int sparkCount = (int)Mathf.Lerp(4f, 8f, self.size);
            for (int i = 0; i < sparkCount; i++)
            {
                self.room.AddObject(new Spark(
                    self.HeadChunk.pos,
                    Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value),
                    new Color(0.7f, 0.7f, 1f),
                    null, 8, 14));
            }
        }

        ApplyRandomVelocity(self.bodyChunks, 6f);
        ApplyRandomVelocity(shockObj.bodyChunks, 6f);

        if (self.AquaCenti || self.Red)
        {
            HandleSpecialCentipede(self, shockObj, self.AquaCenti ? 4 : 2);
            return;
        }

        if (shockObj is Creature creature)
        {
            if (self.Small)
            {
                HandleSmallCentipede(self, creature);
            }
            else
            {
                HandleMassBasedShock(self, creature);
            }
        }
    }

    private static void ApplyRandomVelocity(BodyChunk[] chunks, float magnitude)
    {
        foreach (var chunk in chunks)
        {
            chunk.vel += Custom.RNV() * magnitude * UnityEngine.Random.value;
            chunk.pos += Custom.RNV() * magnitude * UnityEngine.Random.value;
        }
    }

    private static void HandleSpecialCentipede(Centipede self, PhysicalObject shockObj, int randomRange)
    {
        bool isHighKarma = shockObj is Player { KarmaCap: 10 } || Karma11Update.VoidKarma11;
        int randomDeath = UnityEngine.Random.Range(0, randomRange);

        if (isHighKarma && randomDeath != 0)
        {
            HandleStunScenario(self, shockObj as Creature, 240);
        }
        else
        {
            HandleDeathScenario(self, shockObj as Creature, 120);
        }
    }

    private static void HandleSmallCentipede(Centipede self, Creature creature)
    {
        self.Stun(60);
        creature.LoseAllGrasps();
    }

    private static void HandleMassBasedShock(Centipede self, Creature creature)
    {
        int karmaLevel = GetKarmaLevel(creature as Player);
        int randomDeath = UnityEngine.Random.Range(0, GetRandomRange(karmaLevel));

        if (creature.TotalMass * 2 < self.TotalMass)
        {
            HandleMassDifferenceScenario(self, creature, karmaLevel, randomDeath, 20);
        }
        else if (creature.TotalMass < self.TotalMass)
        {
            HandleMassDifferenceScenario(self, creature, karmaLevel, randomDeath, 10);
        }
        else
        {
            HandleEqualMassScenario(self, creature);
        }
    }

    private static int GetKarmaLevel(Player player)
    {
        if (player == null) return 0;
        return Karma11Update.VoidKarma11 ? 10 : player.KarmaCap;
    }

    private static int GetRandomRange(int karmaLevel)
    {
        return karmaLevel >= 10 ? 20 : 10;
    }

    private static void HandleMassDifferenceScenario(Centipede self, Creature creature, int karmaLevel, int randomDeath, int baseRange)
    {
        if (karmaLevel < 5)
        {
            if (randomDeath > 4) HandleDeathScenario(self, creature, 60);
            else HandleStunScenario(self, creature, 120);
        }
        else if (karmaLevel == 10 && randomDeath > 15)
        {
            HandleDeathScenario(self, creature, 60);
        }
        else if (randomDeath > (baseRange - karmaLevel))
        {
            HandleDeathScenario(self, creature, 60);
        }
        else
        {
            HandleStunScenario(self, creature, 120);
        }
    }

    private static void HandleEqualMassScenario(Centipede self, Creature creature)
    {
        creature.Stun(60);
        self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
        creature.LoseAllGrasps();
        self.Stun(120);
        self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
        self.Violence(self.mainBodyChunk, new Vector2?(Vector2.zero), creature.mainBodyChunk, null, Creature.DamageType.Electric, 1f, 200f);
        self.shockGiveUpCounter = Math.Max(self.shockGiveUpCounter, 30);
        self.AI.annoyingCollisions = Math.Min(self.AI.annoyingCollisions / 2, 150);
    }

    private static void HandleDeathScenario(Centipede self, Creature creature, int stunDuration)
    {
        creature.Die();
        self.Stun(stunDuration);
        bool useLongStun = !self.AquaCenti && !self.Red;
        self.room.AddObject(new CreatureSpasmer(self, useLongStun, useLongStun ? (int)Mathf.Lerp(70f, 120f, self.size) : self.stun));
        self.Violence(self.mainBodyChunk, new Vector2?(Vector2.zero),
            (creature.TotalMass >= self.TotalMass) ? creature.mainBodyChunk : self.mainBodyChunk,
            null, Creature.DamageType.Electric,
            (self.AquaCenti || self.Red) ? 2f : 1f, 200f);
    }

    private static void HandleStunScenario(Centipede self, Creature creature, int stunDuration)
    {
        creature.Stun(stunDuration);
        self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
        creature.LoseAllGrasps();
        self.Stun(stunDuration);
        self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
        self.Violence(self.mainBodyChunk, new Vector2?(Vector2.zero), self.mainBodyChunk, null, Creature.DamageType.Electric,
            (self.AquaCenti || self.Red) ? 2f : 1f, 200f);
    }
}
