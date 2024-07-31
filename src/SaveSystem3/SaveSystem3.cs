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
    public static void Hook()
    {
        On.Menu.InitializationScreen.Update += InitializationScreen_Update;
    }

    
    static bool initialized = false;
    private static void InitializationScreen_Update(On.Menu.InitializationScreen.orig_Update orig, Menu.InitializationScreen self)
    {
        orig(self);
        if(self.manager.rainWorld.OptionsReady && !initialized)
        {
            SaveSlot = self.manager.rainWorld.options.saveSlot;
        }
    }


    static int SaveSlot = -1;
    static Dictionary<string, string> SaveData = new();
    static Dictionary<string, string> DeathPersistentSaveData = new();
    static Dictionary<string, string> MiscWorldSaveData = new();

    public enum Datatype
    {
        Normal,
        DeathPersistent,
        MiscWorld
    }

    public static void SaveImminentData()
    {
        Parser.WriteDataToDisk([SaveData, DeathPersistentSaveData, MiscWorldSaveData], SaveSlot);
    }





    static class Parser
    {
        static class DiskCommunicator
        {
            const string filepattern = "TheVoidSaveData";
            static string FolderPath => string.Concat(ModManager.ActiveMods.FirstOrDefault(x => x.id == "thevoid.liebeasano").path, "saveslot");
            static string FullFilePath(int saveslotnum) => string.Concat(FolderPath, filepattern + "_SaveSlot" + saveslotnum);
            internal static string[] LoadSaveData(int saveslot) => File.ReadAllLines(FullFilePath(saveslot));
            internal static void SaveSaveData(int saveslot, string[] data) => File.WriteAllLines(FullFilePath(saveslot), data);
        }
        const string divider = "<DIV>";


        public static void WriteDataToDisk(Dictionary<string, string>[] data, int slot)
        {
            List<String> strings = new List<String>();
            Array.ForEach(data, data =>
            {
                var localstrings = PrepareForWrite(data);
                localstrings.ForEach(x => strings.Add(x));
                strings.Add("<MAJORDATADIV>");
            });
            DiskCommunicator.SaveSaveData(SaveSlot, [.. strings]);
        }
        public static Dictionary<string, string> GetDataFromDisk(int slot)
        {
            return ParseRawData(DiskCommunicator.LoadSaveData(slot));
        }
        
        static List<string> PrepareForWrite(Dictionary<string, string> data)
        {
            List<string> kvp = [];
            foreach (var key in data.Keys)
            {
                kvp.Add(key + divider + data[key]);
            }
            return kvp;
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
