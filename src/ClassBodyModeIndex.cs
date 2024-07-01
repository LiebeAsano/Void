using UnityEngine;
using TheVoid;
using RWCustom;
using System.IO;
using Newtonsoft.Json;
using System;
using static SaveManager;

namespace VoidTemplate
{
    internal class KarmaCapTrigger : UpdatableAndDeletable
    {
        private SaveData saveData;
        private static int targetRoomID = -1;

        public KarmaCapTrigger(Room room, Player player, params Message[] messages)
        {
            messageList = messages;
            this.room = room;
            this.player = player;

            saveData = SaveManager.Load();
        }

        public override void Update(bool eu)
        {
            if (!IsMainCampaign(room.game))
            {
                return;
            }

            if (targetRoomID == -1)
            {
                InitializeTargetRoomID(room);
            }

            if (room.abstractRoom.index == targetRoomID && player.slugcatStats.name == Plugin.TheVoid && saveData.messageShown)
            {
                saveData.messageShown = false;
                SaveManager.Save(saveData);
            }

            if (saveData.messageShown)
            {
                return; // Если сообщение уже было показано, выходим из метода
            }

            if (room.game.session.Players[0].realizedCreature == null ||
                room.game.cameras[0].hud == null ||
                room.game.cameras[0].hud.textPrompt == null)
            {
                return;
            }

            if (player.slugcatStats.name == Plugin.TheVoid && player.KarmaCap == 4)
            {
                foreach (Message message in messageList)
                {
                    string[] array = message.text.Split('/');
                    string text = "";
                    foreach (string s in array)
                        text += Custom.rainWorld.inGameTranslator.Translate(s);

                    room.game.cameras[0].hud.textPrompt.AddMessage(text, message.wait, message.time, true, ModManager.MMF);
                }

                saveData.messageShown = true; // Устанавливаем состояние
                SaveManager.Save(saveData); // Сохраняем состояние

                slatedForDeletetion = true;
            }
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

        public static bool IsMainCampaign(RainWorldGame game)
        {
            if (game.session is StoryGameSession session && session.characterStats.name == Plugin.TheVoid
                && (!ModManager.Expedition || !game.rainWorld.ExpeditionMode))
            {
                return true;
            }

            return false;
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
        private Player player;
    }

    public static class KarmaCapCheck
    {
        public static void Init(Room room, Player player)
        {
            if (!KarmaCapTrigger.IsMainCampaign(room.game))
            {
                // Пропускаем инициализацию для не основной кампании
                return;
            }
            KarmaCapTrigger karmaTrigger = new(room, player,
                new KarmaCapTrigger.Message[]
                {
                    new("Your body is strong enough to climb the ceilings.", 0, 400),
                    new("Hold down the 'Up' and 'Direction' buttons to climb the ceiling.")
                }
            );
            room.AddObject(karmaTrigger);
        }
    }
}

public static class SaveManager
{
    private static string saveFilePath = Application.persistentDataPath + "/voidsavedata.json";

    public static SaveData Load()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonConvert.DeserializeObject<SaveData>(json);
        }
        return new SaveData();
    }

    public static void Save(SaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(saveFilePath, json);
    }

    [System.Serializable]
    public class SaveData
    {
        public bool messageShown;
    }
}