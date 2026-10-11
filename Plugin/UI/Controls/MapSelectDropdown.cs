using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicMaps.Data;
using DynamicMaps.Utils;
using EFT.UI;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public class MapSelectDropdown : MonoBehaviour
    {
        public MapSelectDropdown(IntPtr ptr) : base(ptr) { }

        private static readonly HashSet<string> AcceptableExtensions = new HashSet<string>{ "json", "jsonc" };

        public event Action<MapDef> OnMapSelected;
        public RectTransform RectTransform => gameObject.transform.TryCast<RectTransform>();

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
            OnMapSelected?.Invoke(_selectableMapDefs[index]);
        }

        private void ChangeAvailableMapDefs(IEnumerable<MapDef> mapDefs)
        {
            // check if these are the same map defs as before
            if (_selectableMapDefs != null && Enumerable.SequenceEqual(mapDefs, _selectableMapDefs))
            {
                return;
            }

            _selectableMapDefs = mapDefs.ToList();

            var displayNames = new Il2CppSystem.Collections.Generic.List<string>();
            foreach (var def in _selectableMapDefs)
            {
                displayNames.Add(def.DisplayName);
            }

            try
            {
                _dropdown.Show(displayNames.Cast<Il2CppSystem.Collections.Generic.IEnumerable<string>>(), null);
                _dropdown.OnValueChanged.Subscribe((Action<int>)OnSelectDropdownMap);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"Failed to fill map select dropdown: {e}");
            }

            Plugin.Logger.LogInfo($"Map select dropdown has {_selectableMapDefs.Count} maps");

            gameObject.SetActive(_selectableMapDefs.Count > 1);
        }

        public void LoadMapDefsFromPath(string relPath)
        {
            var absolutePath = Path.Combine(Plugin.Path, relPath);
            var paths = Directory.EnumerateFiles(absolutePath, "*.*", SearchOption.AllDirectories)
                .Where(p => AcceptableExtensions.Contains(Path.GetExtension(p).TrimStart('.').ToLowerInvariant()));

            // NOTE: this will not catch deleted maps, which I'm fine with
            foreach (var path in paths)
            {
                var writeTime = File.GetLastWriteTimeUtc(path);
                if (_writeTimes.ContainsKey(path) && _writeTimes[path] == writeTime)
                {
                    // this is the same file as before
                    continue;
                }

                _writeTimes[path] = writeTime;

                Plugin.Logger.LogInfo($"Trying to load MapDef from path: {path}");
                var mapDef = MapDef.LoadFromPath(path);
                if (mapDef != null)
                {
                    Plugin.Logger.LogInfo($"Loaded MapDef with display name: {mapDef.DisplayName}");
                    _mapDefs[path] = mapDef;
                }
            }

            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        private IEnumerable<MapDef> FilteredMapDefs()
        {
            return _mapDefs.Values.Where(m =>
                {
                    if (string.IsNullOrEmpty(_nameFilter))
                    {
                        return true;
                    }

                    return m.MapInternalNames.Contains(_nameFilter);
                }).OrderBy(m => m.DisplayName);
        }

        public IEnumerable<MapDef> GetMapDefs()
        {
            return _mapDefs.Values;
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
            if (FilteredMapDefs().FirstOrDefault() == null)
            {
                _nameFilter = "";
                Plugin.Logger.LogWarning($"Cannot filter by {internalMapName}, no MapDefs match that map");
            }

            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        public void ClearFilter()
        {
            _nameFilter = null;
            ChangeAvailableMapDefs(FilteredMapDefs());
        }

        public void OnLoadMap(MapDef mapDef)
        {
            var index = _selectableMapDefs?.IndexOf(mapDef) ?? -1;
            if (index < 0)
            {
                return;
            }

            // il2cpp doesn't fill in the optional Nullable<int> parameters (they'd be passed as null and crash),
            // so pass everything explicitly; no callback since the map is already being loaded
            _dropdown.UpdateValue(index, false, new Il2CppSystem.Nullable<int>(), new Il2CppSystem.Nullable<int>(), false);
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
