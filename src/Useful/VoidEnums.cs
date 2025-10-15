namespace VoidTemplate;
using Menu;
using MoreSlugcats;
using VoidTemplate.Objects;

public static class VoidEnums
{
	private const string uniqueprefix = "VoidSlugCat";
	public static void RegisterEnums()
	{
		SceneID.Register();
		DreamID.Register();
		SoundID.Register();
		ConversationID.Register();
		SlugcatID.Register();
		ProcessID.Register();
    }

	public static class SceneID
	{
		public static void Register()
		{
			Farm = new("Farm_Dream_Void");
			HunterRot = new("Hunter_Rot_Dream_Void");
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

			SleepScene = new("Sleep_Void");
			SleepScene11 = new("Sleep_Void11");
            SleepSceneMark = new("Sleep_VoidMark");
			DeathScene = new("Death_Void");
			DeathScene11 = new("Death_Void11");
            DeathSceneMark = new("Death_VoidMark");

			StaticDeath = new("Static_Death_Void");
			StaticDeath11 = new("Static_Death_Void11");
			StaticEnd = new("Static_End_Scene_Void");
			StaticEnd11 = new("Static_End_Scene_Void11");

			UnlockedSlugcat = new("Slugcat_Void");
			LockedSlugcat = new("Slugcat_Void_Dark");
			KarmaDeath = new("karma_death_void");
			KarmaDeath11 = new("karma_death_void_karma11");

			SelectFPScene = new("Scene_Five_Pebbles_Void");
			SelectEndingScene = new("End_Scene_Void");
			SelectEnding11Scene = new("End_Scene_Void11");
			SelectKarma5Scene = new("Scene_Five_Karma_Void");
			SelectKarma11Scene = new("Scene_Eleven_Karma_Void");

			MainMenuSceneMonk = new("Main_Menu_Scene_Monk");
            MainMenuSceneSurv = new("Main_Menu_Scene_Surv");
            MainMenuSceneHunt = new("Main_Menu_Scene_Hunt");
            MainMenuSceneMonkSurv = new("Main_Menu_Scene_MonkSurv");
            MainMenuSceneMonkHunt = new("Main_Menu_Scene_MonkHunt");
            MainMenuSceneMonkSurvHunt = new("Main_Menu_Scene_MonkSurvHunt");
            MainMenuSceneSurvHunt = new("Main_Menu_Scene_SurvHunt");
        }
		public static MenuScene.SceneID SleepScene;
		public static MenuScene.SceneID SleepScene11;
        public static MenuScene.SceneID SleepSceneMark;
        public static MenuScene.SceneID DeathScene;
		public static MenuScene.SceneID DeathScene11;
        public static MenuScene.SceneID DeathSceneMark;

        public static MenuScene.SceneID StaticEnd;
		public static MenuScene.SceneID StaticEnd11;
		public static MenuScene.SceneID StaticDeath;
		public static MenuScene.SceneID StaticDeath11;

		public static MenuScene.SceneID UnlockedSlugcat;
		public static MenuScene.SceneID LockedSlugcat;
		public static MenuScene.SceneID KarmaDeath;
		public static MenuScene.SceneID KarmaDeath11;

		public static MenuScene.SceneID SelectEndingScene;
		public static MenuScene.SceneID SelectEnding11Scene;
		public static MenuScene.SceneID SelectFPScene;
		public static MenuScene.SceneID SelectKarma5Scene;
		public static MenuScene.SceneID SelectKarma11Scene;

		public static MenuScene.SceneID Farm;
		public static MenuScene.SceneID HunterRot;
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

        public static MenuScene.SceneID MainMenuSceneMonk;
        public static MenuScene.SceneID MainMenuSceneSurv;
        public static MenuScene.SceneID MainMenuSceneHunt;
        public static MenuScene.SceneID MainMenuSceneMonkSurv;
        public static MenuScene.SceneID MainMenuSceneMonkHunt;
        public static MenuScene.SceneID MainMenuSceneMonkSurvHunt;
        public static MenuScene.SceneID MainMenuSceneSurvHunt;
    }
	public static class DreamID
	{
		public static void Register()
		{
			FarmDream = new(uniqueprefix + "FarmDream", true);
			HunterRotDream = new(uniqueprefix + "HunterRotDream", true);
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
		}

		public static DreamsState.DreamID FarmDream;
		public static DreamsState.DreamID HunterRotDream;
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
	}

	public static class SoundID
	{
		public static void Register()
		{
            HalcyonMemories = new("Halcyon_Memories", true);

            SleepMarkSound = new("Sleep_Mark_Sound", true);

            VoidNSHDreamSound = new("Void_NSH_Dream_Sound", true);
			NSHDreamSound = new("NSH_Dream_Sound", true);
			SkyDreamSound = new("Sky_Dream_Sound", true);
			SubDreamSound = new("Sub_Dream_Sound", true);
			FarmDreamSound = new("Farm_Dream_Sound", true);
			HunterRotDreamSound = new("Hunter_Rot_Dream_Sound", true);
			MoonDreamSound = new("Moon_Dream_Sound", true);
			PebbleDreamSound = new("Pebble_Dream_Sound", true);
			RotDreamSound = new("Rot_Dream_Sound", true);
            VoidSeaDreamSound = new("Void_Sea_Dream_Sound", true);
            VoidBodyDreamSound = new("Void_Body_Dream_Sound", true);
			VoidHeartDreamSound = new("Void_Heart_Dream_Sound", true);

			UIPitch1 = new("UI_Pitch1", true);
            UIPitch2 = new("UI_Pitch2", true);
        }

        public static global::SoundID HalcyonMemories;

        public static global::SoundID SleepMarkSound;

        public static global::SoundID VoidNSHDreamSound;
		public static global::SoundID NSHDreamSound;
		public static global::SoundID SkyDreamSound;
		public static global::SoundID SubDreamSound;
		public static global::SoundID FarmDreamSound;
        public static global::SoundID HunterRotDreamSound;
        public static global::SoundID MoonDreamSound;
		public static global::SoundID PebbleDreamSound;
		public static global::SoundID RotDreamSound;
        public static global::SoundID VoidSeaDreamSound;
        public static global::SoundID VoidBodyDreamSound;
        public static global::SoundID VoidHeartDreamSound;

        public static global::SoundID UIPitch1;
        public static global::SoundID UIPitch2;
    }
	public static class ConversationID
	{
		public static void Register()
		{
			LWRot = new("LW-rot");
			LWVoid = new("LW-void");
			LWSlugcat = new("LW-slugcat");
			LRDissertation = new("LR_pearl_dissertation");
			LRArchiveRequest = new("LR_archive_request");
			LRSecret = new("LR_secret");
		}
		public static Conversation.ID LWRot;
        public static Conversation.ID LWVoid;
        public static Conversation.ID LWSlugcat;
        public static Conversation.ID LRDissertation;
        public static Conversation.ID LRArchiveRequest;
        public static Conversation.ID LRSecret;
		public static Conversation.ID[] PearlConversations => [LWRot,
		LWVoid,
		LWSlugcat,
		LRDissertation,
		LRArchiveRequest,
		LRSecret];
    }
	public static class RoomNames
	{
		public const string EndingRoomName = "SB_LWA01";
	}
	public static class SlugcatID
	{
		public static void Register()
		{
			Void = new("Void");
			Viy = new("Viy");
		}
		public static SlugcatStats.Name Void;
        public static SlugcatStats.Name Viy;
    }

	public static class ProcessID
	{
		public static void Register()
		{
			TokenDecrease = new("Token Decrease", true);
		}
		public static ProcessManager.ProcessID TokenDecrease;
	}
}
