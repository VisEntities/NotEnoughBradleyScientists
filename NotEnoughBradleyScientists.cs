using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Not Enough Bradley Scientists", "VisEntities", "1.0.0")]
    [Description(" ")]
    public class NotEnoughBradleyScientists : RustPlugin
    {
        #region Fields

        private static NotEnoughBradleyScientists _plugin;
        private static Configuration _config;
        private Harmony _harmony;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Number Of Scientists To Spawn")]
            public int NumberOfScientistsToSpawn { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                NumberOfScientistsToSpawn = 6
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            _harmony = new Harmony(Name + "PATCH");
            _harmony.PatchAll();
        }

        private void Unload()
        {
            _harmony.UnpatchAll(Name + "PATCH");
            _config = null;
            _plugin = null;
        }

        private void OnScientistSpawnPositionsGenerated(BaseEntity attacker, List<GameObjectRef> scientistPrefabs, List<Vector3> spawnPositions)
        {
            ResizeSpawnPositionsList(spawnPositions, _config.NumberOfScientistsToSpawn);
            ResizeScientistPrefabsList(scientistPrefabs, _config.NumberOfScientistsToSpawn);
        }

        #endregion Oxide Hooks

        #region Harmony Patches

        [HarmonyPatch(typeof(BradleyAPC), "CanDeployScientists")]
        public static class BradleyAPC_CanDeployScientists_Patch
        {
            public static void Postfix(BaseEntity attacker, List<GameObjectRef> scientistPrefabs, List<Vector3> spawnPositions)
            {
                Interface.CallHook("OnScientistSpawnPositionsGenerated", attacker, scientistPrefabs, spawnPositions);
            }
        }

        #endregion Harmony Patches

        #region Spawn Position Cloning

        private void ResizeSpawnPositionsList(List<Vector3> spawnPositions, int targetCount)
        {
            int currentCount = spawnPositions.Count;

            if (currentCount < targetCount)
            {
                int positionsToAdd = targetCount - currentCount;
                for (int i = 0; i < positionsToAdd; i++)
                {
                    Vector3 newPosition = spawnPositions[i % currentCount];
                    spawnPositions.Add(newPosition);
                }
            }
            else if (currentCount > targetCount)
            {
                for (int i = currentCount - 1; i >= targetCount; i--)
                {
                    spawnPositions.RemoveAt(i);
                }
            }
        }

        #endregion Spawn Position Cloning

        #region Scientist Prefab Duplication

        private void ResizeScientistPrefabsList(List<GameObjectRef> scientistPrefabs, int targetCount)
        {
            int currentCount = scientistPrefabs.Count;

            if (currentCount < targetCount)
            {
                for (int i = currentCount; i < targetCount; i++)
                {
                    scientistPrefabs.Add(scientistPrefabs[i % currentCount]);
                }
            }
            else if (currentCount > targetCount)
            {
                for (int i = currentCount - 1; i >= targetCount; i--)
                {
                    scientistPrefabs.RemoveAt(i);
                }
            }
        }

        #endregion Scientist Prefab Duplication
    }
}