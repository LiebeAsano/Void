namespace VoidTemplate;
using Menu;
using static System.Net.Mime.MediaTypeNames;
using static VoidTemplate.SaveManager;

public static class StaticStuff
{
    private const string uniqueprefix = "TheVoidSlugCat";
    public static void RegisterEnums()
    {
        SleepSceneID = new("Sleep_Void");
        DeathSceneID = new("Death_Void");
        SleepKarma11ID = new("Sleep_Void_Karma11");
        TheVoid = new("TheVoid");
        StaticDeath = new("Static_Void_Death");

        Farm = new("Farm_Dream_Void");
        Moon = new("Moon_Dream_Void");
        NSH = new("NSH_Dream_Void");
        Pebble = new("Pebble_Dream_Void");
        Rot = new("Rot_Dream_Void");
        Sky = new("Sky_Dream_Void");
        Sub = new("Sub_Dream_Void");
        Void_Body = new("Body_Dream_Void");
        Void_Heart = new("Heart_Dream_Void");
        Void_NSH = new("Void_NSH_Dream_Void");
        Void_Sea = new("Void_Sea_Dream_Void");

        FarmDream = new(uniqueprefix + "FarmDream", true);
        MoonDream = new(uniqueprefix + "MoonDream", true);
        NSHDream = new(uniqueprefix + "NSHDream", true);
        PebbleDream = new(uniqueprefix + "PebbleDream", true);
        RotDream = new(uniqueprefix + "RotDream", true);
        SkyDream = new(uniqueprefix + "SkyDream", true);
        SubDream = new(uniqueprefix + "SubDream", true);
        Void_BodyDream = new(uniqueprefix + "BodyDream", true);
        Void_HeartDream = new(uniqueprefix + "HeartDream", true);
        Void_NSHDream = new(uniqueprefix + "NSHVoidDream", true);
        Void_SeaDream = new(uniqueprefix + "VoidSeaDream", true);

        UnlockedSlugcat = new("Slugcat_Void");
        LockedSlugcat = new("Slugcat_Void_Dark");
        KarmaDeath = new("karma_death_void");
        KarmaDeath11 = new("karma_death_void_karma11");

        SelectFPScene = new("placeholder1");
        SelectEndingScene = new("placeholder2");
        SelectKarma5Scene = new("placeholder3");

        Moon_VoidConversation = new("Moon_VoidConversation", true);
    }
    #region StandardScenes
    public static MenuScene.SceneID SleepSceneID;
    public static MenuScene.SceneID DeathSceneID;
    public static MenuScene.SceneID SleepKarma11ID;
    public static MenuScene.SceneID StaticDeath;

    public static MenuScene.SceneID UnlockedSlugcat;
    public static MenuScene.SceneID LockedSlugcat;
    public static MenuScene.SceneID KarmaDeath;
    public static MenuScene.SceneID KarmaDeath11;
    #endregion
    #region SelectScreenScenes
    public static MenuScene.SceneID SelectEndingScene;
    public static MenuScene.SceneID SelectFPScene;
    public static MenuScene.SceneID SelectKarma5Scene;
    #endregion
    #region dreams and dream scenes
    public static MenuScene.SceneID Farm;
    public static MenuScene.SceneID Moon;
    public static MenuScene.SceneID NSH;
    public static MenuScene.SceneID Pebble;
    public static MenuScene.SceneID Rot;
    public static MenuScene.SceneID Sky;
    public static MenuScene.SceneID Sub;
    public static MenuScene.SceneID Void_Body;
    public static MenuScene.SceneID Void_Heart;
    public static MenuScene.SceneID Void_NSH;
    public static MenuScene.SceneID Void_Sea;

    public static DreamsState.DreamID FarmDream;
    public static DreamsState.DreamID MoonDream;
    public static DreamsState.DreamID NSHDream;
    public static DreamsState.DreamID PebbleDream;
    public static DreamsState.DreamID RotDream;
    public static DreamsState.DreamID SkyDream;
    public static DreamsState.DreamID SubDream;
    public static DreamsState.DreamID Void_BodyDream;
    public static DreamsState.DreamID Void_HeartDream;
    public static DreamsState.DreamID Void_NSHDream;
    public static DreamsState.DreamID Void_SeaDream;
    #endregion
    #region conversations
    public static Conversation.ID Moon_VoidConversation;
    #endregion


    public const string EndingRoomName = "SI_A07";

    public const int TicksPerSecond = 40;
    public static SlugcatStats.Name TheVoid;
    public static string TranslateStringComplex(this string str) => RWCustom.Custom.rainWorld.inGameTranslator.Translate(str).Replace("<LINE>", "\n");
}
