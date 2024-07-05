namespace TheVoid;

public static class StaticStuff
{
    static StaticStuff()
    {
        SleepSceneID = new("Sleep_Void");
        DeathSceneID = new("Death_Void");
        SleepKarma11ID = new("Sleep_Void_Karma11");
        TheVoid = new("TheVoid");
    }

    public static Menu.MenuScene.SceneID SleepSceneID;
    public static Menu.MenuScene.SceneID DeathSceneID;
    public static Menu.MenuScene.SceneID SleepKarma11ID;

    public const string EndingRoomName = "SI_A07";

    public const int TicksPerSecond = 40;
    public static readonly SlugcatStats.Name TheVoid;
}
