using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT.UI;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public class MapSelectDropdown : MonoBehaviour
    {
        private static readonly HashSet<string> AcceptableJsonExtensions = new HashSet<string>{ "json", "jsonc" };

        public event Action<MapDef> OnMapSelected;
        public RectTransform RectTransform => gameObject.transform as RectTransform;

        private DropDownBox _dropdown;
        private readonly Dictionary<string, DateTime> _writeTimes = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, MapDef> _mapDefs = new Dictionary<string, MapDef>();
        private List<MapDef> _selectableMapDefs;
        private string _nameFilter = null;

        public static MapSelectDropdown Create(GameObject prefab, Transform parent)
        {
            var go = GameObject.Instantiate(prefab);
            go.name = "MapSelectDropdown";

            var rectTransform = go.GetRectTransform();
            rectTransform.SetParent(parent);
            rectTransform.localScale = Vector3.one;

            var dropdown = go.AddComponent<MapSelectDropdown>();
            return dropdown;
        }

        private void Awake()
        {
            _dropdown = gameObject.GetComponentInChildren<DropDownBox>();
            _dropdown.SetLabelText("Select a Map");
        }

        private void OnSelectDropdownMap(int index)
        {
            if (_selectableMapDefs == null || index < 0 || index >= _selectableMapDefs.Count)
                return;
            
            OnMapSelected?.Invoke(_selectableMapDefs[index]);
        }

        private void ChangeAvailableMapDefs(IEnumerable<MapDef> mapDefs)
        {
            // check if these are the same map defs as before
            var mapDefList = mapDefs.ToList();
            if (_selectableMapDefs != null && mapDefList.SequenceEqual(_selectableMapDefs))
            {
                return;
            }

            _selectableMapDefs = mapDefList.ToList();

            if (_selectableMapDefs.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            _dropdown.Show(_selectableMapDefs.Select(def => def.DisplayName));

            // TODO: this references a BSG method of GInterface390
            _dropdown.OnValueChanged.Subscribe(OnSelectDropdownMap);

            gameObject.SetActive(_selectableMapDefs.Count > 1);
        }

        public void LoadMapDefsFromPath(string relPath)
        {
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var absolutePath = Path.Combine(Plugin.Path, relPath);
            if (Directory.Exists(absolutePath))
            {
                foreach (var file in Directory.EnumerateFiles(absolutePath, "*.*", SearchOption.AllDirectories))
                {
                    if (AcceptableJsonExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLowerInvariant()))
                    {
                        paths[file] = null;
                    }
                }
            }

            var addonsPath = Path.Combine(Plugin.Path, "Addons");
            if (Directory.Exists(addonsPath))
            {
                foreach (var addon in Directory.EnumerateDirectories(addonsPath))
                {
                    foreach (var map in Directory.EnumerateDirectories(addon))
                    {
                        foreach (var file in Directory.EnumerateFiles(map))
                        {
                            if (AcceptableJsonExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLowerInvariant()))
                            {
                                paths[file] = Path.GetFileName(addon);
                            }
                        }
                    }
                }
            }

            foreach (var removed in _mapDefs.Keys.Where(p => !paths.ContainsKey(p)).ToList())
            {
                _mapDefs.Remove(removed);
                _writeTimes.Remove(removed);
            }

            foreach (var entry in paths.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                var writeTime = File.GetLastWriteTimeUtc(entry.Key);
                if (_writeTimes.TryGetValue(entry.Key, out var previous) && previous == writeTime)
                    continue;

                var mapDef = MapDef.LoadFromPath(entry.Key, entry.Value);
                if (mapDef == null)
                {
                    _mapDefs.Remove(entry.Key);
                    continue;
                }

                _writeTimes[entry.Key] = writeTime;
                _mapDefs[entry.Key] = mapDef;
            }

            RegisterMapPreferences();
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        private Dictionary<string, MapDef> GetMapChoices(string location)
        {
            return _mapDefs.Values
                .Where(m => m.MapInternalNames.Contains(location, StringComparer.OrdinalIgnoreCase))
                .OrderBy(m => m.IsAddon).ThenBy(m => m.SelectionId, StringComparer.OrdinalIgnoreCase)
                .GroupBy(m => m.DisplayName)
                .SelectMany(group => group.Select(m => new
                {
                    Name = group.Count() == 1 ? m.DisplayName : $"{m.DisplayName} [{m.SelectionId}]",
                    Map = m
                }))
                .ToDictionary(entry => entry.Name, entry => entry.Map);
        }

        private void RegisterMapPreferences()
        {
            foreach (var location in _mapDefs.Values.SelectMany(m => m.MapInternalNames)
                .Where(nameOf => !string.IsNullOrWhiteSpace(nameOf)).Distinct(StringComparer.OrdinalIgnoreCase))
                Settings.RegisterMapPreference(location, GetMapChoices(location).Keys);
        }

        public void RemoveUnavailableMap(MapDef mapDef)
        {
            foreach (var entry in _mapDefs.Where(p => p.Value == mapDef).ToList())
            {
                _mapDefs.Remove(entry.Key);
            }
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        public bool HasMapForLocation(string location)
        {
            return !string.IsNullOrWhiteSpace(location) && _mapDefs.Values.Any(x => x.MapInternalNames.Contains(location, StringComparer.OrdinalIgnoreCase));
        }

        public void LoadPreferredMap(string location)
        {
            var choices = GetMapChoices(location);
            if (choices.TryGetValue(Settings.GetPreferredMap(location), out var preferred))
            {
                OnMapSelected?.Invoke(preferred);
                if (_mapDefs.ContainsValue(preferred))
                    return;
            }

            foreach (var fallback in choices.Values)
            {
                if (!_mapDefs.ContainsValue(fallback))
                    continue;
                OnMapSelected?.Invoke(fallback);
                if (_mapDefs.ContainsValue(fallback))
                    return;
            }
        }

        private IEnumerable<MapDef> FilteredMapDefs()
        {
            return _mapDefs.Values.Where(m =>
                {
                    if (string.IsNullOrEmpty(_nameFilter))
                    {
                        return true;
                    }

                    return m.MapInternalNames.Contains(_nameFilter, StringComparer.OrdinalIgnoreCase);
                }).OrderBy(m => m.DisplayName);
        }

        public IEnumerable<MapDef> GetMapDefs()
        {
            return _mapDefs.Values.ToArray();
        }

        public void LoadFirstAvailableMap()
        {
            if (_selectableMapDefs.Count  == 0)
            {
                return;
            }

            OnSelectDropdownMap(0);
        }

        public void FilterByInternalMapName(string internalMapName)
        {
            _nameFilter = internalMapName;
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        public void ClearFilter()
        {
            _nameFilter = null;
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        public void OnLoadMap(MapDef mapDef)
        {
            _dropdown.UpdateValue(_selectableMapDefs.IndexOf(mapDef));
        }

        public bool IsDropdownOpen()
        {
            return _dropdown.CurrentState;
        }

        public void TryCloseDropdown()
        {
            _dropdown.Close();
        }
    }
}
