using BepInEx.Configuration;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using EFT.InputSystem;
using EFT.GlobalEvents;
using EFT.Settings;
using EFT.Settings.Control;
using EFT.UI;
using UnityEngine;

namespace DynamicMaps.UI.Components
{
    internal class MapPeekComponent : MonoBehaviour
    {
        public ModdedMapScreen MapScreen { get; set; }
        internal RectTransform RectTransform { get; private set; }
        
        public KeyboardShortcut PeekShortcut { get; set; }
        public KeyboardShortcut HideMinimapShortcut { get; set; }
        public KeyboardShortcut PositionHotkey { get; set; }
        
        public bool HoldForPeek { get; set; }
        
        private bool IsPeeking
        {
            get => MapScreen != null && MapScreen.DisplayMode == MapDisplayMode.Peek;
        }
        
        public bool WasMiniMapActive { get; set; }
        private bool _isMiniMapHidden;
        
        private bool _editingPosition;
        private bool _draggingPosition;
        private bool _cursorWasVisible;
        private Vector2 _dragOffset;
        private InputTree _inputTree;
        private MiniMapPositionInputNode _positionInputNode;
        private KeyGroup _aimBinding;
        
        internal bool ShouldShowMiniMap
        {
            get => Settings.MiniMapEnabled.Value && !_isMiniMapHidden;
        }

        internal static MapPeekComponent Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "MapPeek");
            var rectTransform = go.GetRectTransform();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            return go.AddComponent<MapPeekComponent>();
        }

        private void Awake()
        {
            RectTransform = gameObject.GetRectTransform();
        }

        private void Update()
        {
            var inventoryScreen = Singleton<CommonUI>.Instance.InventoryScreen;
            var inventoryOpen = inventoryScreen != null && inventoryScreen.gameObject.activeInHierarchy;
            
            if (_editingPosition && (MapScreen == null || MapScreen.IsShowingMapScreen || inventoryOpen || !GameUtils.HasActiveRaidDetails() || !ShouldShowMiniMap))
            {
                CancelMiniMapPositionEdit();
            }

            if (MapScreen == null || MapScreen.IsShowingMapScreen)
            {
                return;
            }

            if (inventoryOpen)
            {
                MapScreen.RequestMode(MapDisplayMode.Hidden, false);
                return;
            }

            if (!GameUtils.HasActiveRaidDetails())
            {
                MapScreen.RequestMode(MapDisplayMode.Hidden, false);
                return;
            }
            
            if (!MapScreen.EnsureInRaidMapAvailable() || !GameUtils.ShouldShowMapInRaid())
            {
                CancelMiniMapPositionEdit();
                WasMiniMapActive = false;
                MapScreen.RequestMode(MapDisplayMode.Hidden, false);
                return;
            }
            
            if (PositionHotkey.BetterIsDown())
            {
                if (_editingPosition)
                {
                    SaveMiniMapPositionEdit();
                }
                else if (ShouldShowMiniMap && MapScreen.BeginMiniMapPositionEdit())
                {
                    _inputTree = FindInputTree();
                    
                    if (_inputTree == null)
                    {
                        Plugin.Log.LogWarning("Cannot edit mini-map position: EFT input tree is unavailable");
                        MapScreen.CancelMiniMapPositionEdit();
                        return;
                    }

                    if (_positionInputNode == null)
                    {
                        _positionInputNode = gameObject.AddComponent<MiniMapPositionInputNode>();
                    }
                    
                    _inputTree.Add(_positionInputNode);
                    UIEventSystem.Instance.SetTemporaryStatus(true);
                    _cursorWasVisible = Cursor.visible;
                    SetCursorVisible(true);
                    _aimBinding = ControlSettingsGroup.GetKeyGroup(Singleton<SettingsManager>.Instance.Control.Settings.UserKeyBindings.Value, EGameKey.Aim);
                    _editingPosition = true;
                }
                
                return;
            }
            
            if (_editingPosition)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || IsAimPressed())
                {
                    SaveMiniMapPositionEdit();
                    return;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform, Input.mousePosition, null, out var point))
                {
                    if (Input.GetMouseButtonDown(0) && MapScreen.ContainsMiniMapPoint(point))
                    {
                        _draggingPosition = true;
                        _dragOffset = MapScreen.MiniMapCenter - point;
                    }
                    
                    if (_draggingPosition && Input.GetMouseButton(0))
                    {
                        MapScreen.SetMiniMapEditPosition(point + _dragOffset);
                    }
                }
                
                if (Input.GetMouseButtonUp(0))
                {
                    _draggingPosition = false;
                }
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

        internal void CancelMiniMapPositionEdit()
        {
            if (!_editingPosition)
            {
                return;
            }

            if (MapScreen != null)
            {
                MapScreen.CancelMiniMapPositionEdit();
            }
            
            EndMiniMapPositionEdit();
        }

        private void SaveMiniMapPositionEdit()
        {
            MapScreen.SaveMiniMapPositionEdit();
            EndMiniMapPositionEdit();
        }

        private bool IsAimPressed()
        {
            if (_aimBinding?.variants == null)
                return false;

            foreach (var source in _aimBinding.variants)
            {
                if (source?.keyCode == null || source.keyCode.Count == 0)
                    continue;

                var allHeld = true;
                var pressed = false;
                
                foreach (var key in source.keyCode)
                {
                    allHeld &= Input.GetKey(key);
                    pressed |= Input.GetKeyDown(key);
                }
                
                if (allHeld && pressed)
                    return true;
            }

            return false;
        }

        private void EndMiniMapPositionEdit()
        {
            _editingPosition = false;
            _draggingPosition = false;
            
            if (_inputTree != null && _positionInputNode != null)
            {
                _inputTree.Remove(_positionInputNode);
            }
            
            _inputTree = null;
            _aimBinding = null;
            
            UIEventSystem.Instance.SetTemporaryStatus(false);
            SetCursorVisible(_cursorWasVisible);
        }

        private static void SetCursorVisible(bool visible)
        {
            GlobalEventsController.Instance.CreateCommonEvent<ToggleShowInGameCursorEvent>().Invoke(visible);
        }

        private static InputTree FindInputTree()
        {
            return GameObject.Find("___Input")?.GetComponent<InputTree>();
        }

        private void OnDestroy()
        {
            CancelMiniMapPositionEdit();
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
