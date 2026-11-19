using BepInEx.Configuration;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        internal RectTransform RectTransform { get; private set; }
        
        public KeyboardShortcut PeekShortcut { get; set; }
        public KeyboardShortcut HideMinimapShortcut { get; set; }
        public bool HoldForPeek { get; set; }
        private bool IsPeeking
        {
            get => MapScreen != null && MapScreen.DisplayMode == MapDisplayMode.Peek;
        }
        public bool WasMiniMapActive { get; set; }
        private bool _isMiniMapHidden;
        internal bool ShouldShowMiniMap
        {
            get => Settings.MiniMapEnabled.Value && !_isMiniMapHidden;
        }

        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;
            
            return go.AddComponent<MapPeekComponent>();
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Update()
        {
            if (MapScreen == null || MapScreen.IsShowingMapScreen)
            {
                return;
            }
            
            if (!MapScreen.EnsureInRaidMapAvailable() || !GameUtils.ShouldShowMapInRaid())
            {
                WasMiniMapActive = false;
                MapScreen.RequestMode(MapDisplayMode.Hidden, false);
                return;
            }
            if (HideMinimapShortcut.BetterIsDown())
            {
                _isMiniMapHidden = !_isMiniMapHidden;
            }

            if (HoldForPeek)
            {
                if (PeekShortcut.BetterIsPressed())
                {
                    BeginPeek();
                }
                else if (IsPeeking)
                {
                    EndPeek();
                }
            }
            else if (PeekShortcut.BetterIsDown())
            {
                if (IsPeeking)
                {
                    EndPeek();
                }
                else
                {
                    BeginPeek();
                }
            }
            if (!IsPeeking)
            {
                MapScreen.RequestMode(ShouldShowMiniMap ? MapDisplayMode.Mini : MapDisplayMode.Hidden, true);
            }
        }

        private void BeginPeek()
        {
            if (IsPeeking)
            {
                return;
            }
            WasMiniMapActive = ShouldShowMiniMap;
            MapScreen.RequestMode(MapDisplayMode.Peek, true);
        }

        private void EndPeek()
        {
            if (IsPeeking)
            {
                MapScreen.RequestMode(ShouldShowMiniMap ? MapDisplayMode.Mini : MapDisplayMode.Hidden, true);
            }
        }
    }
}
