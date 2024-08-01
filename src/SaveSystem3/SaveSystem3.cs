using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;

namespace VoidTemplate.SaveSystem3;

internal static class SaveSystem3
{
    #region hooks
    public static void Hook()
    {
        //Assign saveslot when the game is launched
        On.Menu.InitializationScreen.Update += InitializationScreen_Update;
        //Assign saveslot when saveslot is changed
        On.Menu.OptionsMenu.SetCurrentlySelectedOfSeries += OptionsMenu_SetCurrentlySelectedOfSeries;

        //save all data when vanilla saves it
        On.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString;
        On.MiscWorldSaveData.ToString += MiscWorldSaveData_ToString;
        On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;

    }

    private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
    {
        Parser.WriteDataToDisk(data: SaveWipeResistantSaveData, Datatype.SaveWipeResistant, SaveSlot);
        return orig(self);
    }

    private static string MiscWorldSaveData_ToString(On.MiscWorldSaveData.orig_ToString orig, MiscWorldSaveData self)
    {
        Parser.WriteDataToDisk(data: SaveWipeResistantSaveData, Datatype.MiscWorld, SaveSlot);
        return orig(self);
    }
    private static string DeathPersistentSaveData_SaveToString(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
    {
        Parser.WriteDataToDisk(data: DeathPersistentSaveData, Datatype.DeathPersistent, SaveSlot);
        return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);

    }

    private static void OptionsMenu_SetCurrentlySelectedOfSeries(On.Menu.OptionsMenu.orig_SetCurrentlySelectedOfSeries orig, Menu.OptionsMenu self, string series, int to)
    {
        orig(self, series, to);
        if (series == "SaveSlot")
        {
            SaveSlot = to;
            UpdateSaveData();
        }
    }

    static bool initialized = false;
    private static void InitializationScreen_Update(On.Menu.InitializationScreen.orig_Update orig, Menu.InitializationScreen self)
    {
        orig(self);
        if(self.manager.rainWorld.OptionsReady && !initialized)
        {
            SaveSlot = self.manager.rainWorld.options.saveSlot;
            UpdateSaveData();
            initialized = true;
        }
    }
    #endregion

    public bool TryGet<T>(string key, Datatype datatype, out T value)
    {
        value = default(T);
        switch(datatype)
        {
            case Datatype.DeathPersistent:
                {
                    
                    break;
                }
        }
    }

    static int SaveSlot = -1;
    static Dictionary<string, string> SaveDataConstantForCycle;
    static Dictionary<string, string> SaveDataChangingForCycle;
    static Dictionary<string, string> DeathPersistentSaveData;
    static Dictionary<string, string> SaveWipeResistantSaveData;
    static void UpdateSaveData()
    {
        SaveDataConstantForCycle = Parser.GetDataFromDisk(SaveSlot, Datatype.MiscWorld);
        DeathPersistentSaveData = Parser.GetDataFromDisk(SaveSlot, Datatype.DeathPersistent);
        SaveWipeResistantSaveData = Parser.GetDataFromDisk(SaveSlot, Datatype.SaveWipeResistant);
    }

    static Datatype[] datatypes = [Datatype.SaveWipeResistant, Datatype.DeathPersistent, Datatype.MiscWorld];
    public enum Datatype
    {
        SaveWipeResistant,
        DeathPersistent,
        MiscWorld
    }
    static Dictionary<Datatype, string> DatatypeToString = new()
    {
        { Datatype.SaveWipeResistant, "SaveWipeResistant" },
        { Datatype.MiscWorld, "MiscWorld" },
        { Datatype.DeathPersistent, "DeathPersistent" }
    };
ы
    static class Parser
    {
        static class DiskCommunicator
        {
            const string filepattern = "SaveData";
            static string FolderPath => string.Concat(ModManager.ActiveMods.FirstOrDefault(x => x.id == "thevoid.liebeasano").path, "SaveData");
            static string FullFilePath(int saveslotnum, Datatype type) => string.Concat(FolderPath, DatatypeToString[type] + "_SaveSlot" + saveslotnum);
            internal static string[] LoadSaveData(int saveslot, Datatype type)
            {
                if (File.Exists(FullFilePath(saveslot, type))) 
                    File.ReadAllLines(FullFilePath(saveslot, type));
                return [];

            }
            internal static void SaveSaveData(int saveslot, Datatype type, string[] data) => File.WriteAllLines(FullFilePath(saveslot, type), data);
        }
        const string divider = "<DIV>";
        public static void WriteDataToDisk(Dictionary<string, string> data, Datatype type, int slot) => DiskCommunicator.SaveSaveData(slot, type, PrepareForWrite(data));
        public static Dictionary<string, string> GetDataFromDisk(int slot, Datatype type)
        {
            return ParseRawData(DiskCommunicator.LoadSaveData(slot, type));
        }
        static string[] PrepareForWrite(Dictionary<string, string> data)
        {
            List<string> kvp = [];
            foreach (var key in data.Keys)
            {
                kvp.Add(key + divider + data[key]);
            }
            return [..kvp];
        }
        static Dictionary<string, string> ParseRawData(string[] strings)
        {
            Dictionary<string, string> dic = [];
            Array.ForEach(strings, str =>
            {
                var arr = str.Split(new string[] { divider }, StringSplitOptions.None);
                dic.Add(arr[0], arr[1]);
            });
            return dic;
        }
    }
}
