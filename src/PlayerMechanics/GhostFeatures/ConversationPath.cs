using MoreSlugcats;
using RWCustom;
using System.IO;

namespace VoidTemplate.PlayerMechanics.GhostFeatures;

public static class ConversationPath
{
	public static void Hook()
	{
		On.GhostConversation.AddEvents += GhostConversation_AddEvents;
	}

	//dialogue path : text/text_{language id}/ghost_{ghost region name (lower)}_{mark/nomark}.txt
	//eg: text/text_rus/ghost_sb_mark.txt

	//If the corresponding language dialogue cannot be found, the <English> version will be read.
	//If it is still not found, read the original in-game text (a prompt will be added for DEBUG)

	private static void GhostConversation_AddEvents(On.GhostConversation.orig_AddEvents orig, GhostConversation self)
	{
		if (self.ghost.room.game.session is StoryGameSession session &&
			session.saveStateNumber == VoidEnums.SlugcatID.Void)
		{
			var path = AssetManager.ResolveFilePath(GetGhostConversationPath(self, Custom.rainWorld.inGameTranslator.currentLanguage, self.id,
				session.saveState.deathPersistentSaveData.theMark));
			if (!File.Exists(path))
			{
				path = AssetManager.ResolveFilePath(GetGhostConversationPath(self, InGameTranslator.LanguageID.English, self.id,
					session.saveState.deathPersistentSaveData.theMark));
			}

			if (File.Exists(path))
			{
				foreach (var line in File.ReadAllLines(path))
				{
					var split = LocalizationTranslator.ConsolidateLineInstructions(line);
					if (split.Length == 3)
						self.events.Add(new Conversation.TextEvent(self, int.Parse(split[0]),
							split[1], int.Parse(split[2])));
					else
						self.events.Add(new Conversation.TextEvent(self, 0, line, 0));
				}

				return;
			}

			self.events.Add(new Conversation.TextEvent(self, 0, $"Can't find conv at {GetGhostConversationPath(self, InGameTranslator.LanguageID.English, self.id, session.saveState.deathPersistentSaveData.theMark)}<LINE> for {self.id}", 0));

		}
		orig(self);
	}
	private static string GetGhostConversationPath(GhostConversation self, InGameTranslator.LanguageID id, Conversation.ID convId, bool hasMark)
	{
		var translator = Custom.rainWorld.inGameTranslator;
		var path = $"{translator.SpecificTextFolderDirectory(id)}/{convId}_";
		if (convId != MoreSlugcatsEnums.ConversationID.Ghost_MS)
		{
			switch (UnityEngine.Random.Range(0, 3))
			{
				case 0:
					path += "first_";
					break;
				case 1:
					path += "second_";
					break;
				case 2:
					path += "third_";
					break;
			}
		}
		else
		{
			if (self.ghost.room.game.rainWorld.progression.miscProgressionData.beaten_Watcher_Ascension)
				path += "third_";
			else
				path += SaveManager.ExternalSaveData.ViyUnlocked ? "second_" : "first_";
        }
		path += hasMark ? "mark.txt" : "nomark.txt";
		return path;
	}
}
