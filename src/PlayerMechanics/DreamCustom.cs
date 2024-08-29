using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Mesh;
namespace VoidTemplate.PlayerMechanics;

public static class DreamCustom
{
    public static void Hook()
    {
        On.DreamsState.EndOfCycleProgress += CustomDream;
    }

    private static void CustomDream(On.DreamsState.orig_EndOfCycleProgress orig, DreamsState self, SaveState saveState, string currentRegion, string denPosition)
    {
        if (saveState.saveStateNumber == VoidEnums.SlugcatID.TheVoid)
        {
            var miscData = saveState.miscWorldSaveData;

            switch (currentRegion)
            {
                case "LF":
                {
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Farm);
                    break;
                }
                case "SI":
                    {
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sky);
                    break;
                    }
                case "SB" when saveState.deathPersistentSaveData.karmaCap != 2:
                {
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sub);
                        break;
                }
                case "SL" when miscData.SLOracleState.playerEncountersWithMark <= 0:
                    {
                    saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Moon);
                    break;
                }
            }
            switch (saveState.deathPersistentSaveData.karmaCap)
            {
                case 10:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidHeart);
                        break;
                    }
                case >=8:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidSea);
                        break;
                    }
                case >=6:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidBody);
                        break;
                    }
                case >=4:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.NSH);
                        break;
                    }
                default:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidNSH);
                        break;
                    }
            }
        }
        orig(self, saveState, currentRegion, denPosition);
    }
}
