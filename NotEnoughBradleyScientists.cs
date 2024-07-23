/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Not Enough Bradley Scientists", "VisEntities", "1.1.0")]
    [Description("Changes how many scientists spawn when the Bradley APC is attacked.")]
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

            [JsonProperty("Road Bradley")]
            public BradleyConfig RoadBradley { get; set; }

            [JsonProperty("Launch Site Bradley")]
            public BradleyConfig LaunchSiteBradley { get; set; }
        }

        public class BradleyConfig
        {
            [JsonProperty("Minimum Number Of Scientists To Spawn")]
            public int MinimumNumberOfScientistsToSpawn { get; set; }

            [JsonProperty("Maximum Number Of Scientists To Spawn")]
            public int MaximumNumberOfScientistsToSpawn { get; set; }
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

            if (string.Compare(_config.Version, "1.1.0") < 0)
            {
                _config = defaultConfig;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                RoadBradley = new BradleyConfig
                {
                    MinimumNumberOfScientistsToSpawn = 4,
                    MaximumNumberOfScientistsToSpawn = 6
                },
                LaunchSiteBradley = new BradleyConfig
                {
                    MinimumNumberOfScientistsToSpawn = 4,
                    MaximumNumberOfScientistsToSpawn = 6
                }
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

        private void OnScientistSpawnPositionsGenerated(BradleyAPC bradley, BaseEntity attacker, List<GameObjectRef> scientistPrefabs, List<Vector3> spawnPositions)
        {
            if (bradley == null)
                return;

            if (spawnPositions == null || spawnPositions.Count == 0)
                return;

            if (scientistPrefabs == null || scientistPrefabs.Count == 0)
                return;
           
            BradleyConfig bradleyConfig = _config.LaunchSiteBradley;
            if (bradley.RoadSpawned)
                bradleyConfig = _config.RoadBradley;

            int numberOfScientistsToSpawn = Random.Range(bradleyConfig.MinimumNumberOfScientistsToSpawn, bradleyConfig.MaximumNumberOfScientistsToSpawn + 1);

            ResizeSpawnPositionsList(spawnPositions, numberOfScientistsToSpawn);
            ResizeScientistPrefabsList(scientistPrefabs, numberOfScientistsToSpawn);
        }

        #endregion Oxide Hooks

        #region Harmony Patches

        [HarmonyPatch(typeof(BradleyAPC), "CanDeployScientists")]
        public static class BradleyAPC_CanDeployScientists_Patch
        {
            public static void Postfix(BradleyAPC __instance, BaseEntity attacker, List<GameObjectRef> scientistPrefabs, List<Vector3> spawnPositions)
            {
                Interface.CallHook("OnScientistSpawnPositionsGenerated", __instance, attacker, scientistPrefabs, spawnPositions);
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