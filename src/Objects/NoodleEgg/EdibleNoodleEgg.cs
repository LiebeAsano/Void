using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.OptionInterface;
using RWCustom;


namespace VoidTemplate.Objects.NoodleEgg;

public class EdibleNoodleEgg
{
    public int bites = 4;
    public bool shellCrack = false;
    public NeedleEgg sourceEgg;

    private bool hookActive = false;

    public EdibleNoodleEgg(NeedleEgg egg)
    {
        sourceEgg = egg;
        Hook();
    }

    public void Hook()
    {
        if (!hookActive)
        {
            On.NeedleEgg.Shell.Draw += Shell_Draw;
            On.NeedleEgg.Update += NeedleEgg_Update;
            hookActive = true;
        }
    }

    public void Unhook()
    {
        if (hookActive)
        {
            On.NeedleEgg.Shell.Draw -= Shell_Draw;
            On.NeedleEgg.Update -= NeedleEgg_Update;
            hookActive = false;
        }
    }

    private void Shell_Draw(On.NeedleEgg.Shell.orig_Draw orig, NeedleEgg.Shell self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 drwPos, Vector2 rotVec, Vector2 prp)
    {
        if (self == sourceEgg.halves[0] || self == sourceEgg.halves[1])
        {
            if (!shellCrack)
            {
                orig(self, sLeaser, rCam, timeStacker, camPos, drwPos, rotVec, prp);
            }
            else
            {
                for (int i = self.sprite; i < self.sprite + 2; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }
            }
        }
        else
        {
            orig(self, sLeaser, rCam, timeStacker, camPos, drwPos, rotVec, prp);
        }
    }

    private void NeedleEgg_Update(On.NeedleEgg.orig_Update orig, NeedleEgg self, bool eu)
    {
        orig(self, eu);

        if (self == sourceEgg && shellCrack)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sourceEgg.shellpositions[i, j] = Vector2.zero;
                }
            }
        }
    }


    public void Bite(Creature.Grasp grasp, bool eu)
    {
        if (bites == 4)
        {
            sourceEgg.room.PlaySound(DLCSharedEnums.SharedSoundID.Duck_Pop, grasp.grabber.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
            shellCrack = true;
            for (int i = 0; i < 3; i++)
            {
                sourceEgg.room.AddObject(new WaterDrip(sourceEgg.firstChunk.pos, Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(4f, 21f, UnityEngine.Random.value), false));
            }
        }
        bites--;
        sourceEgg.room.PlaySound((bites != 0) ? SoundID.Slugcat_Bite_Dangle_Fruit : SoundID.Slugcat_Eat_Dangle_Fruit, sourceEgg.firstChunk);
        sourceEgg.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
        if (bites < 1)
        {
            grasp.Release();
            (grasp.grabber as Player).AddFood(OptionAccessors.SimpleFood ? 4 : 2);
            sourceEgg.Destroy();
            sourceEgg.RemoveEdible();
        }
    }
}

public static class EdibleNoodleEggCWT
{
    private static readonly ConditionalWeakTable<NeedleEgg, EdibleNoodleEgg> edibleEgg = new();

    public static EdibleNoodleEgg GetEdible(this NeedleEgg egg)
    {
        return edibleEgg.GetValue(egg, e =>
        {
            var edible = new EdibleNoodleEgg(e);
            edible.Hook();
            return edible;
        });
    }

    public static void RemoveEdible(this NeedleEgg egg)
    {
        if (edibleEgg.TryGetValue(egg, out var edible))
        {
            edible.Unhook();
            edibleEgg.Remove(egg);
        }
    }
}