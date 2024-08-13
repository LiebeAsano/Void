using UnityEngine;
using VoidTemplate;
using RWCustom;
using System.IO;
using Newtonsoft.Json;
using System;
using static VoidTemplate.SaveManager;

namespace VoidTemplate;

internal class KarmaCapTrigger : UpdatableAndDeletable
{
    private static int targetRoomID = -1;

    public KarmaCapTrigger(Room room, params Message[] messages)
    {
        messageList = messages;
        this.room = room;
    }

    public override void Update(bool eu)
    {
        if (room.game.session.Players[0].realizedCreature == null ||
            room.game.cameras[0].hud == null ||
            room.game.cameras[0].hud.textPrompt == null)
        {
            return;
        }
        foreach (Message message in messageList)
        {
            string[] array = message.text.Split('/');
            string text = "";
            foreach (string s in array)
                text += Custom.rainWorld.inGameTranslator.Translate(s);

            room.game.cameras[0].hud.textPrompt.AddMessage(text, message.wait, message.time, true, ModManager.MMF);
        }
        room.game.GetStorySession.saveState.SetMessageShown(true);
        slatedForDeletetion = true;
    }

    private static void InitializeTargetRoomID(Room room)
    {
        AbstractRoom targetRoom = room.world.GetAbstractRoom("SB_A14");
        if (targetRoom == null)
        {
            throw new Exception($"[TheVoid] Room 'SB_A14' does not exist.");
        }
        targetRoomID = targetRoom.index;
    }

    public class Message
    {
        public readonly string text;
        public readonly int wait;
        public readonly int time;

        public Message(string text, int waitTime, int holdTime)
        {
            this.text = text;
            wait = waitTime;
            time = holdTime;
        }

        public Message(string text) : this(text, 0, 160)
        {
        }
    }

    private Message[] messageList;
}