using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SlugBase.SaveData;

namespace VoidTemplate;

public static class SaveManager
{
	private const string uniqueprefix = "VoidSlugcat";
	private const string teleportationDone = uniqueprefix + "TeleportationDone";
	private const string messageShown = uniqueprefix + "MessageShown";
	private const string punishDeath = uniqueprefix + "NonPermaDeath";
	private const string punishPebble = uniqueprefix + "PunishFromPebble";
	private const string startClimbingMessageShown = uniqueprefix + "StartClimbingMessageShown";

	const string endingDone = uniqueprefix + "EndingDone";
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

	public static bool GetStartClimbingMessageShown(this SaveState saveState) => saveState.miscWorldSaveData.GetSlugBaseData().TryGet(startClimbingMessageShown, out bool shown) && shown;
	public static void SetStartClimbingMessageShown(this SaveState saveState, bool value) => saveState.miscWorldSaveData.GetSlugBaseData().Set(startClimbingMessageShown, value);

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
			if (ExternalSaveData.VoidDead && ExternalSaveData.VoidKarma11)
			{
                KarmaTokenAmount = 0;
                data.Set(KarmaToken, 0);
            }
			else
			{
                KarmaTokenAmount = 10;
                data.Set(KarmaToken, 10);
            }
		}
		return KarmaTokenAmount;
	}

	private const string CycleToken = uniqueprefix + "CycleToken";

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
	public static void SetVoidCatDead(this SaveState save, bool value)
	{
		save.miscWorldSaveData.GetSlugBaseData().Set(voidCatDead, value);
		ExternalSaveData.VoidDead = value;
    }
	public static bool GetVoidMeetMoon(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidMeetMoon, out bool dead) && dead;
	public static void SetVoidMeetMoon(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidMeetMoon, value);
	public static bool GetEndingEncountered(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(endingDone, out bool done) && done;
	public static void SetEndingEncountered(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(endingDone, value);
	public static int GetVoidExtraCycles(this SaveState save) => save.deathPersistentSaveData.GetSlugBaseData().TryGet(voidExtraCycles, out int extraCycles) ? extraCycles : 0;
	public static void SetVoidExtraCycles(this SaveState save, int value) => save.deathPersistentSaveData.GetSlugBaseData().Set(voidExtraCycles, value);


    private const string stomachPearls = uniqueprefix + "stomachPearls";
    public static Dictionary<int, List<string>> GetStomachPearls(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet<Dictionary<int, List<string>>>(stomachPearls, out var dic) ? dic : [];
    public static void SetStomachPearls(this SaveState save, Dictionary<int, List<string>> pearls) => save.miscWorldSaveData.GetSlugBaseData().Set(stomachPearls, pearls);

    //Pearls

    private const string voidMarkV2 = uniqueprefix + "VoidMarkV2";
    private const string voidMarkV3 = uniqueprefix + "VoidMarkV3";

    public static bool GetVoidMarkV2(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidMarkV2, out bool voidmark2) && voidmark2;
    public static void SetVoidMarkV2(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidMarkV2, value);

    public static bool GetVoidMarkV3(this SaveState save) => save.miscWorldSaveData.GetSlugBaseData().TryGet(voidMarkV3, out bool voidmark3) && voidmark3;
    public static void SetVoidMarkV3(this SaveState save, bool value) => save.miscWorldSaveData.GetSlugBaseData().Set(voidMarkV3, value);

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

	#region ConvulsionObjects
	const string convulsionList = "convulsionList";
	public static bool IsValidForAppearing(this SaveState save, string roomname) => !(save.miscWorldSaveData.GetSlugBaseData().TryGet(convulsionList, out List<string> list) && list.Contains(roomname));
	public static void DelistConvulsion(this SaveState save, string roomname)
	{
		var slugbase = save.miscWorldSaveData.GetSlugBaseData();
		List<string> list;
		if (slugbase.TryGet(convulsionList, out list)) list.Add(roomname);
		else list = [roomname];
		slugbase.Set(convulsionList, list);
	}
	#endregion

	public static class ExternalSaveData
	{
#nullable enable
		const string SaveFolder = "modsavedata";
		static string PathToSaves()
		{
			string path = Path.Combine(RWCustom.Custom.RootFolderDirectory(), SaveFolder, "lastwish");
			Directory.CreateDirectory(path);
			return path;
		}
		private static string FullPathOfSaveProperty(string id) => Path.Combine(PathToSaves(), id + ".json");
		/// <summary>
		/// Fetches a single entry associated with saveslot from save folder using ID.
		/// </summary>
		/// <typeparam name="T">Type of value being fetched</typeparam>
		/// <param name="id">ID of fetched stored value</param>
		/// <param name="defaultValue">Returned value in case it doesn't exist</param>
		/// <param name="saveslot">Saveslot to fetch data for</param>
		/// <returns></returns>
		private static T GetData<T>(string id, T defaultValue, int? saveslot = null)
		{
			int slot = saveslot ?? RWCustom.Custom.rainWorld.options.saveSlot;
			string path = FullPathOfSaveProperty(id);
			if (!File.Exists(path)) return defaultValue;
			string rawData = File.ReadAllText(path);
			Dictionary<int, T>? dataPerSave = JsonConvert.DeserializeObject<Dictionary<int, T>>(rawData, new JsonSerializerSettings
			{
				Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
				{
                    args.ErrorContext.Handled = true;
                }
			});
			if (dataPerSave is null
				|| !dataPerSave.TryGetValue(slot, out T result))
				return defaultValue;
			return result;
		}
		/// <summary>
		/// Sets data for specified saveslot, ignores saveslot number below 0 (expedition/safari)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id"></param>
		/// <param name="value"></param>
		/// <param name="saveslot">Optional saveslot number for which this should be set</param>
		private static void SetData<T>(string id, T value, int? saveslot = null)
		{
			int slot = saveslot ?? RWCustom.Custom.rainWorld.options.saveSlot;
			string path = FullPathOfSaveProperty(id);
			Dictionary<int, T>? dataPerSave = null;

			if (File.Exists(path))
				dataPerSave = JsonConvert.DeserializeObject<Dictionary<int, T>>(File.ReadAllText(path), new JsonSerializerSettings
                {
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                    }
                });

			if (dataPerSave is null)
			{
				dataPerSave = new();
			}

			dataPerSave[slot] = value;

			string rawData = JsonConvert.SerializeObject(dataPerSave);
			File.WriteAllText(path, rawData);
		}



		private const string VoidDeadString = "voiddead";
		public static bool VoidDead
		{
			get => GetData(VoidDeadString, false);
			set => SetData(VoidDeadString, value);
		}

		private const string VoidKarma11String = "voidkarma11";
        public static bool VoidKarma11
        {
            get => GetData(VoidKarma11String, false);
            set => SetData(VoidKarma11String, value);
        }

        private const string ViyLungExtendedString = "viylungextended";
        public static bool ViyLungExtended
        {
            get => GetData(ViyLungExtendedString, false);
            set => SetData(ViyLungExtendedString, value);
        }

        private const string ViyPoisonImmuneString = "viypoisonimmune";
        public static bool ViyPoisonImmune
        {
            get => GetData(ViyPoisonImmuneString, false);
            set => SetData(ViyPoisonImmuneString, value);
        }

    }


}
