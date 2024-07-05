using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Useful;

public static class StaticStuff
{
    static StaticStuff()
    {
        SleepSceneID = new("Sleep_Void");
        DeathSceneID = new("Death_Void");
    }

    public static Menu.MenuScene.SceneID SleepSceneID;
    public static Menu.MenuScene.SceneID DeathSceneID;
}
