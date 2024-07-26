using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoidTemplate.Oracles;
/// <summary>
/// This class parses text files to become conversations. Extended for properly reflecting Conversations (which are not limited to just text)
/// Here are all the styles you can use:
/// // this line is ignored. Write whatever
/// 40>>This is text event that will linger for 1 second
/// //anything marked special will trigger event in conversation owner associated action.
/// //pebbles has 'karma' for giving mark, 'panic', 'resync', 'tag', 'unlock'. without ''.
/// //anything after karma actually 
/// <special>karma
/// <wait>200
/// </summary>
internal static class ConversationParser
{
    static string eventDirectory => Path.Combine("text","OracleConversations");
    static string FolderPath => Path.Combine(ModManager.ActiveMods.FirstOrDefault(x => x.id == "thevoid.liebeasano").path,eventDirectory);
    static string FilePath(string file) => FolderPath + Path.DirectorySeparatorChar + file;
    static void LogErr(object e) => _Plugin.logger.LogError(e); 



    public static Conversation InitializeConversation(Conversation.IOwnAConversation interfaceOwner, Conversation.ID id, HUD.DialogBox dialogBox)
    {
        Conversation conversation = new VoidConversation(interfaceOwner, id, dialogBox);
        conversation.LoadConversation(id.ToString());
        return conversation;
    }

    public static void LoadConversation(this Conversation owner, string filename)
    {
        List < Conversation.DialogueEvent > events = new();
        string[] strings = File.ReadAllLines(FilePath(filename));
        foreach (string s in strings)
        {
            if (s.StartsWith("//")) continue;
            else if (Regex.IsMatch(s, @"^\d>>*")) events.Add(GetConversationEvent(s, owner));
            else if (Regex.IsMatch(s, @"^<special>*")) events.Add(GetSpecialEvent(s, owner));
            else if (Regex.IsMatch(s, @"^<wait>\d")) events.Add(GetWaitEvent(s, owner));
            else
            {
                LogErr($"File {filename}, string {s}, couldn't parse it correctly");
            }
        }
        owner.events = events;
    }

    static Conversation.TextEvent GetConversationEvent(string s, Conversation conversation)
    {
        var results = s.Split(new string[] { ">>" }, StringSplitOptions.None);
        if (!int.TryParse(results[0], out var linger)) LogErr($"CONVERSATION PARSER BROKE! See Oracles.ConversationParser #33 for details. This particular error is raised attempting to parse invalid number. Number in question: {results[1]} ");
        return new(conversation, 0, StaticStuff.TranslateStringComplex(results[1]), linger);
    }
    static Conversation.SpecialEvent GetSpecialEvent(string s, Conversation conversation)
    {
        return new Conversation.SpecialEvent(conversation, 0, s.Split('>')[1]);
    }
    static Conversation.WaitEvent GetWaitEvent(string s, Conversation conversation)
    {
        return new Conversation.WaitEvent(conversation, int.Parse(s.Split('>')[1]));
    }
}
