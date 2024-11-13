using System.Collections.Generic;
using System.IO;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.Oracles;

internal static class ConversationParser
{
	public static void GetConversationEvents(Conversation conversation, string path)
	{
		List<Conversation.DialogueEvent> result = new List<Conversation.DialogueEvent>();
		if(File.Exists(path))
		{
			foreach(string line in File.ReadAllLines(path))
			{
				var split = line.Split([" : "], System.StringSplitOptions.None);
				switch(split.Length)
				{
					//three segments are always text
					case 3:
						{
							
							conversation.events.Add(new Conversation.TextEvent(conversation, int.Parse(split[0]),
							split[1].TranslateStringComplex(), int.Parse(split[2])));
							break;
						}
					case 2:
						{
							//special event processing
							if (split[0] == "SPECEVENT")
							{
								conversation.events.Add(new Conversation.SpecialEvent(conversation, 0, split[1]));
							}
							else //assuming text
							{
								if (int.TryParse(split[0], out int startingnumber))
								{
									conversation.events.Add(new Conversation.TextEvent(conversation, startingnumber, split[0], 0));
								}
								else if (int.TryParse(split[1], out int endingnumber))
								{
									conversation.events.Add(new Conversation.TextEvent(conversation, 0, split[0], endingnumber));
								}
								else ErrorIntoLogsAndDialogue(conversation, result, "couldn't parse line: \"" + line + "\"");
							}
							break;
						}
					case 1:
						{
                            conversation.events.Add(new Conversation.TextEvent(conversation, 0, split[0], 0));
                            break;
						}
					default:
						{
							ErrorIntoLogsAndDialogue(conversation, result, "line contains an abnormal amount of \" : \" (more than 2): " + line);
							break;
						}
				}
			}
		}
		else
		{
			string error = $"failed to find conversation file by path {path}";
			result.Add(new Conversation.TextEvent(conversation, 0, $"failed to find conversation file by path {path}", 200));
			logerr(error);
		}
	}
	static void ErrorIntoLogsAndDialogue(Conversation conversation, List<Conversation.DialogueEvent> events, string error)
	{
		events.Add(new Conversation.TextEvent(conversation, 0, error, 200));
		logerr(error);
	}
}
