using SlugBase.SaveData;

namespace VoidTemplate;

public static class SaveManager
{
    private const string uniqueprefix = "VoidSlugcat";
    private const string teleportationDone = uniqueprefix + "TeleportationDone";
    private const string messageShown = uniqueprefix + "MessageShown";

    public const string endingDone = uniqueprefix + "EndingDone";
    private const string voidCatDead = uniqueprefix + "VoidCatDead";

    public static bool GetTeleportationDone(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(teleportationDone, out bool done) && done;
    public static void SetTeleportationDone(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(teleportationDone, value);

    public static bool GetMessageShown(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(messageShown, out bool shown) && shown;
    public static void SetMessageShown(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(messageShown, value);


    #region oracle data
    private const string lastMeetCycles = uniqueprefix + "LastMeetCycles";
    private const string pebblesPearlsEaten = uniqueprefix + "PebblesPearlsEaten";
    private const string moonPearlsEaten = uniqueprefix + "MoonPearlsEaten";
    private const string VisitedFP6times = uniqueprefix + "visitedFP6times";
    #region last meet
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
    #endregion
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

    public static int GetMoonPearlsEaten(this SaveState save)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if (!data.TryGet(moonPearlsEaten, out int eatenPearlsAmount))
        {
            eatenPearlsAmount = 0;
            data.Set(moonPearlsEaten, 0);
        }
        return eatenPearlsAmount;
    }
    public static void SetMoonPearlsEaten(this SaveState save, int amount) => save.miscWorldSaveData.GetSlugBaseData().Set(moonPearlsEaten, amount);
    #endregion

    public static bool GetVoidCatDead(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidCatDead, out bool dead) && dead;
    public static void SetVoidCatDead(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidCatDead, value);

    public static bool GetEndingEncountered(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(endingDone, out bool done) && done;
    public static void SetEndingEncountered(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(endingDone, value);

    #region Dreams scheduled/shown
    private const string dream = "Dream";
    public enum Dream
    {
        None,
        Farm,
        Moon,
        NSH,
        Pebble,
        Rot,
        Sky,
        Sub,
        VoidBody,
        VoidHeart,
        VoidNSH,
        VoidSea
    }
    public struct DreamData
    {
        public DreamData(bool HasShowConditions = false, bool WasShown = false)
        {
            this.HasShowConditions = HasShowConditions;
            this.WasShown = WasShown;
        }
        public bool HasShowConditions;
        public bool WasShown;
        public override string ToString()
        {
            return $"met conditions: {HasShowConditions}, was shown: {WasShown}";
        }
    }
    public static DreamData GetDreamData(this SaveState save, Dream dream)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if(!data.TryGet<DreamData>(uniqueprefix + dream.ToString() + SaveManager.dream, out var dreamdata))
        {
            data.Set(uniqueprefix + dream.ToString() + dream, new DreamData());
            return new DreamData();
        }
        return dreamdata;
    }
    public static void SetDreamData(this SaveState save, Dream dream, DreamData data)
    {
        save.miscWorldSaveData.GetSlugBaseData().Set(uniqueprefix+dream.ToString() + SaveManager.dream, data);
    }
    /// <summary>
    /// Request dream to be shown next time (if it was already shown, it will enlist it again)
    /// </summary>
    /// <param name="save"></param>
    /// <param name="dream"></param>
    public static void EnlistDreamInShowQueue(this SaveState save, Dream dream) => save.SetDreamData(dream, new DreamData(true));
    #endregion

}
