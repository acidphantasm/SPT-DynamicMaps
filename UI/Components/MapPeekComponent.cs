using BepInEx.Configuration;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        public RectTransform MapScreenTrueParent { get; set; }

        public RectTransform RectTransform { get; private set; }
        public KeyboardShortcut PeekShortcut { get; set; }
        public bool HoldForPeek { get; set; }  // opposite is peek toggle
        public bool IsPeeking { get; private set; }
        private bool _isMiniMapEnabled => Settings.MiniMapEnabled.Value;
        public bool ShowingMiniMap { get; private set; }
        public bool WasMiniMapActive { get; set; }
        private bool _firstUpdate = true;
        
        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            go.GetRectTransform().sizeDelta = parent.GetRectTransform().sizeDelta;

            var component = go.AddComponent<MapPeekComponent>();

            return component;
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Update()
        {
            // Show or hide the mini-map
            if (_isMiniMapEnabled)
            {
                if (_firstUpdate)
                {
                    BeginMiniMap();
                    _firstUpdate = false;
                }
                
                if (!ShowingMiniMap && Settings.MiniMapShowOrHide.Value.BetterIsDown())
                {
                    BeginMiniMap();
                }
                else if (ShowingMiniMap && Settings.MiniMapShowOrHide.Value.BetterIsDown())
                {
                    EndMiniMap();
                }
            }
            
            if (HoldForPeek && PeekShortcut.BetterIsPressed() != IsPeeking)
            {
                // hold for peek
                if (PeekShortcut.BetterIsPressed())
                {
                    WasMiniMapActive = ShowingMiniMap;
                    EndMiniMap();
                    BeginPeek();
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
                    BeginPeek();
                }
                else
                {
                    EndPeek();
                }
            }
        }

        internal void BeginPeek()
        {
            if (IsPeeking) return;
            
            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();

            IsPeeking = true;
            
            // attach map screen to peek mask
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show();
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
    
        internal void BeginMiniMap()
        {
            ShowingMiniMap = true;
            
            // just in case something else is attached and tries to be in front
            transform.SetAsLastSibling();
            
            MapScreen.transform.SetParent(RectTransform);
            MapScreen.Show();
        }

        internal void EndMiniMap()
        {
            ShowingMiniMap = false;
            
            MapScreen.Hide();
            MapScreen.transform.SetParent(MapScreenTrueParent);
        }
    }
}
