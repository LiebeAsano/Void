using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Permissions;

namespace VoidTemplate.SaveSystem3;

internal static class SaveSystem3
{
    static Dictionary<string, string> SaveData = new();
    static Dictionary<string, string> DeathPersistentSaveData = new();
    static Dictionary<string, string> MiscWorldSaveData = new();

    public enum Datatype
    {
        Normal,
        DeathPersistent,
        MiscWorld
    }
    public static SaveSystem3()
    {
        
        
    }
    public static void SaveImminentData(int saveslot)
    {
        Parser.WriteDataToDisk(SaveData, saveslot);
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

        public static void WriteDataToDisk(Dictionary<string, string> data, int slot) => DiskCommunicator.SaveSaveData(slot, PrepareForWrite(data));
        public static Dictionary<string, string> GetDataFromDisk(int slot) => ParseRawData(DiskCommunicator.LoadSaveData(slot));
        
        static string[] PrepareForWrite(Dictionary<string, string> data)
        {
            List<string> kvp = [];
            foreach (var key in data.Keys)
            {
                kvp.Add(key + divider + data[key]);
            }
            return [.. kvp];
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
