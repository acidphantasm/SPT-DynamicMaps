using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using DG.Tweening;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.UI.Controls;
using DynamicMaps.Utils;
using DynamicMaps.ExternalModSupport;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;
using DynamicMaps.ExternalModSupport.SamSWATHeliCrash;
using EFT;

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        #region Variables and Declerations

        private const string _mapRelPath = "Maps";

        private bool _initialized = false;
        
        private static float _positionTweenTime = 0.25f;
        private static float _scrollZoomScaler = 1.75f;
        private static float _zoomScrollTweenTime = 0.25f;

        private static Vector2 _levelSliderPosition = new Vector2(15f, 750f);
        private static Vector2 _mapSelectDropdownPosition = new Vector2(-780f, -50f);
        private static Vector2 _mapSelectDropdownSize = new Vector2(360f, 31f);
        private static Vector2 _maskSizeModifierInRaid = new Vector2(0, -42f);
        private static Vector2 _maskPositionInRaid = new Vector2(0, -20f);
        private static Vector2 _maskSizeModifierOutOfRaid = new Vector2(0, -70f);
        private static Vector2 _maskPositionOutOfRaid = new Vector2(0, -5f);
        private static Vector2 _textAnchor = new Vector2(0f, 1f);
        private static Vector2 _cursorPositionTextOffset = new Vector2(15f, -52f);
        private static Vector2 _playerPositionTextOffset = new Vector2(15f, -68f);
        private static float _positionTextFontSize = 15f;
        
        public RectTransform RectTransform => gameObject.GetRectTransform();

        private RectTransform _parentTransform => gameObject.transform.parent as RectTransform;

        private bool _isShown = false;

        // map and transport mechanism
        private ScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;

        // map controls
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;

        // peek
        private MapPeekComponent _peekComponent;
        private bool _isPeeking => _peekComponent != null && _peekComponent.IsPeeking;
        private bool _showingMiniMap => _peekComponent != null && _peekComponent.ShowingMiniMap;
        
        public bool IsShowingMapScreen { get; private set; }
        
        // dynamic map marker providers
        private Dictionary<Type, IDynamicMarkerProvider> _dynamicMarkerProviders = [];

        // config
        private bool _autoCenterOnPlayerMarker = true;
        private bool _autoSelectLevel = true;
        private bool _resetZoomOnCenter = false;
        private bool _rememberMapPosition = true;
        private bool _transitionAnimations = true;
        
        private float _centeringZoomResetPoint = 0f;
        private KeyboardShortcut _centerPlayerShortcut;
        private KeyboardShortcut _dumpShortcut;
        private KeyboardShortcut _moveMapUpShortcut;
        private KeyboardShortcut _moveMapDownShortcut;
        private KeyboardShortcut _moveMapLeftShortcut;
        private KeyboardShortcut _moveMapRightShortcut;
        private float _moveMapSpeed = 0.25f;
        private KeyboardShortcut _moveMapLevelUpShortcut;
        private KeyboardShortcut _moveMapLevelDownShortcut;
        
        private KeyboardShortcut _zoomMainMapInShortcut;
        private KeyboardShortcut _zoomMainMapOutShortcut;
        
        private KeyboardShortcut _zoomMiniMapInShortcut;
        private KeyboardShortcut _zoomMiniMapOutShortcut;
        
        private float _zoomMapHotkeySpeed = 2.5f;
        
        #endregion
        
        internal static ModdedMapScreen Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");
            return go.AddComponent<ModdedMapScreen>();
        }

        #region Unity Methods

        private void Awake()
        {
            // make our game object hierarchy
            var scrollRectGO = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGO = UIUtils.CreateUIGameObject(scrollRectGO, "ScrollMask");

            Settings.MiniMapPosition.SettingChanged += (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapScreenOffsetX.SettingChanged += (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapScreenOffsetY.SettingChanged += (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapSizeX.SettingChanged += (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapSizeY.SettingChanged += (sender, args) => AdjustForMiniMap(false); 
            
            _mapView = MapView.Create(scrollMaskGO, "MapView");

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGO.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            _scrollMask = scrollMaskGO.AddComponent<Mask>();

            // set up scroll rect
            _scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;

            // create map controls

            // level select slider
            var sliderPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/Map Panel/MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, RectTransform);
            _levelSelectSlider.OnLevelSelectedBySlider += _mapView.SelectTopLevel;
            _mapView.OnLevelSelected += (level) => _levelSelectSlider.SelectedLevel = level;

            // map select dropdown, this will call LoadMap on the first option
            var selectPrefab = Singleton<CommonUI>.Instance.transform.Find(
                "Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, RectTransform);
            _mapSelectDropdown.OnMapSelected += ChangeMap;

            // texts
            _cursorPositionText = CursorPositionText.Create(gameObject, _mapView.RectTransform, _positionTextFontSize);
            _cursorPositionText.RectTransform.anchorMin = _textAnchor;
            _cursorPositionText.RectTransform.anchorMax = _textAnchor;

            _playerPositionText = PlayerPositionText.Create(gameObject, _positionTextFontSize);
            _playerPositionText.RectTransform.anchorMin = _textAnchor;
            _playerPositionText.RectTransform.anchorMax = _textAnchor;
            _playerPositionText.gameObject.SetActive(false);

            // read config before setting up marker providers
            ReadConfig();

            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;

            // load initial maps from path
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);
            PrecacheMapLayerImages();
        }

        private void OnDestroy()
        {
            GameWorldOnDestroyPatch.OnRaidEnd -= OnRaidEnd;
            
            Settings.MiniMapPosition.SettingChanged -= (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapScreenOffsetX.SettingChanged -= (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapScreenOffsetY.SettingChanged -= (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapSizeX.SettingChanged -= (sender, args) => AdjustForMiniMap(false); 
            Settings.MiniMapSizeY.SettingChanged -= (sender, args) => AdjustForMiniMap(false); 
        }

        private void Update()
        {
            // because we have a scroll rect, it seems to eat OnScroll via IScrollHandler
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                if (!_mapSelectDropdown.isActiveAndEnabled || !_mapSelectDropdown.IsDropdownOpen())
                {
                    OnScroll(scroll);
                }
            }

            // change level hotkeys
            if (!_showingMiniMap)
            {
                if (_moveMapLevelUpShortcut.BetterIsDown())
                {
                    _levelSelectSlider.ChangeLevelBy(1);
                }

                if (_moveMapLevelDownShortcut.BetterIsDown())
                {
                    _levelSelectSlider.ChangeLevelBy(-1);
                }
            }
            
            // shift hotkeys
            var shiftMapX = 0f;
            var shiftMapY = 0f;

            if (!_showingMiniMap)
            {
                if (_moveMapUpShortcut.BetterIsPressed())
                {
                    shiftMapY += 1f;
                }

                if (_moveMapDownShortcut.BetterIsPressed())
                {
                    shiftMapY -= 1f;
                }

                if (_moveMapLeftShortcut.BetterIsPressed())
                {
                    shiftMapX -= 1f;
                }

                if (_moveMapRightShortcut.BetterIsPressed())
                {
                    shiftMapX += 1f;
                }
            }
            
            if (shiftMapX != 0f || shiftMapY != 0f)
            {
                _mapView.ScaledShiftMap(new Vector2(shiftMapX, shiftMapY), _moveMapSpeed * Time.deltaTime, false);
            }

            if (_showingMiniMap)
            {
                OnZoomMini();

            }
            else
            {
                OnZoomMain();
            }
            
            OnCenter();
            
            if (_dumpShortcut.BetterIsDown())
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }
        }

        // private void OnDisable()
        // {
        //     OnHide();
        // }

        #endregion

        #region Show And Hide Top Level

        internal void OnMapScreenShow()
        {
            if (_peekComponent is not null)
            {
                _peekComponent.WasMiniMapActive = _showingMiniMap;
                
                _peekComponent?.EndPeek();
                _peekComponent?.EndMiniMap();
            }

            IsShowingMapScreen = true;

            if (_rememberMapPosition)
            {
                _mapView.SetMapPos(_mapView.MainMapPos, 0f);
            }
            
            transform.parent.Find("MapBlock").gameObject.SetActive(false);
            transform.parent.Find("EmptyBlock").gameObject.SetActive(false);
            transform.parent.gameObject.SetActive(true);

            Show(false);
        }

        internal void OnMapScreenClose()
        {
            Hide();

            IsShowingMapScreen = false;
            
            if (_peekComponent is not null && _peekComponent.WasMiniMapActive)
            {
                _peekComponent.BeginMiniMap();
            }
        }

        internal void Show(bool playAnimation)
        {
            if (!_initialized)
            {
                //Plugin.Log.LogInfo("Map was not initialized, is resetting size and position");
                AdjustSizeAndPosition();
                _initialized = true;
            }
            
            _isShown = true;
            gameObject.SetActive(GameUtils.ShouldShowMapInRaid());

            // populate map select dropdown
            _mapSelectDropdown.LoadMapDefsFromPath(_mapRelPath);

            if (GameUtils.IsInRaid())
            {
                // Plugin.Log.LogInfo("Showing map in raid");
                OnShowInRaid(playAnimation);
            }
            else
            {
                // Plugin.Log.LogInfo("Showing map out-of-raid");
                OnShowOutOfRaid();
            }
        }

        internal void Hide()
        {
            _mapSelectDropdown?.TryCloseDropdown();

            // close isn't called when hidden
            if (GameUtils.IsInRaid())
            {
                // Plugin.Log.LogInfo("Hiding map in raid");
                OnHideInRaid();
            }
            else
            {
                // Plugin.Log.LogInfo("Hiding map out-of-raid");
                OnHideOutOfRaid();
            }

            _isShown = false;
            gameObject.SetActive(false);
        }

        private void OnRaidEnd()
        {
            if (!BattleUIScreenShowPatch.IsAttached) return;
            
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnRaidEnd(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnRaidEnd");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }

            // reset peek and remove reference, it will be destroyed very shortly with parent object
            _peekComponent?.EndPeek();
            _peekComponent?.EndMiniMap();
            
            Destroy(_peekComponent.gameObject);
            _peekComponent = null;

            // unload map completely when raid ends, since we've removed markers
            _mapView.UnloadMap();
        }
        
        #endregion
        
        #region Size And Positioning

        private void AdjustSizeAndPosition()
        {
            // set width and height based on inventory screen
            var rect = Singleton<CommonUI>.Instance.InventoryScreen.GetRectTransform().rect;
            RectTransform.sizeDelta = new Vector2(rect.width, rect.height);
            RectTransform.anchoredPosition = Vector2.zero;

            _scrollRect.GetRectTransform().sizeDelta = RectTransform.sizeDelta;

            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

            _levelSelectSlider.RectTransform.anchoredPosition = _levelSliderPosition;

            _mapSelectDropdown.RectTransform.sizeDelta = _mapSelectDropdownSize;
            _mapSelectDropdown.RectTransform.anchoredPosition = _mapSelectDropdownPosition;

            _cursorPositionText.RectTransform.anchoredPosition = _cursorPositionTextOffset;
            _playerPositionText.RectTransform.anchoredPosition = _playerPositionTextOffset;
        }

        private void AdjustForOutOfRaid()
        {
            // adjust mask
            _scrollMask.GetRectTransform().anchoredPosition = _maskPositionOutOfRaid;
            _scrollMask.GetRectTransform().sizeDelta = RectTransform.sizeDelta + _maskSizeModifierOutOfRaid;

            // turn on cursor and off player position texts
            _cursorPositionText.gameObject.SetActive(true);
            _levelSelectSlider.gameObject.SetActive(true);
            _playerPositionText.gameObject.SetActive(false);
        }

        private void AdjustForInRaid(bool playAnimation)
        {
            var speed = playAnimation ? 0.35f : 0f;
            
            // adjust mask
            _scrollMask.GetRectTransform().DOSizeDelta(RectTransform.sizeDelta + _maskSizeModifierInRaid, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOAnchorPos(_maskPositionInRaid, _transitionAnimations ? speed : 0f);
            
            // turn both cursor and player position texts on
            _cursorPositionText.gameObject.SetActive(true);
            _playerPositionText.gameObject.SetActive(true);
            _levelSelectSlider.gameObject.SetActive(true);
        }

        private void AdjustForPeek(bool playAnimation)
        {
            var speed = playAnimation ? 0.35f : 0f;
            
            // adjust mask
            _scrollMask.GetRectTransform().DOAnchorPos(Vector2.zero, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOSizeDelta(RectTransform.sizeDelta, _transitionAnimations ? speed : 0f);
            
            // turn both cursor and player position texts off
            _cursorPositionText.gameObject.SetActive(false);
            _playerPositionText.gameObject.SetActive(false);
            _levelSelectSlider.gameObject.SetActive(false);
        }

        private void AdjustForMiniMap(bool playAnimation)
        {
            var speed = playAnimation ? 0.35f : 0f;
            
            var cornerPosition = ConvertEnumToScreenPos(Settings.MiniMapPosition.Value);
            
            var offset = new Vector2(Settings.MiniMapScreenOffsetX.Value, Settings.MiniMapScreenOffsetY.Value);
            offset *= ConvertEnumToScenePivot(Settings.MiniMapPosition.Value);
            
            var size = new Vector2(Settings.MiniMapSizeX.Value, Settings.MiniMapSizeY.Value);
            
            _scrollMask.GetRectTransform().DOSizeDelta(size, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOAnchorPos(offset, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOAnchorMin(cornerPosition, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOAnchorMax(cornerPosition, _transitionAnimations ? speed : 0f);
            _scrollMask.GetRectTransform().DOPivot(cornerPosition, _transitionAnimations ? speed : 0f);
            
            _cursorPositionText.gameObject.SetActive(false);
            _playerPositionText.gameObject.SetActive(false);
            _levelSelectSlider.gameObject.SetActive(false);
        }

        private Vector2 ConvertEnumToScreenPos(EMiniMapPosition pos)
        {
            // 0,0 Bottom left
            // 0,1 Top left
            // 1,1 Top right
            // 1,0 Bottom right
            
            switch (pos)
            {
                case EMiniMapPosition.TopRight:
                    return new Vector2(1, 1);
                
                case EMiniMapPosition.BottomRight:
                    return new Vector2(1, 0);
                
                case EMiniMapPosition.TopLeft:
                    return new Vector2(0, 1);
                
                case EMiniMapPosition.BottomLeft:
                    return new Vector2(0, 0);
            }

            return Vector2.zero;
        }

        private Vector2 ConvertEnumToScenePivot(EMiniMapPosition pos)
        {
            // Top right = neg neg
            // Bottom right = neg pos
            // Top left = pos neg
            // Bottom left = pos pos
            
            switch (pos)
            {
                case EMiniMapPosition.TopRight:
                    return new Vector2(-1, -1);
                
                case EMiniMapPosition.BottomRight:
                    return new Vector2(-1, 1);
                
                case EMiniMapPosition.TopLeft:
                    return new Vector2(1, -1);
                
                case EMiniMapPosition.BottomLeft:
                    return new Vector2(1, 1);
            }

            return Vector2.zero;
        }
        
        #endregion

        #region Show And Hide Bottom Level

        private void OnShowInRaid(bool playAnimation)
        {
            if (_showingMiniMap)
            {
                AdjustForMiniMap(playAnimation);
            }
            else if (_isPeeking)
            {
                AdjustForPeek(playAnimation);
            }
            else
            {
                AdjustForInRaid(playAnimation);
            }
            
            // filter dropdown to only maps containing the internal map name
            var mapInternalName = GameUtils.GetCurrentMapInternalName();
            _mapSelectDropdown.FilterByInternalMapName(mapInternalName);
            _mapSelectDropdown.LoadFirstAvailableMap();

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }

            // rest of this function needs player
            var player = GameUtils.GetMainPlayer();
            if (player is null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);

            // select layers to show
            if (_autoSelectLevel)
            {
                _mapView.SelectLevelByCoords(mapPosition);
            }

            // Don't set the map position if we're the mini-map, otherwise it can cause artifacting
            if (_rememberMapPosition && !_showingMiniMap && _mapView.MainMapPos != Vector2.zero)
            {
                _mapView.SetMapPos(_mapView.MainMapPos, _transitionAnimations ? 0.35f : 0f);
                return;
            }
            
            // Auto centering while the minimap is active here can cause artifacting
            if (_autoCenterOnPlayerMarker && !_showingMiniMap)
            {
                // change zoom to desired level
                if (_resetZoomOnCenter)
                {
                    _mapView.SetMapZoom(GetInRaidStartingZoom(), 0);
                }

                // shift map to player position, Vector3 to Vector2 discards z
                _mapView.ShiftMapToPlayer(mapPosition, 0, false);
            }
        }

        private void OnHideInRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideInRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideInRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnShowOutOfRaid()
        {
            AdjustForOutOfRaid();

            // clear filter on dropdown
            _mapSelectDropdown.ClearFilter();

            // load first available map if no maps loaded
            if (_mapView.CurrentMapDef == null)
            {
                _mapSelectDropdown.LoadFirstAvailableMap();
            }

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnShowOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnShowOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void OnHideOutOfRaid()
        {
            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnHideOutOfRaid(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in OnHideOutOfRaid");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        #endregion
        
        #region Map Manipulation
        
        private void OnScroll(float scrollAmount)
        {
            if (_isPeeking || _showingMiniMap)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (scrollAmount > 0)
                {
                    _levelSelectSlider.ChangeLevelBy(1);
                }
                else
                {
                    _levelSelectSlider.ChangeLevelBy(-1);
                }

                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapView.RectTransform, Input.mousePosition, null, out Vector2 mouseRelative);

            var zoomDelta = scrollAmount * _mapView.ZoomCurrent * _scrollZoomScaler;
            _mapView.IncrementalZoomInto(zoomDelta, mouseRelative, _zoomScrollTweenTime);
        }

        private void OnZoomMain()
        {
            var zoomAmount = 0f;
            
            if (_zoomMainMapOutShortcut.BetterIsPressed())
            {
                zoomAmount -= 1f;
            }

            if (_zoomMainMapInShortcut.BetterIsPressed())
            {
                zoomAmount += 1f;
            }
            
            if (zoomAmount != 0f)
            {
                var currentCenter = _mapView.RectTransform.anchoredPosition / _mapView.ZoomMain;
                zoomAmount = _mapView.ZoomMain * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);
                _mapView.IncrementalZoomInto(zoomAmount, currentCenter, 0f);
                
                return;
            }
            
            _mapView.SetMapZoom(_mapView.ZoomMain, 0f);
        }

        private void OnZoomMini()
        {
            var zoomAmount = 0f;
            
            if (_zoomMiniMapOutShortcut.BetterIsPressed())
            {
                zoomAmount -= 1f;
            }
            
            if (_zoomMiniMapInShortcut.BetterIsPressed())
            {
                zoomAmount += 1f;
            }
            
            if (zoomAmount != 0f)
            {
                var player = GameUtils.GetMainPlayer();
                var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                zoomAmount = _mapView.ZoomMini * zoomAmount * (_zoomMapHotkeySpeed * Time.deltaTime);
                    
                _mapView.IncrementalZoomIntoMiniMap(zoomAmount, mapPosition, 0.0f);
                
                return;
            }
            
            _mapView.SetMapZoom(_mapView.ZoomMini, 0f, false, true);
        }

        private void OnCenter()
        {
            if (_centerPlayerShortcut.BetterIsDown() || _showingMiniMap)
            {
                var player = GameUtils.GetMainPlayer();
                
                if (player is not null)
                {
                    var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
                    
                    _mapView.ShiftMapToCoordinate(
                        mapPosition, 
                        _showingMiniMap ? 0f : _positionTweenTime, 
                        _showingMiniMap);
                    
                    _mapView.SelectLevelByCoords(mapPosition);
                }
            }
        }

        #endregion

        #region Config and Marker Providers

        internal void ReadConfig()
        {
            _centerPlayerShortcut = Settings.CenterOnPlayerHotkey.Value;
            _dumpShortcut = Settings.DumpInfoHotkey.Value;

            _moveMapUpShortcut = Settings.MoveMapUpHotkey.Value;
            _moveMapDownShortcut = Settings.MoveMapDownHotkey.Value;
            _moveMapLeftShortcut = Settings.MoveMapLeftHotkey.Value;
            _moveMapRightShortcut = Settings.MoveMapRightHotkey.Value;
            _moveMapSpeed = Settings.MapMoveHotkeySpeed.Value;

            _moveMapLevelUpShortcut = Settings.ChangeMapLevelUpHotkey.Value;
            _moveMapLevelDownShortcut = Settings.ChangeMapLevelDownHotkey.Value;

            _zoomMainMapInShortcut = Settings.ZoomMapInHotkey.Value;
            _zoomMainMapOutShortcut = Settings.ZoomMapOutHotkey.Value;
            
            _zoomMiniMapInShortcut = Settings.ZoomInMiniMapHotkey.Value;
            _zoomMiniMapOutShortcut = Settings.ZoomOutMiniMapHotkey.Value;
            
            _zoomMapHotkeySpeed = Settings.ZoomMapHotkeySpeed.Value;

            _autoCenterOnPlayerMarker = Settings.AutoCenterOnPlayerMarker.Value;
            _resetZoomOnCenter = Settings.ResetZoomOnCenter.Value;
            _rememberMapPosition = Settings.RetainMapPosition.Value;
            
            _autoSelectLevel = Settings.AutoSelectLevel.Value;
            _centeringZoomResetPoint = Settings.CenteringZoomResetPoint.Value;

            _transitionAnimations = Settings.MapTransitionEnabled.Value;
            
            if (_mapView is not null)
            {
                _mapView.ZoomMain = Settings.ZoomMainMap.Value;
                _mapView.ZoomMini = Settings.ZoomMiniMap.Value;
            }
            
            if (_peekComponent is not null)
            {
                _peekComponent.PeekShortcut = Settings.PeekShortcut.Value;
                _peekComponent.HoldForPeek = Settings.HoldForPeek.Value;
                _peekComponent.HideMinimapShortcut = Settings.MiniMapShowOrHide.Value;
            }

            AddRemoveMarkerProvider<PlayerMarkerProvider>(Settings.ShowPlayerMarker.Value);
            AddRemoveMarkerProvider<QuestMarkerProvider>(Settings.ShowQuestsInRaid.Value);
            AddRemoveMarkerProvider<LockedDoorMarkerMutator>(Settings.ShowLockedDoorStatus.Value);
            AddRemoveMarkerProvider<BackpackMarkerProvider>(Settings.ShowDroppedBackpackInRaid.Value);
            AddRemoveMarkerProvider<BTRMarkerProvider>(Settings.ShowBTRInRaid.Value);
            AddRemoveMarkerProvider<AirdropMarkerProvider>(Settings.ShowAirdropsInRaid.Value);
            AddRemoveMarkerProvider<LootMarkerProvider>(Settings.ShowWishListItemsInRaid.Value);
            AddRemoveMarkerProvider<HiddenStashMarkerProvider>(Settings.ShowHiddenStashesInRaid.Value);
            AddRemoveMarkerProvider<TransitMarkerProvider>(Settings.ShowTransitPointsInRaid.Value);
            AddRemoveMarkerProvider<SecretMarkerProvider>(Settings.ShowSecretPointsInRaid.Value);
            
            if (Settings.ShowAirdropsInRaid.Value)
            {
                GetMarkerProvider<AirdropMarkerProvider>()
                    .RefreshMarkers();
            }

            if (Settings.ShowWishListItemsInRaid.Value)
            {
                GetMarkerProvider<LootMarkerProvider>()
                    .RefreshMarkers();
            }

            if (Settings.ShowHiddenStashesInRaid.Value)
            {
                GetMarkerProvider<HiddenStashMarkerProvider>()
                    .RefreshMarkers();
            }

            // Transits
            if (Settings.ShowTransitPointsInRaid.Value)
            {
                GetMarkerProvider<TransitMarkerProvider>()
                    .RefreshMarkers(_mapView);
            }

            // Secret Exfils
            AddRemoveMarkerProvider<SecretMarkerProvider>(Settings.ShowSecretPointsInRaid.Value);
            if (Settings.ShowSecretPointsInRaid.Value)
            {
                var provider = GetMarkerProvider<SecretMarkerProvider>();
                provider.ShowExtractStatusInRaid = Settings.ShowExtractStatusInRaid.Value;
            }

            // Exfils
            AddRemoveMarkerProvider<ExtractMarkerProvider>(Settings.ShowExtractsInRaid.Value);
            if (Settings.ShowExtractsInRaid.Value)
            {
                var provider = GetMarkerProvider<ExtractMarkerProvider>();
                provider.ShowExtractStatusInRaid = Settings.ShowExtractStatusInRaid.Value;
            }

            // other player markers
            var needOtherPlayerMarkers = Settings.ShowFriendlyPlayerMarkersInRaid.Value
                                      || Settings.ShowEnemyPlayerMarkersInRaid.Value
                                      || Settings.ShowBossMarkersInRaid.Value
                                      || Settings.ShowScavMarkersInRaid.Value;

            AddRemoveMarkerProvider<OtherPlayersMarkerProvider>(needOtherPlayerMarkers);
            
            if (needOtherPlayerMarkers)
            {
                var provider = GetMarkerProvider<OtherPlayersMarkerProvider>();
                provider.ShowFriendlyPlayers = Settings.ShowFriendlyPlayerMarkersInRaid.Value;
                provider.ShowEnemyPlayers = Settings.ShowEnemyPlayerMarkersInRaid.Value;
                provider.ShowScavs = Settings.ShowScavMarkersInRaid.Value;
                provider.ShowBosses = Settings.ShowBossMarkersInRaid.Value;
                
                provider.RefreshMarkers();
            }

            // corpse markers
            var needCorpseMarkers = Settings.ShowFriendlyCorpsesInRaid.Value
                                 || Settings.ShowKilledCorpsesInRaid.Value
                                 || Settings.ShowFriendlyKilledCorpsesInRaid.Value
                                 || Settings.ShowBossCorpsesInRaid.Value
                                 || Settings.ShowOtherCorpsesInRaid.Value;

            AddRemoveMarkerProvider<CorpseMarkerProvider>(needCorpseMarkers);
            if (needCorpseMarkers)
            {
                var provider = GetMarkerProvider<CorpseMarkerProvider>();
                provider.ShowFriendlyCorpses = Settings.ShowFriendlyCorpsesInRaid.Value;
                provider.ShowKilledCorpses = Settings.ShowKilledCorpsesInRaid.Value;
                provider.ShowFriendlyKilledCorpses = Settings.ShowFriendlyKilledCorpsesInRaid.Value;
                provider.ShowBossCorpses = Settings.ShowBossCorpsesInRaid.Value;
                provider.ShowOtherCorpses = Settings.ShowOtherCorpsesInRaid.Value;
                
                provider.RefreshMarkers();
            }
            
            if (ModDetection.HeliCrashLoaded)
            {
                AddRemoveMarkerProvider<HeliCrashMarkerProvider>(Settings.ShowHeliCrashMarker.Value);
            }
        }

        internal void TryAddPeekComponent(EftBattleUIScreen battleUI)
        {
            // Peek component already instantiated, return
            if (_peekComponent is not null)
            {
                return;
            }

            Plugin.Log.LogInfo("Trying to attach peek component to BattleUI");

            _peekComponent = MapPeekComponent.Create(battleUI.gameObject);
            _peekComponent.MapScreen = this;
            _peekComponent.MapScreenTrueParent = _parentTransform;

            ReadConfig();
        }
        
        public void AddRemoveMarkerProvider<T>(bool status) where T : IDynamicMarkerProvider, new()
        {
            if (status && !_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)] = new T();

                // if the map is shown, need to call OnShowXXXX
                if (_isShown && GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowInRaid(_mapView);
                }
                else if (_isShown && !GameUtils.IsInRaid())
                {
                    _dynamicMarkerProviders[typeof(T)].OnShowOutOfRaid(_mapView);
                }
            }
            else if (!status && _dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                _dynamicMarkerProviders[typeof(T)].OnDisable(_mapView);
                _dynamicMarkerProviders.Remove(typeof(T));
            }
        }

        private T GetMarkerProvider<T>() where T : IDynamicMarkerProvider
        {
            if (!_dynamicMarkerProviders.ContainsKey(typeof(T)))
            {
                return default;
            }

            return (T)_dynamicMarkerProviders[typeof(T)];
        }

        #endregion

        #region Utils And Caching

        private float GetInRaidStartingZoom()
        {
            var startingZoom = _mapView.ZoomMin;
            startingZoom += _centeringZoomResetPoint * (_mapView.ZoomMax - _mapView.ZoomMin);

            return startingZoom;
        }

        private void ChangeMap(MapDef mapDef)
        {
            if (mapDef == null || _mapView.CurrentMapDef == mapDef)
            {
                return;
            }

            Plugin.Log.LogInfo($"MapScreen: Loading map {mapDef.DisplayName}");

            // Reset size and position when loading map and in raid
            if (GameUtils.IsInRaid())
            {
                // Plugin.Log.LogInfo($"MapScreen: Resetting Map Size");
                AdjustSizeAndPosition();
            }

            _mapView.LoadMap(mapDef);

            _mapSelectDropdown.OnLoadMap(mapDef);
            _levelSelectSlider.OnLoadMap(mapDef, _mapView.SelectedLevel);

            foreach (var dynamicProvider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    dynamicProvider.OnMapChanged(_mapView, mapDef);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Dynamic marker provider {dynamicProvider} threw exception in ChangeMap");
                    Plugin.Log.LogError($"  Exception given was: {e.Message}");
                    Plugin.Log.LogError($"  {e.StackTrace}");
                }
            }
        }

        private void PrecacheMapLayerImages()
        {
            Singleton<CommonUI>.Instance.StartCoroutine(
                PrecacheCoroutine(_mapSelectDropdown.GetMapDefs()));
        }

        private static IEnumerator PrecacheCoroutine(IEnumerable<MapDef> mapDefs)
        {
            foreach (var mapDef in mapDefs)
            {
                foreach (var layerDef in mapDef.Layers.Values)
                {
                    // just load sprite to cache it, one a frame
                    Plugin.Log.LogInfo($"Precaching sprite: {layerDef.ImagePath}");
                    TextureUtils.GetOrLoadCachedSprite(layerDef.ImagePath);
                    yield return null;
                }
            }
        }

        #endregion
    }
}
