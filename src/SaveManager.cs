using SlugBase.SaveData;

namespace VoidTemplate;

public static class SaveManager
{
    private const string uniqueprefix = "VoidSlugcat";
    private const string teleportationDone = uniqueprefix + "TeleportationDone";
    private const string messageShown = uniqueprefix + "MessageShown";
    private const string lastMeetCycles = uniqueprefix + "LastMeetCycles";
    private const string pebblesPearlsEaten = uniqueprefix + "PebblesPearlsEaten";

    public static bool GetTeleportationDone(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(teleportationDone, out bool done) && done;
    public static void SetTeleportationDone(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(teleportationDone, value);

    public static bool GetMessageShown(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(messageShown, out bool shown) && shown;
    public static void SetMessageShown(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(messageShown, value);

    public static int GetLastMeetCycles(this SaveState save)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if(!data.TryGet(lastMeetCycles, out int cycles))
        {
            cycles = 0;
            data.Set(lastMeetCycles, 0);
        }
        return cycles;
    }
    public static void SetLastMeetCycles(this SaveState save, int cycles) => save.miscWorldSaveData.GetSlugBaseData().Set(lastMeetCycles, cycles);

    public static int GetPebblesPearlsEaten(this SaveState save)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if (!data.TryGet(pebblesPearlsEaten, out int eatenPearlsAmount))
        {
            eatenPearlsAmount = 0;
            data.Set(pebblesPearlsEaten, 0);
        }
        return eatenPearlsAmount;
    }
    public static void SetPebblesPearlsEaten(this SaveState save, int amount) => save.miscWorldSaveData.GetSlugBaseData().Set(pebblesPearlsEaten, amount);
}
