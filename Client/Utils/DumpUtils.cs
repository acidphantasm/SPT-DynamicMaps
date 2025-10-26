using System.Collections.Generic;
using System.IO;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using EFT;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using Newtonsoft.Json;
using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class DumpUtils
    {
        private const string ExtractCategory = "Extract";
        private const string ExtractImagePath = "Markers/exit.png";

        private const string SecretCategory = "Secret";
        private const string SecretImagePath = "Markers/exit.png";

        private const string TransitCategory = "Transit";
        private const string TransitImagePath = "Makers/transit.png";
        
        private static readonly Color ExtractScavColor = Color.Lerp(Color.yellow, Color.red, 0.5f);
        private static readonly Color TransitColor = Color.Lerp(Color.yellow, Color.red, 0.6f);
        private static readonly Color SecretColor = new Color(0.1f, 0.6f, 0.6f);
        private static readonly Color ExtractPmcColor = Color.green;

        private const string SwitchCategory = "Switch";
        private const string SwitchImagePath = "Markers/lever.png";

        private const string LockedDoorCategory = "Locked Door";
        private const string LockedDoorImagePath = "Markers/door_with_lock.png";
        private static readonly Color LockedDoorColor = Color.yellow;

        public static void DumpExtracts()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;
            var pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;
            var secretExfils = gameWorld.ExfiltrationController.SecretExfiltrationPoints;

            var dump = new List<MapMarkerDef>();

            if (scavExfils is not null)
            {

                foreach (var scavExfil in scavExfils)
                {
                    var dumped = new MapMarkerDef
                    {
                        Category = ExtractCategory,
                        ShowInRaid = false,
                        ImagePath = ExtractImagePath,
                        Text = scavExfil.Settings.Name.BSGLocalized(),
                        Position = MathUtils.ConvertToMapPosition(scavExfil.transform),
                        Color = ExtractScavColor
                    };

                    dump.Add(dumped);
                }
            }

            foreach (var pmcExfil in pmcExfils)
            {
                var dumped = new MapMarkerDef
                {
                    Category = ExtractCategory,
                    ShowInRaid = false,
                    ImagePath = ExtractImagePath,
                    Text = pmcExfil.Settings.Name.BSGLocalized(),
                    Position = MathUtils.ConvertToMapPosition(pmcExfil.transform),
                    Color = ExtractPmcColor
                };

                dump.Add(dumped);
            }

            foreach (var transit in LocationScene.GetAllObjects<TransitPoint>(true))
            {
                var dumped = new MapMarkerDef
                {
                    Category = TransitCategory,
                    ShowInRaid = false,
                    ImagePath = TransitImagePath,
                    Text = transit.parameters.description.BSGLocalized(),
                    Position = MathUtils.ConvertToMapPosition(transit.transform),
                    Color = TransitColor
                };
                
                dump.Add(dumped);
            }

            if (secretExfils is not null)
            {
                foreach (var secret in secretExfils)
                {
                    var dumped = new MapMarkerDef
                    {
                        Category = SecretCategory,
                        ShowInRaid = false,
                        ImagePath = SecretImagePath,
                        Text = secret.Settings.Name.BSGLocalized(),
                        Position = MathUtils.ConvertToMapPosition(secret.transform),
                        Color = SecretColor
                    };

                    dump.Add(dumped);
                }
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-extracts.json"), dumpString);

            Plugin.Log.LogInfo("Dumped extracts");
        }

        public static void DumpSwitches()
        {
            var switches = GameObject.FindObjectsOfType<Switch>();
            var dump = new List<MapMarkerDef>();

            foreach (var @switch in switches)
            {
                if (!@switch.Operatable || !@switch.HasAuthority)
                {
                    continue;
                }

                var dumped = new MapMarkerDef
                {
                    Category = SwitchCategory,
                    ImagePath = SwitchImagePath,
                    Text = @switch.name,
                    Position = MathUtils.ConvertToMapPosition(@switch.transform)
                };

                dump.Add(dumped);
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-switches.json"), dumpString);

            Plugin.Log.LogInfo("Dumped switches");
        }

        public static void DumpLocks()
        {
            var objects = GameObject.FindObjectsOfType<WorldInteractiveObject>();
            var dump = new List<MapMarkerDef>();
            var i = 1;

            foreach (var locked in objects)
            {
                if (string.IsNullOrEmpty(locked.KeyId) || !locked.Operatable)
                {
                    continue;
                }

                var dumped = new MapMarkerDef
                {
                    Text = $"door {i++}",
                    Category = LockedDoorCategory,
                    ImagePath = LockedDoorImagePath,
                    Position = MathUtils.ConvertToMapPosition(locked.transform),
                    AssociatedItemId = locked.KeyId,
                    Color = LockedDoorColor
                };

                dump.Add(dumped);
            }

            var mapName = GameUtils.GetCurrentMapInternalName();
            var dumpString = JsonConvert.SerializeObject(dump, Formatting.Indented);
            File.WriteAllText(Path.Combine(Plugin.Path, $"{mapName}-locked.json"), dumpString);

            Plugin.Log.LogInfo("Dumped locks");
        }
    }
}
