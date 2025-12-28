using BepInEx.Configuration;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using System;
using UnityEngine;
using static DynamicMaps.UI.ModdedMapScreen;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        public RectTransform MapScreenTrueParent { get; set; }

        public RectTransform RectTransform { get; private set; }
        public KeyboardShortcut PeekShortcut { get; set; }
        public KeyboardShortcut HideMinimapShortcut { get; set; }
        public bool HoldForPeek { get; set; }  // opposite is peek toggle
        public bool IsPeeking { get; private set; }
        public Func<bool> IsMiniMapEnabled { get; private set; }
        public bool ShowingMiniMap { get; private set; }
        public bool WasMiniMapActive { get; set; }
        private bool IsMiniMapHidden = false;

        internal static MapPeekComponent Create(GameObject parent, CombinedConfig config)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;

            var component = go.AddComponent<MapPeekComponent>();
            component.IsMiniMapEnabled = () => config.AllowMiniMap;

            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Update()
        {
            if (!GameUtils.ShouldShowMapInRaid())
            {
                if (ShowingMiniMap)
                {
                    EndMiniMap();
                }

                if (IsPeeking)
                {
                    EndPeek();
                }

                return;
            }

            HandleMinimapState();
            HandlePeekState();
        }

        private void HandleMinimapState()
        {
            if (!IsMiniMapEnabled.Invoke())
            {
                if (ShowingMiniMap)
                {
                    EndMiniMap();
                    WasMiniMapActive = false;
                }

                return;
            }

            if (HideMinimapShortcut.BetterIsDown())
            {
                IsMiniMapHidden = !IsMiniMapHidden;

                if (!IsMiniMapHidden && !ShowingMiniMap)
                {
                    BeginMiniMap(false);
                }
                else
                {
                    EndMiniMap();
                }

                return;
            }

            if (IsMiniMapHidden) return;

            if (!IsPeeking && !MapScreen.IsShowingMapScreen)
            {
                BeginMiniMap();
            }
            else
            {
                EndMiniMap();
            }
        }

        private void HandlePeekState()
        {
            if (HoldForPeek && PeekShortcut.BetterIsPressed() != IsPeeking)
            {
                // hold for peek
                if (PeekShortcut.BetterIsPressed())
                {
                    WasMiniMapActive = ShowingMiniMap;
                    EndMiniMap();
                    BeginPeek(WasMiniMapActive && !IsMiniMapHidden);
                }
                else
                {
                    EndPeek();
                }
            }
            else if (!HoldForPeek && PeekShortcut.BetterIsDown())
            {
                // toggle peek
                if (!IsPeeking)
                {
                    WasMiniMapActive = ShowingMiniMap;
                    EndMiniMap();
                    BeginPeek(WasMiniMapActive);
                }
                else
                {
                    EndPeek();
                }
            }
        }

        private void BeginPeek(bool playAnimation = true)
        {
            if (IsPeeking) return;

            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();

            IsPeeking = true;

            // attach map screen to peek mask
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show(playAnimation);
        }

        internal void EndPeek()
        {
            if (!IsPeeking) return;

            IsPeeking = false;

            // un-attach map screen and re-attach to true parent
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);

            if (WasMiniMapActive)
            {
                BeginMiniMap();
            }
        }

        internal void BeginMiniMap(bool playAnimation = true)
        {
            if (ShowingMiniMap) return;

            ShowingMiniMap = true;

            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();

            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show(playAnimation);
        }

        internal void EndMiniMap()
        {
            if (!ShowingMiniMap) return;

            ShowingMiniMap = false;

            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);
        }
    }
}
