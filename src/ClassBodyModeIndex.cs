using UnityEngine;
using System.Linq;
using TheVoid;
using RWCustom;
using BepInEx;
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
            //Debug.Log("KarmaCapTrigger: Constructor called");
            messageList = messages;
            this.room = room;
            this.player = player;

            // Загружаем состояние
            saveData = SaveManager.Load();
            //Debug.Log("KarmaCapTrigger: Save data loaded - messageShown: " + saveData.messageShown);
        }

        public override void Update(bool eu)
        {
            if (!IsMainCampaign(room.game))
            {
                // Пропускаем обработку, если не основная кампания
                return;
            }

            // Проверяем инициализацию targetRoomID только для основной кампании
            if (targetRoomID == -1)
            {
                InitializeTargetRoomID(room);
            }

            //Debug.Log($"KarmaCapTrigger: Entering Update - Current Room: {room.abstractRoom.name}, Target Room: {(targetRoomID != -1 ? targetRoomID.ToString() : "Not Set")}, Player ID: {player.slugcatStats.name}, messageShown: {saveData.messageShown}");

            if (room.abstractRoom.index == targetRoomID && player.slugcatStats.name == Plugin.TheVoid && saveData.messageShown)
            {
                saveData.messageShown = false;
                SaveManager.Save(saveData);
                //Debug.Log("KarmaCapTrigger: Player entered SB_A14, messageShown reset to false");
            }

            if (saveData.messageShown)
            {
                //Debug.Log("KarmaCapTrigger: Message already shown, skipping update");
                return; // Если сообщение уже было показано, выходим из метода
            }

            if (room.game.session.Players[0].realizedCreature == null ||
                room.game.cameras[0].hud == null ||
                room.game.cameras[0].hud.textPrompt == null)
            {
                //Debug.Log("KarmaCapTrigger: HUD or realizedCreature is null, skipping update");
                return;
            }

            if (player.slugcatStats.name == Plugin.TheVoid && player.KarmaCap == 4)
            {
                // Debug.Log("KarmaCapTrigger: Conditions met to show message");
                foreach (Message message in messageList)
                {
                    // Debug.Log("KarmaCapTrigger: Processing message - " + message.text);
                    string[] array = message.text.Split('/');
                    string text = "";
                    foreach (string s in array)
                        text += Custom.rainWorld.inGameTranslator.Translate(s);

                    room.game.cameras[0].hud.textPrompt.AddMessage(text, message.wait, message.time, true, ModManager.MMF);
                }

                saveData.messageShown = true; // Устанавливаем состояние
                SaveManager.Save(saveData); // Сохраняем состояние
                                            // Debug.Log("KarmaCapTrigger: Message shown and state saved");

                slatedForDeletetion = true;
                //Debug.Log("KarmaCapTrigger: Marked for deletion");
            }
        }

        private static void InitializeTargetRoomID(Room room)
        {
            // Добавляем лог для начала функции
            //Debug.Log("[TheVoid] Инициализация targetRoomID начата.");

            AbstractRoom targetRoom = room.world.GetAbstractRoom("SB_A14");
            if (targetRoom == null)
            {
                //Debug.LogError($"[TheVoid] Комната 'SB_A14' не существует.");
                throw new Exception($"[TheVoid] Room 'SB_A14' does not exist.");
            }

            targetRoomID = targetRoom.index;
            //Debug.Log($"[TheVoid] targetRoomID успешно инициализирован с индексом: {targetRoomID}");
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

            //Debug.Log("KarmaCapCheck: Init called");
            KarmaCapTrigger karmaTrigger = new(room, player,
                new KarmaCapTrigger.Message[]
                {
                    new("Your body is strong enough to climb the ceilings.", 0, 400),
                    new("Hold down the 'Up' and 'Direction' buttons to climb the ceiling.")
                }
            );

            room.AddObject(karmaTrigger);
            //Debug.Log("KarmaCapCheck: KarmaCapTrigger added to room");
        }
    }
}

public static class SaveManager
{
    private static string saveFilePath = Application.persistentDataPath + "/voidsavedata.json";

    public static SaveData Load()
    {
        //Debug.Log("SaveManager: Loading save data from " + saveFilePath);
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            //Debug.Log("SaveManager: Save data found and loaded");
            return JsonConvert.DeserializeObject<SaveData>(json);
        }
        //Debug.Log("SaveManager: No save data found, returning new SaveData");
        return new SaveData();
    }

    public static void Save(SaveData data)
    {
        //Debug.Log("SaveManager: Saving data to " + saveFilePath);
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(saveFilePath, json);
        //Debug.Log("SaveManager: Save complete");
    }

    [System.Serializable]
    public class SaveData
    {
        public bool messageShown;
    }
}