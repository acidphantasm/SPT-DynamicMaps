using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.UI.Controls;
using DynamicMaps.Utils;
using DynamicMaps.ExternalModSupport;
using DynamicMaps.ExternalModSupport.SamSWATHeliCrash;
using EFT;
using EFT.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DynamicMaps.UI
{
    public class ModdedMapScreen : MonoBehaviour
    {
        private const string MapRelPath = "Maps";
        private static readonly Vector2 LevelSliderPosition = new(15f, 750f);
        private static readonly Vector2 MapSelectDropdownPosition = new(-780f, -50f);
        private static readonly Vector2 MapSelectDropdownSize = new(540f, 31f);
        private static readonly Vector2 MaskSizeModifierInRaid = new(0, -42f);
        private static readonly Vector2 MaskPositionInRaid = new(0, -20f);
        private static readonly Vector2 MaskSizeModifierOutOfRaid = new(0, -70f);
        private static readonly Vector2 MaskPositionOutOfRaid = new(0, -5f);
        private static readonly Vector2 TextAnchor = new(0f, 1f);
        private static readonly Vector2 CursorPositionTextOffset = new(15f, -52f);
        private static readonly Vector2 PlayerPositionTextOffset = new(15f, -68f);
        private const float PositionTextFontSize = 15f;

        private readonly MainMapController _mainController = new();
        private readonly MiniMapController _miniController = new();
        
        private readonly Vector3[] _maskCorners = new Vector3[4];
        private readonly Dictionary<Type, IDynamicMarkerProvider> _dynamicMarkerProviders = new();
        
        private MapScrollRect _scrollRect;
        private Mask _scrollMask;
        private MapView _mapView;
        private LevelSelectSlider _levelSelectSlider;
        private MapSelectDropdown _mapSelectDropdown;
        private CursorPositionText _cursorPositionText;
        private PlayerPositionText _playerPositionText;
        private MapPeekComponent _peekComponent;
        private RectTransform _mainParent;
        private MapDisplayMode _mode;
        
        private bool _isShown;
        private bool _changingMode;
        private bool _applyingViewportState;
        private bool _mapChanged;
        private bool _providersActive;
        private bool _providersInRaid;
        private bool _transitionAnimations;
        private float _transitionProgress = 1f;
        
        private MapViewportState _transitionFromState;
        private MapLayout _transitionFromLayout;
        private MapLayout _targetLayout;
        
        private RectTransform RectTransform
        {
            get => gameObject.GetRectTransform();
        }
        private RectTransform ParentTransform
        {
            get => transform.parent as RectTransform;
        }
        private bool IsTransitioning
        {
            get => _transitionProgress < 1f;
        }
        internal MapView MapView
        {
            get => _mapView;
        }
        internal MapDisplayMode DisplayMode
        {
            get => _mode;
        }
        public bool IsShowingMapScreen { get; private set; }
        public static DmServerConfig ServerConfig;

        public class DmServerConfig
        {
            public bool AllowShowFriendlyPlayerMarkersInRaid;
            public bool AllowShowEnemyPlayerMarkersInRaid;
            public bool AllowShowBossMarkersInRaid;
            public bool AllowShowScavMarkersInRaid;
            public bool AllowShowNonVanillaBotMarkersInRaid;
        }

        private struct MapLayout
        {
            internal Vector2 Position;
            internal Vector2 Size;
        }

        internal static ModdedMapScreen Create(GameObject parent)
        {
            var go = UIUtils.CreateUIGameObject(parent, "ModdedMapBlock");
            return go.AddComponent<ModdedMapScreen>();
        }

        private void Awake()
        {
            // make our game object hierarchy
            var scrollRectGo = UIUtils.CreateUIGameObject(gameObject, "Scroll");
            var scrollMaskGo = UIUtils.CreateUIGameObject(scrollRectGo, "ScrollMask");

            
            _mapView = MapView.Create(scrollMaskGo, "MapView");

            // set up mask; size will be set later in Raid/NoRaid
            var scrollMaskImage = scrollMaskGo.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.5f);
            _scrollMask = scrollMaskGo.AddComponent<Mask>();

            // set up scroll rect
            _scrollRect = scrollRectGo.AddComponent<MapScrollRect>();
            _scrollRect.scrollSensitivity = 0;  // don't scroll on mouse wheel
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            _scrollRect.inertia = false;
            _scrollRect.viewport = _scrollMask.GetRectTransform();
            _scrollRect.content = _mapView.RectTransform;
            _scrollRect.OnDragCallback = () =>
            {
                if (!_applyingViewportState && !_changingMode && !IsTransitioning && _mode == MapDisplayMode.Main)
                {
                    _mainController.AcceptDrag(_mapView);
                    ApplyDisplayedViewportState(_mainController.State);
                }
            };
            _scrollRect.OnBeginDragCallback = () =>
            {
                if (_mode == MapDisplayMode.Main && !IsTransitioning)
                {
                    _mainController.AcceptDrag(_mapView);
                }
            };
            // create map controls

            // level select slider
            var sliderPrefab = Singleton<CommonUI>.Instance.transform.Find("Common UI/InventoryScreen/Map Panel/MapBlock/ZoomScroll").gameObject;
            _levelSelectSlider = LevelSelectSlider.Create(sliderPrefab, RectTransform);
            _levelSelectSlider.OnLevelSelectedBySlider += level =>
            {
                if (_mode == MapDisplayMode.Main && !IsTransitioning)
                {
                    _mainController.ChangeLevel(level);
                }
            };
            _mapView.OnLevelSelected += (level) => _levelSelectSlider.SelectedLevel = level;
            _levelSelectSlider.gameObject.SetActive(false);

            // map select dropdown, this will call LoadMap on the first option
            var selectPrefab = Singleton<CommonUI>.Instance.transform.Find("Common UI/InventoryScreen/SkillsAndMasteringPanel/BottomPanel/SkillsPanel/Options/Filter").gameObject;
            _mapSelectDropdown = MapSelectDropdown.Create(selectPrefab, RectTransform);
            _mapSelectDropdown.OnMapSelected += ChangeMap;

            // texts
            _cursorPositionText = CursorPositionText.Create(gameObject, _mapView.RectTransform, PositionTextFontSize);
            _cursorPositionText.RectTransform.anchorMin = TextAnchor;
            _cursorPositionText.RectTransform.anchorMax = TextAnchor;

            _playerPositionText = PlayerPositionText.Create(gameObject, PositionTextFontSize);
            _playerPositionText.RectTransform.anchorMin = TextAnchor;
            _playerPositionText.RectTransform.anchorMax = TextAnchor;
            _playerPositionText.gameObject.SetActive(false);

            // read config before setting up marker providers
            ReadConfig();

            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
            
            // load initial maps from path
            _mapSelectDropdown.LoadMapDefsFromPath(MapRelPath);
            PrecacheMapLayerImages();
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
            {
                _scrollRect.OnBeginDragCallback = null;
                _scrollRect.OnDragCallback = null;
            }
            GameWorldOnDestroyPatch.OnRaidEnd -= OnRaidEnd;
        }

        private void Update()
        {
            if (!_isShown)
            {
                return;
            }
            if (GameUtils.IsInRaid() && (!EnsureInRaidMapAvailable() || !GameUtils.ShouldShowMapInRaid()))
            {
                RequestMode(MapDisplayMode.Hidden, false);
                return;
            }
            var layout = GetLayout(_mode);
            if (layout.Size != _targetLayout.Size || layout.Position != _targetLayout.Position)
            {
                RequestMode(_mode, false, true);
            }

            var player = GameUtils.GetMainPlayer();
            UpdateActiveMap(player, !IsTransitioning);
            if (IsTransitioning)
            {
                _transitionProgress = Mathf.Min(1f, _transitionProgress + Time.deltaTime / 0.35f);
                var progress = Mathf.SmoothStep(0f, 1f, _transitionProgress);
                ApplyLayout(new MapLayout
                {
                    Position = Vector2.Lerp(_transitionFromLayout.Position, _targetLayout.Position, progress),
                    Size = Vector2.Lerp(_transitionFromLayout.Size, _targetLayout.Size, progress)
                });
                ApplyDisplayedViewportState(MapViewportState.Lerp(_transitionFromState, DestinationViewportState(), progress));
            }
            else
            {
                ApplyDisplayedViewportState(DestinationViewportState());
            }
            _scrollRect.enabled = _mode == MapDisplayMode.Main && !IsTransitioning;
            if (Settings.DumpInfoHotkey.Value.BetterIsDown())
            {
                DumpUtils.DumpExtracts();
                DumpUtils.DumpSwitches();
                DumpUtils.DumpLocks();
            }
        }

        internal void OnMapScreenShow()
        {
            if (_mainParent == null)
            {
                _mainParent = ParentTransform;
            }
            if (_peekComponent != null)
                _peekComponent.WasMiniMapActive = _mode == MapDisplayMode.Mini
                    || (_mode == MapDisplayMode.Peek && _peekComponent.ShouldShowMiniMap);
            IsShowingMapScreen = true;
            _mainParent.Find("MapBlock").gameObject.SetActive(false);
            _mainParent.Find("EmptyBlock").gameObject.SetActive(false);
            _mainParent.gameObject.SetActive(true);
            if (!_mainController.HasViewportStateFor(_mapView.CurrentMapDef))
            {
                Canvas.ForceUpdateCanvases();
            }
            RequestMode(MapDisplayMode.Main, true);
        }

        internal void OnMapScreenClose()
        {
            IsShowingMapScreen = false;
            var restoreMiniMap = _peekComponent != null && _peekComponent.WasMiniMapActive && _peekComponent.ShouldShowMiniMap;
            RequestMode(restoreMiniMap ? MapDisplayMode.Mini : MapDisplayMode.Hidden, true);
        }

        internal bool EnsureInRaidMapAvailable()
        {
            if (!GameUtils.IsInRaid() || _mapSelectDropdown.HasMapForLocation(GameUtils.GetCurrentMapInternalName()))
            {
                return true;
            }
            HideDisplay();
            if (_mapView.CurrentMapDef != null)
            {
                foreach (var provider in _dynamicMarkerProviders.Values)
                {
                    try
                    {
                        provider.OnRaidEnd(_mapView);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"Failed to clear map markers: {e}");
                    }
                }
                _mapView.UnloadMap();
                InvalidateControllers(null);
            }
            return false;
        }

        internal void RequestMode(MapDisplayMode mode, bool animate, bool resize = false)
        {
            if (_changingMode || (!resize && _mode == mode))
            {
                return;
            }
            
            if (mode == MapDisplayMode.Hidden)
            {
                HideDisplay();
                return;
            }
            
            if (_mainParent == null)
            {
                _mainParent = ParentTransform;
            }
            
            var targetParent = mode == MapDisplayMode.Main ? _mainParent : _peekComponent?.RectTransform;
            if (targetParent == null)
            {
                return;
            }
            
            _changingMode = true;
            try
            {
                var previousMode = _mode;
                var wasVisible = _isShown;
                var fromState = _mapView.CaptureViewportState();
                var oldScale = RectTransform.lossyScale.x;
                
                if (wasVisible)
                {
                    _scrollMask.GetRectTransform().GetWorldCorners(_maskCorners);
                }
                
                if (previousMode != mode)
                {
                    if (previousMode == MapDisplayMode.Mini)
                    {
                        _miniController.Close();
                    }
                    else if (previousMode == MapDisplayMode.Main || previousMode == MapDisplayMode.Peek)
                    {
                        _mainController.Close();
                    }
                }
                if (!_isShown)
                {
                    _mapSelectDropdown.LoadMapDefsFromPath(MapRelPath);
                }
                if (!EnsureInRaidMapAvailable() || !GameUtils.ShouldShowMapInRaid())
                {
                    HideDisplay();
                    return;
                }
                if (GameUtils.IsInRaid())
                {
                    _mapSelectDropdown.FilterByInternalMapName(GameUtils.GetCurrentMapInternalName());
                    _mapSelectDropdown.LoadPreferredMap(GameUtils.GetCurrentMapInternalName());
                }
                else
                {
                    _mapSelectDropdown.ClearFilter();
                    if (_mapView.CurrentMapDef == null)
                    {
                        _mapSelectDropdown.LoadFirstAvailableMap();
                    }
                }
                if (_mapView.CurrentMapDef == null)
                {
                    HideDisplay();
                    return;
                }

                transform.SetParent(targetParent, false);
                transform.SetAsLastSibling();
                RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                RectTransform.anchorMax = RectTransform.anchorMin;
                RectTransform.pivot = RectTransform.anchorMin;
                RectTransform.sizeDelta = Singleton<CommonUI>.Instance.InventoryScreen.GetRectTransform().rect.size;
                RectTransform.anchoredPosition = Vector2.zero;
                RectTransform.localScale = Vector3.one;
                _scrollRect.GetRectTransform().anchorMin = Vector2.zero;
                _scrollRect.GetRectTransform().anchorMax = Vector2.one;
                _scrollRect.GetRectTransform().sizeDelta = Vector2.zero;
                _scrollRect.GetRectTransform().anchoredPosition = Vector2.zero;
                _scrollRect.StopMovement();
                _scrollRect.enabled = false;
                _mode = mode;
                _isShown = true;
                gameObject.SetActive(true);
                PositionControls();
                _targetLayout = GetLayout(mode);
                var definition = _mapView.CurrentMapDef;
                var player = GameUtils.GetMainPlayer();
                if (mode == MapDisplayMode.Mini)
                {
                    _miniController.Open(definition, _targetLayout.Size);
                }
                else
                {
                    if (resize && previousMode == mode && !_mapChanged)
                    {
                        _mainController.Resize(definition, _targetLayout.Size);
                    }
                    else
                    {
                        _mainController.Open(definition, _targetLayout.Size, player, GameUtils.IsInRaid());
                    }
                    
                    if (GameUtils.IsInRaid() && !(resize && previousMode == mode))
                    {
                        _mainController.SelectPlayerLevel(_mapView, player);
                    }
                }
                UpdateActiveMap(player, false);
                SetProviderVisibility(true, GameUtils.IsInRaid());
                
                _cursorPositionText.gameObject.SetActive(mode == MapDisplayMode.Main);
                _playerPositionText.gameObject.SetActive(mode == MapDisplayMode.Main && GameUtils.IsInRaid());
                var hasMultipleLevels = definition.Layers.Values.Select(layer => layer.Level).Distinct().Count() > 1;
                _levelSelectSlider.gameObject.SetActive(mode == MapDisplayMode.Main && hasMultipleLevels);
                _mapSelectDropdown.gameObject.SetActive(
                    mode == MapDisplayMode.Main && !GameUtils.IsInRaid() && _mapSelectDropdown.GetMapDefs().Count() > 1);
                _transitionProgress = 1f;
                
                if (wasVisible && !_mapChanged && animate && _transitionAnimations)
                {
                    var lower = RectTransform.InverseTransformPoint(_maskCorners[0]);
                    var upper = RectTransform.InverseTransformPoint(_maskCorners[2]);
                    
                    _transitionFromLayout = new MapLayout
                    {
                        Position = (lower + upper) * 0.5f,
                        Size = new Vector2(Mathf.Abs(upper.x - lower.x), Mathf.Abs(upper.y - lower.y))
                    };
                    
                    fromState.Zoom *= oldScale / Mathf.Max(RectTransform.lossyScale.x, 0.0001f);
                    _transitionFromState = fromState;
                    _transitionProgress = 0f;
                    
                    ApplyLayout(_transitionFromLayout);
                    ApplyDisplayedViewportState(fromState);
                }
                else
                {
                    ApplyLayout(_targetLayout);
                    ApplyDisplayedViewportState(DestinationViewportState());
                }
                _mapChanged = false;
                _scrollRect.enabled = mode == MapDisplayMode.Main && !IsTransitioning;
            }
            finally
            {
                _changingMode = false;
            }
        }

        private void HideDisplay()
        {
            _mapSelectDropdown?.TryCloseDropdown();
            
            switch (_mode)
            {
                case MapDisplayMode.Main:
                case MapDisplayMode.Peek:
                    _mainController.Close();
                    break;
                case MapDisplayMode.Mini:
                    _miniController.Close();
                    break;
            }

            SetProviderVisibility(false, _providersInRaid);
            
            _transitionProgress = 1f;
            _mode = MapDisplayMode.Hidden;
            _isShown = false;
            _scrollRect?.StopMovement();
            gameObject.SetActive(false);
            
            if (_mainParent != null)
            {
                transform.SetParent(_mainParent, false);
            }
        }

        private void UpdateActiveMap(Player player, bool allowInput)
        {
            switch (_mode)
            {
                case MapDisplayMode.Mini:
                    _miniController.UpdateMap(_mapView, player, Time.deltaTime, allowInput);
                    break;
                case MapDisplayMode.Main:
                case MapDisplayMode.Peek:
                    _mainController.UpdateMap(_mapView, player, Time.deltaTime, _mode == MapDisplayMode.Peek, allowInput, _mode == MapDisplayMode.Main && _mapSelectDropdown.isActiveAndEnabled && _mapSelectDropdown.IsDropdownOpen());
                    break;
            }
        }

        private MapViewportState DestinationViewportState()
        {
            return _mode == MapDisplayMode.Mini ? _miniController.State : _mainController.State;
        }

        private void ApplyDisplayedViewportState(MapViewportState state)
        {
            _applyingViewportState = true;
            try
            {
                _mapView.ApplyViewportState(state);
            }
            finally
            {
                _applyingViewportState = false;
            }
        }

        private void ApplyLayout(MapLayout layout)
        {
            _applyingViewportState = true;
            try
            {
                var mask = _scrollMask.GetRectTransform();
                mask.anchorMin = new Vector2(0.5f, 0.5f);
                mask.anchorMax = mask.anchorMin;
                mask.pivot = mask.anchorMin;
                mask.sizeDelta = layout.Size;
                mask.anchoredPosition = layout.Position;
            }
            finally
            {
                _applyingViewportState = false;
            }
        }

        private MapLayout GetLayout(MapDisplayMode mode)
        {
            var size = Singleton<CommonUI>.Instance.InventoryScreen.GetRectTransform().rect.size;
            if (mode == MapDisplayMode.Main)
            {
                return new MapLayout
                {
                    Size = size + (GameUtils.IsInRaid() ? MaskSizeModifierInRaid : MaskSizeModifierOutOfRaid),
                    Position = GameUtils.IsInRaid() ? MaskPositionInRaid : MaskPositionOutOfRaid
                };
            }
            if (mode == MapDisplayMode.Peek)
            {
                return new MapLayout
                {
                    Size = size,
                    Position = Vector2.zero
                };
            }
            var miniSize = new Vector2(Settings.MiniMapSizeX.Value, Settings.MiniMapSizeY.Value);
            var corner = ConvertEnumToScreenPos(Settings.MiniMapPosition.Value);
            var offset = new Vector2(Settings.MiniMapScreenOffsetX.Value, Settings.MiniMapScreenOffsetY.Value)
                * ConvertEnumToScenePivot(Settings.MiniMapPosition.Value);
            return new MapLayout
            {
                Size = miniSize,
                Position = (corner - new Vector2(0.5f, 0.5f)) * size + offset
                    + (new Vector2(0.5f, 0.5f) - corner) * miniSize
            };
        }

        private void PositionControls()
        {
            _levelSelectSlider.RectTransform.anchoredPosition = LevelSliderPosition;
            _mapSelectDropdown.RectTransform.sizeDelta = MapSelectDropdownSize;
            _mapSelectDropdown.RectTransform.anchoredPosition = MapSelectDropdownPosition;
            _cursorPositionText.RectTransform.anchorMin = TextAnchor;
            _cursorPositionText.RectTransform.anchorMax = TextAnchor;
            _cursorPositionText.RectTransform.anchoredPosition = CursorPositionTextOffset;
            _playerPositionText.RectTransform.anchorMin = TextAnchor;
            _playerPositionText.RectTransform.anchorMax = TextAnchor;
            _playerPositionText.RectTransform.anchoredPosition = PlayerPositionTextOffset;
        }

        private void SetProviderVisibility(bool visible, bool inRaid)
        {
            if (_providersActive && (!visible || inRaid != _providersInRaid))
            {
                foreach (var provider in _dynamicMarkerProviders.Values)
                {
                    try
                    {
                        if (_providersInRaid)
                        {
                            provider.OnHideInRaid(_mapView);
                        }
                        else
                        {
                            provider.OnHideOutOfRaid(_mapView);
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"Failed to hide map markers: {e}");
                    }
                }
                _providersActive = false;
            }
            if (visible && !_providersActive)
            {
                _providersInRaid = inRaid;
                _providersActive = true;
                foreach (var provider in _dynamicMarkerProviders.Values)
                {
                    try
                    {
                        if (inRaid)
                        {
                            provider.OnShowInRaid(_mapView);
                        }
                        else
                        {
                            provider.OnShowOutOfRaid(_mapView);
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"Failed to show map markers: {e}");
                    }
                }
            }
        }

        private void InvalidateControllers(MapDef definition)
        {
            _mainController.Invalidate(definition);
            _miniController.Invalidate(definition);
        }

        private void OnRaidEnd()
        {
            HideDisplay();
            foreach (var provider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    provider.OnRaidEnd(_mapView);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to clear raid markers: {e}");
                }
            }
            if (_peekComponent != null)
            {
                Destroy(_peekComponent.gameObject);
            }
            _peekComponent = null;
            _mapView.UnloadMap();
            InvalidateControllers(null);
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
        
        internal void ReadConfig()
        {
            _mainController.ReadConfig();
            _miniController.ReadConfig();
            _transitionAnimations = Settings.MapTransitionEnabled.Value;
            if (_peekComponent != null)
            {
                _peekComponent.PeekShortcut = Settings.PeekShortcut.Value;
                _peekComponent.HoldForPeek = Settings.HoldForPeek.Value;
                _peekComponent.HideMinimapShortcut = Settings.MiniMapShowOrHide.Value;
            }
            if (_isShown && !_changingMode)
            {
                var layout = GetLayout(_mode);
                if (layout.Size != _targetLayout.Size || layout.Position != _targetLayout.Position)
                {
                    RequestMode(_mode, false, true);
                }
            }

            AddRemoveMarkerProvider<PlayerMarkerProvider>(Settings.ShowPlayerMarker.Value);
            AddRemoveMarkerProvider<QuestMarkerProvider>(Settings.ShowQuestsInRaid.Value);
            AddRemoveMarkerProvider<LockedDoorMarkerMutator>(Settings.ShowLockedDoorStatus.Value);
            AddRemoveMarkerProvider<BackpackMarkerProvider>(Settings.ShowDroppedBackpackInRaid.Value);
            AddRemoveMarkerProvider<BtrMarkerProvider>(Settings.ShowBTRInRaid.Value);
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
                                      || Settings.ShowScavMarkersInRaid.Value
                                      || Settings.ShowNonVanillaBotMarkersInRaid.Value;

            AddRemoveMarkerProvider<OtherPlayersMarkerProvider>(needOtherPlayerMarkers);
            
            if (needOtherPlayerMarkers)
            {
                var provider = GetMarkerProvider<OtherPlayersMarkerProvider>();
                provider.ShowFriendlyPlayers = ServerConfig != null && ServerConfig.AllowShowFriendlyPlayerMarkersInRaid && Settings.ShowFriendlyPlayerMarkersInRaid.Value;
                provider.ShowEnemyPlayers = ServerConfig != null && ServerConfig.AllowShowEnemyPlayerMarkersInRaid && Settings.ShowEnemyPlayerMarkersInRaid.Value;
                provider.ShowScavs = ServerConfig != null && ServerConfig.AllowShowScavMarkersInRaid && Settings.ShowScavMarkersInRaid.Value;
                provider.ShowBosses = ServerConfig != null && ServerConfig.AllowShowBossMarkersInRaid && Settings.ShowBossMarkersInRaid.Value;
                provider.ShowNonVanillaBots = ServerConfig != null && ServerConfig.AllowShowNonVanillaBotMarkersInRaid && Settings.ShowNonVanillaBotMarkersInRaid.Value;
                
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
            if (_peekComponent != null)
            {
                return;
            }
            if (_mainParent == null)
            {
                _mainParent = ParentTransform;
            }
            _peekComponent = MapPeekComponent.Create(battleUI.gameObject);
            _peekComponent.MapScreen = this;
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

        private void ChangeMap(MapDef mapDef)
        {
            if (mapDef == null || _mapView.CurrentMapDef == mapDef)
            {
                return;
            }
            Plugin.Log.LogInfo($"MapScreen: Loading map {mapDef.DisplayName}");
            try
            {
                _levelSelectSlider.OnLoadMap(mapDef, mapDef.DefaultLevel);
                _mapView.LoadMap(mapDef);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to load map {mapDef.DisplayName}: {e}");
                _mapSelectDropdown.RemoveUnavailableMap(mapDef);
                HideDisplay();
                foreach (var provider in _dynamicMarkerProviders.Values)
                {
                    try
                    {
                        provider.OnRaidEnd(_mapView);
                    }
                    catch (Exception cleanupError)
                    {
                        Plugin.Log.LogError($"Failed to clear map markers: {cleanupError}");
                    }
                }
                _mapView.UnloadMap();
                InvalidateControllers(null);
                return;
            }
            InvalidateControllers(mapDef);
            _mapChanged = true;
            _mapSelectDropdown.OnLoadMap(mapDef);
            foreach (var provider in _dynamicMarkerProviders.Values)
            {
                try
                {
                    provider.OnMapChanged(_mapView, mapDef);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to change map markers: {e}");
                }
            }
            if (_isShown && !_changingMode)
            {
                RequestMode(_mode, false, true);
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
                if (mapDef.IsAddon)
                {
                    continue;
                }

                foreach (var layerDef in mapDef.Layers.Values)
                {
                    // just load sprite to cache it, one a frame
                    Plugin.Log.LogInfo($"Precaching sprite: {layerDef.ImagePath}");
                    if (!System.IO.Path.GetExtension(layerDef.ImagePath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    try
                    {
                        SvgUtils.GetOrLoadCachedSprite(layerDef);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"Failed to precache {layerDef.ImagePath}: {e}");
                    }
                    yield return null;
                }
            }
        }

    }
}
