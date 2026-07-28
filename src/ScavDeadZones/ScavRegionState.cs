using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace VoidTemplate.ScavDeadZones
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ScavRegionState
    {
        public string regionName;

        public string[] regions;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool isDeadRegion;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int migrationScavs;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int migrationEleteScavs;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool migrated;

        public float deadCount = 1;

        [JsonIgnore]
        public bool killScavs = false;

        [JsonConstructor]
        private ScavRegionState()
        {
        }

        public ScavRegionState(string regionName, World world)
        {
            this.regionName = regionName;
            if (world != null)
            {
                UpdateRegions(world);
            }
        }

        public void UpdateRegions(World world)
        {
            if (!isDeadRegion)
            {
                List<string> regions = [];
                for (int i = 0; i < world.gates.Length; i++)
                {
                    string gateName = world.GetAbstractRoom(world.gates[i]).name;
                    if (!world.DisabledMapRooms.Contains(gateName))
                    {
                        string[] regionNames = Regex.Split(gateName, "_");
                        if (regionNames.Length == 3)
                        {
                            for (int j = 1; j < 3; j++)
                            {
                                if (regionNames[j] != world.name && !regions.Contains(regionNames[j]))
                                {
                                    regions.Add(regionNames[j]);
                                    break;
                                }
                            }
                        }
                    }
                }
                this.regions = [.. regions];
            }
        }

        public void CycleTick(SaveState saveState)
        {
            if (saveState.progression.regionNames.Contains(regionName) && !isDeadRegion && deadCount < 1)
            {
                if (!killScavs)
                {
                    deadCount = Mathf.Clamp01(deadCount + 0.03f);
                }
                else if (deadCount <= 0)
                {
                    isDeadRegion = true;
                    MigrateToOtherRegions(saveState);
                }
            }
        }

        public void MigrateToOtherRegions(SaveState saveState)
        {
            if (isDeadRegion)
            {
                for (int i = 0; i < regions.Length; i++)
                {
                    var state = saveState.GetOrCreateScavRegionState(regions[i]);
                    if (!state.isDeadRegion)
                    {
                        state.migrationScavs += 3;
                        state.migrationEleteScavs += 1;
                    }
                }
                regions = null;
            }
        }
    }
}
