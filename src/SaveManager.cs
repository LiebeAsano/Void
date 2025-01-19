using System.Collections;
using SlugBase.SaveData;

namespace VoidTemplate;

public static class SaveManager
{
	private const string uniqueprefix = "VoidSlugcat";
	private const string teleportationDone = uniqueprefix + "TeleportationDone";
	private const string messageShown = uniqueprefix + "MessageShown";
	private const string punishDeath = uniqueprefix + "NonPermaDeath";
    private const string punishPebble = uniqueprefix + "PunishFromPebble";

    public const string endingDone = uniqueprefix + "EndingDone";
	private const string voidCatDead = uniqueprefix + "VoidCatDead";
    private const string voidMeetMoon = uniqueprefix + "VoidMeetMoon";
    private const string voidExtraCycles = uniqueprefix + "ExtraCycles";
	private const string hasHadFirstCycleAsViy = uniqueprefix + "ViyFirstCycle";


	public static bool GetViyFirstCycle(this SaveState saveState) =>
		saveState.miscWorldSaveData.GetSlugBaseData().TryGet(hasHadFirstCycleAsViy, out bool h) && h;
	public static void SetViyFirstCycle(this SaveState saveState, bool value) => saveState.miscWorldSaveData.GetSlugBaseData().Set(hasHadFirstCycleAsViy, value);
    public static bool GetTeleportationDone(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(teleportationDone, out bool done) && done;
	public static void SetTeleportationDone(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(teleportationDone, value);

	public static bool GetMessageShown(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(messageShown, out bool shown) && shown;
	public static void SetMessageShown(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(messageShown, value);

	private const string KarmaToken = uniqueprefix + "KarmaToken";

	public static void SetKarmaToken(this SaveState save, int amount) => save.deathPersistentSaveData.GetSlugBaseData().Set(KarmaToken, amount);

	public static int GetKarmaToken(this SaveState save)
	{
		return save.deathPersistentSaveData.GetKarmaToken();
	}
	public static int GetKarmaToken(this DeathPersistentSaveData save)
	{
		var data = save.GetSlugBaseData();
		if (!data.TryGet(KarmaToken, out int KarmaTokenAmount))
		{
			KarmaTokenAmount = 10;
			data.Set(KarmaToken, 10);
		}
		return KarmaTokenAmount;
	}

    private const string CycleToken = uniqueprefix + "CycleToken";

    public static void SetCycleToken(this SaveState save, int amount) => save.deathPersistentSaveData.GetSlugBaseData().Set(CycleToken, amount);

    public static int GetCycleToken(this SaveState save)
    {
        return save.deathPersistentSaveData.GetCycleToken();
    }
    public static int GetCycleToken(this DeathPersistentSaveData save)
    {
        var data = save.GetSlugBaseData();
        if (!data.TryGet(CycleToken, out int CycleTokenAmount))
        {
            CycleTokenAmount = 10;
            data.Set(CycleToken, 10);
        }
        return CycleTokenAmount;
    }
    public static bool GetPunishNonPermaDeath(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(punishDeath, out bool choose) && choose;

	public static void SetPunishNonPermaDeath(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(punishDeath, value);

    public static bool GetPunishFromPebble(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(punishPebble, out bool choose) && choose;

    public static void SetPunishFromPebble(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(punishPebble, value);

    #region oracle data
    private const string lastMeetCycles = uniqueprefix + "LastMeetCycles";
    private const string encountersWithMark = uniqueprefix + "EncountersWithMark";
    private const string pebblesPearlsEaten = uniqueprefix + "PebblesPearlsEaten";
	#region last meet
	public static int GetLastMeetCycles(this SaveState save)
	{
		var data = save.miscWorldSaveData.GetSlugBaseData();
		if (!data.TryGet(lastMeetCycles, out int cycles))
		{
			cycles = 0;
			data.Set(lastMeetCycles, 0);
		}
		return cycles;
	}
	public static void SetLastMeetCycles(this SaveState save, int cycles) => save.miscWorldSaveData.GetSlugBaseData().Set(lastMeetCycles, cycles);
    public static int GetEncountersWithMark(this SaveState save)
    {
        var data = save.miscWorldSaveData.GetSlugBaseData();
        if (!data.TryGet(encountersWithMark, out int cycles))
        {
            cycles = 0;
            data.Set(encountersWithMark, 0);
        }
        return cycles;
    }
    public static void SetEncountersWithMark(this SaveState save, int cycles) => save.miscWorldSaveData.GetSlugBaseData().Set(encountersWithMark, cycles);
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
	#endregion

	public static bool GetVoidCatDead(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidCatDead, out bool dead) && dead;
	public static void SetVoidCatDead(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidCatDead, value);
    public static bool GetVoidMeetMoon(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidMeetMoon, out bool dead) && dead;
    public static void SetVoidMeetMoon(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidMeetMoon, value);
    public static bool GetEndingEncountered(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(endingDone, out bool done) && done;
	public static void SetEndingEncountered(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(endingDone, value);
	public static int GetVoidExtraCycles(this SaveState save) => save.deathPersistentSaveData.GetSlugBaseData().TryGet(voidExtraCycles, out int extraCycles) ? extraCycles : 0;
	public static void SetVoidExtraCycles(this SaveState save, int value) => save.deathPersistentSaveData.GetSlugBaseData().Set(voidExtraCycles, value);

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
		if (!data.TryGet<DreamData>(uniqueprefix + dream.ToString() + SaveManager.dream, out var dreamdata))
		{
			data.Set(uniqueprefix + dream.ToString() + dream, new DreamData());
			return new DreamData();
		}
		return dreamdata;
	}
	public static void SetDreamData(this SaveState save, Dream dream, DreamData data)
	{
		save.miscWorldSaveData.GetSlugBaseData().Set(uniqueprefix + dream.ToString() + SaveManager.dream, data);
	}
	/// <summary>
	/// Request dream to be shown next time (if it was already shown, it will enlist it again)
	/// </summary>
	/// <param name="save"></param>
	/// <param name="dream"></param>
	public static void ForceEnlistDreamInShowQueue(this SaveState save, Dream dream) => save.SetDreamData(dream, new DreamData(true));
	public static void EnlistDreamIfNotSeen(this SaveState save, Dream dream)
	{
		if (!save.GetDreamData(dream).WasShown)
		{
			save.ForceEnlistDreamInShowQueue(dream);
		}
	}
	#endregion

}
