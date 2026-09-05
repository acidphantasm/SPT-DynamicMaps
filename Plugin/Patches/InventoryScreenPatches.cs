using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DynamicMaps.Utils;
using EFT;
using EFT.Achievements;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.Prestige;
using EFT.Quests;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DynamicMaps.Patches
{
    internal class InventoryScreenShowPatch : ModulePatch
    {
        private const string ButtonName = "DynamicMapsMarkCorpseSearchedButton";
        private const float ButtonSize = 28f;
        private const string TooltipText = "Hide marker";
        private const string HiddenTooltipText = "Show marker";
        private static readonly Vector2 SlotOffset = new(-4f, -4f);
        private static readonly Vector2 FallbackOffset = new(-40f, -72f);
        private static readonly FieldInfo ComplexStashEquipmentPanelField = AccessTools.Field(typeof(ComplexStashPanel), "_equipmentPanel");
        private static Sprite _markerSprite;
        private static int _buttonGeneration;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Show), new[]
            {
                typeof(IHealthController),
                typeof(InventoryController),
                typeof(QuestController),
                typeof(AchievementsController),
                typeof(PrestigeController),
                typeof(CompoundItem),
                typeof(EInventoryTab),
                typeof(IEftSession),
                typeof(ItemContext),
                typeof(bool)
            });
        }

        [PatchPostfix]
        public static void PatchPostfix(InventoryScreen __instance, CompoundItem lootItem)
        {
            try
            {
                ShowOrHideMarkSearchedButton(__instance, __instance?._itemsPanel?._complexStashPanel, lootItem, "InventoryScreen.Show");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Caught error while showing corpse marker button");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }

        internal static void ShowOrHideMarkSearchedButton(InventoryScreen screen, ComplexStashPanel complexStash, CompoundItem lootItem, string source)
        {
            _buttonGeneration++;
            var root = GetButtonRoot(screen, complexStash);
            var coroutineHost = GetCoroutineHost(screen, complexStash);
            DestroyExistingButton(root);
            Plugin.Log.LogInfo($"[CorpseMarkerButton] {source} generation={_buttonGeneration}; screenNull={screen is null}; complexStashNull={complexStash is null}; root={GetTransformPath(root?.transform)}; mapReady={Plugin.Instance?.Map is not null}; lootItem={DescribeLootItem(lootItem)}");

            if (coroutineHost is null || root is null || lootItem is null || Plugin.Instance?.Map is null)
            {
                Plugin.Log.LogWarning($"[CorpseMarkerButton] Button not scheduled from {source} because host/root/lootItem/map is missing. screenNull={screen is null}; complexStashNull={complexStash is null}; rootNull={root is null}; hostNull={coroutineHost is null}; lootItemNull={lootItem is null}; mapReady={Plugin.Instance?.Map is not null}");
                return;
            }

            if (!Plugin.Instance.Map.TryGetCorpseLootMarkerState(lootItem, out var isMarkedSearched, out var reason))
            {
                Plugin.Log.LogWarning($"[CorpseMarkerButton] Button not scheduled from {source}; loot item is not matched to a corpse marker. reason={reason}");
                return;
            }

            Plugin.Log.LogInfo($"[CorpseMarkerButton] Button scheduled from {source}; initialMarked={isMarkedSearched}; reason={reason}");
            coroutineHost.StartCoroutine(ShowButtonAfterLayout(screen, complexStash, lootItem, _buttonGeneration, source));
        }

        private static IEnumerator ShowButtonAfterLayout(InventoryScreen screen, ComplexStashPanel complexStash, CompoundItem lootItem, int generation, string source)
        {
            yield return null;
            yield return null;

            var root = GetButtonRoot(screen, complexStash);
            if (generation != _buttonGeneration || root == null || !root.activeInHierarchy)
            {
                Plugin.Log.LogWarning($"[CorpseMarkerButton] Coroutine aborted before creation from {source}. generation={generation}; currentGeneration={_buttonGeneration}; screenNull={screen == null}; complexStashNull={complexStash == null}; rootNull={root == null}; rootActive={root != null && root.activeInHierarchy}; screenActive={screen != null && screen.gameObject.activeInHierarchy}; complexStashActive={complexStash != null && complexStash.gameObject.activeInHierarchy}");
                yield break;
            }

            var reason = "map is null";
            if (Plugin.Instance?.Map is null
                || !Plugin.Instance.Map.TryGetCorpseLootMarkerState(lootItem, out var isMarkedSearched, out reason))
            {
                Plugin.Log.LogWarning($"[CorpseMarkerButton] Coroutine from {source} could not confirm corpse marker state. mapReady={Plugin.Instance?.Map is not null}; reason={reason}");
                yield break;
            }

            DestroyExistingButton(root);

            var buttonGo = UIUtils.CreateUIGameObject(root, ButtonName);
            buttonGo.SetActive(true);
            buttonGo.AddComponent<LayoutElement>().ignoreLayout = true;

            var background = buttonGo.AddComponent<Image>();
            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var overlayCanvas = buttonGo.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 1000;
            buttonGo.AddComponent<GraphicRaycaster>();

            var tooltip = buttonGo.AddComponent<HoverTooltipArea>();
            tooltip.Init(ItemUiContext.Instance.Tooltip, isMarkedSearched ? HiddenTooltipText : TooltipText, true);

            var icon = CreateMarkerIcon(buttonGo);
            var slash = CreateSlash(buttonGo);
            var placement = PositionButton(buttonGo.GetRectTransform(), screen, complexStash);
            ApplyButtonState(button, background, icon, slash, isMarkedSearched);
            var buttonRect = buttonGo.GetRectTransform();
            Plugin.Log.LogInfo($"[CorpseMarkerButton] Button created from {source}. placement={placement}; marked={isMarkedSearched}; parent={GetTransformPath(buttonGo.transform.parent)}; anchorMin={buttonRect.anchorMin}; anchorMax={buttonRect.anchorMax}; pivot={buttonRect.pivot}; position={buttonRect.anchoredPosition}; size={buttonRect.sizeDelta}; activeSelf={buttonGo.activeSelf}; activeInHierarchy={buttonGo.activeInHierarchy}; lootItem={DescribeLootItem(lootItem)}");

button.onClick.AddListener(() =>
            {
                var clickReason = "map is null";
                var markerHidden = false;
                if (Plugin.Instance?.Map?.TryToggleCorpseSearchedByLootItem(lootItem, out markerHidden, out clickReason) == true)
                {
                    Plugin.Log.LogInfo($"[CorpseMarkerButton] Button clicked; corpse marker {(markerHidden ? "hidden" : "shown")}. reason={clickReason}");
                    ApplyButtonState(button, background, icon, slash, markerHidden);
                    tooltip.SetMessageText(markerHidden ? HiddenTooltipText : TooltipText, true);
                    return;
                }

                Plugin.Log.LogWarning($"[CorpseMarkerButton] Button click could not toggle corpse marker. reason={clickReason}");
            });
        }

        private static Image CreateMarkerIcon(GameObject parent)
        {
            var iconGo = UIUtils.CreateUIGameObject(parent, "MarkerIcon");
            var iconRect = iconGo.GetRectTransform();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5f, 4f);
            iconRect.offsetMax = new Vector2(-5f, -4f);

            var icon = iconGo.AddComponent<Image>();
            icon.sprite = GetMarkerSprite();
            icon.color = new Color(0.86f, 0.91f, 0.95f, 0.96f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private static Image CreateSlash(GameObject parent)
        {
            var slashGo = UIUtils.CreateUIGameObject(parent, "Slash");
            var slashRect = slashGo.GetRectTransform();
            slashRect.anchorMin = new Vector2(0.5f, 0.5f);
            slashRect.anchorMax = new Vector2(0.5f, 0.5f);
            slashRect.pivot = new Vector2(0.5f, 0.5f);
            slashRect.sizeDelta = new Vector2(27f, 3f);
            slashRect.anchoredPosition = Vector2.zero;
            slashRect.localEulerAngles = new Vector3(0f, 0f, -45f);

            var slash = slashGo.AddComponent<Image>();
            slash.color = new Color(0.95f, 0.20f, 0.18f, 1f);
            slash.raycastTarget = false;
            return slash;
        }

        private static string PositionButton(RectTransform buttonRect, InventoryScreen screen, ComplexStashPanel complexStash)
        {
            buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            buttonRect.localScale = Vector3.one;

            var slotRect = FindPreferredSlotRect(screen, complexStash, out var slotSource);
            if (slotRect is not null && TryAttachToSlot(buttonRect, slotRect))
            {
                return $"slotOverlay={slotSource}; slotPath={GetTransformPath(slotRect)}";
            }

            var fallbackParent = GetFallbackParent(screen, complexStash);
            buttonRect.SetParent(fallbackParent.transform, false);
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = FallbackOffset;
            buttonRect.SetAsLastSibling();
            return $"fallback; parent={GetTransformPath(fallbackParent.transform)}";
        }

        private static bool TryAttachToSlot(RectTransform buttonRect, RectTransform slotRect)
        {
            if (slotRect is null)
            {
                return false;
            }

            buttonRect.SetParent(slotRect, false);
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
            buttonRect.anchoredPosition = SlotOffset;
            buttonRect.localScale = Vector3.one;
            buttonRect.SetAsLastSibling();
            return true;
        }

        private static RectTransform FindPreferredSlotRect(InventoryScreen screen, ComplexStashPanel complexStash, out string source)
        {
            source = "none";
            var complexEquipment = GetComplexEquipmentPanel(complexStash);
            var screenComplexStash = screen?._itemsPanel?._complexStashPanel;
            var screenComplexEquipment = ReferenceEquals(screenComplexStash, complexStash)
                ? null
                : GetComplexEquipmentPanel(screenComplexStash);

            return TryFindSlotRect(complexEquipment, EquipmentSlot.Dogtag, "complexEquipment.Dogtag", ref source)
                   ?? TryFindSlotRect(complexEquipment, EquipmentSlot.Earpiece, "complexEquipment.Earpiece", ref source)
                   ?? TryFindSlotRectByName(complexStash?.gameObject, "complexStash.nameSearch", ref source, "Dogtag", "Earpiece")
                   ?? TryFindSlotRect(screenComplexEquipment, EquipmentSlot.Dogtag, "screenComplexEquipment.Dogtag", ref source)
                   ?? TryFindSlotRect(screenComplexEquipment, EquipmentSlot.Earpiece, "screenComplexEquipment.Earpiece", ref source)
                   ?? TryFindSlotRectByName(screenComplexStash?.gameObject, "screenComplexStash.nameSearch", ref source, "Dogtag", "Earpiece")
                   ?? TryFindSlotRect(screen?._itemsPanel?._equipmentPanel, EquipmentSlot.Dogtag, "itemsPanel.Equipment.Dogtag", ref source)
                   ?? TryFindSlotRect(screen?._itemsPanel?._equipmentPanel, EquipmentSlot.Earpiece, "itemsPanel.Equipment.Earpiece", ref source)
                   ?? TryFindSlotRectByName(screen?.gameObject, "screen.nameSearch", ref source, "Dogtag", "Earpiece");
        }

        private static EquipmentTab GetComplexEquipmentPanel(ComplexStashPanel complexStash)
        {
            return complexStash is null
                ? null
                : ComplexStashEquipmentPanelField?.GetValue(complexStash) as EquipmentTab;
        }

        private static RectTransform TryFindSlotRect(EquipmentTab equipmentPanel, EquipmentSlot slot, string slotSource, ref string source)
        {
            var rect = TryGetSlotRect(equipmentPanel, slot);
            if (rect is null)
            {
                return null;
            }

            source = slotSource;
            return rect;
        }

        private static RectTransform TryGetSlotRect(EquipmentTab equipmentPanel, EquipmentSlot slot)
        {
            try
            {
                var slotView = equipmentPanel?.GetSlotView(slot);
                if (slotView == null || !slotView.gameObject.activeInHierarchy)
                {
                    return null;
                }

                return slotView.gameObject.GetRectTransform();
            }
            catch
            {
                return null;
            }
        }

        private static RectTransform TryFindSlotRectByName(GameObject root, string slotSource, ref string source, params string[] slotNames)
        {
            if (root is null)
            {
                return null;
            }

            foreach (var slotName in slotNames)
            {
                var rect = root.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(r =>
                    {
                        var name = r.name.ToLowerInvariant();
                        var expected = slotName.ToLowerInvariant();
                        return name.Contains(expected) && name.Contains("slot");
                    });

                if (rect is not null && rect.gameObject.activeInHierarchy)
                {
                    source = $"{slotSource}.{slotName}";
                    return rect;
                }
            }

            return null;
        }

        private static GameObject GetFallbackParent(InventoryScreen screen, ComplexStashPanel complexStash)
        {
            if (complexStash?._lootPanel != null)
            {
                return complexStash._lootPanel.gameObject;
            }

            if (screen?._itemsPanel?._complexStashPanel?._lootPanel != null)
            {
                return screen._itemsPanel._complexStashPanel._lootPanel.gameObject;
            }

            if (complexStash != null)
            {
                return complexStash.gameObject;
            }

            return screen?._itemsPanel != null ? screen._itemsPanel.gameObject : screen.gameObject;
        }

        private static void ApplyButtonState(Button button, Image background, Image icon, Image slash, bool isMarkedSearched)
        {
            button.interactable = true;
            background.color = isMarkedSearched
                ? new Color(0.12f, 0.24f, 0.16f, 0.88f)
                : new Color(0.08f, 0.10f, 0.12f, 0.92f);
            icon.color = isMarkedSearched
                ? new Color(0.56f, 0.70f, 0.60f, 0.9f)
                : new Color(0.86f, 0.91f, 0.95f, 0.96f);
            slash.color = isMarkedSearched
                ? new Color(0.52f, 0.78f, 0.48f, 0.95f)
                : new Color(0.95f, 0.20f, 0.18f, 1f);

            var colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = new Color(0.18f, 0.22f, 0.27f, 0.96f);
            colors.pressedColor = new Color(0.10f, 0.34f, 0.20f, 0.98f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = background.color;
            button.colors = colors;
        }

        private static Sprite GetMarkerSprite()
        {
            if (_markerSprite != null)
            {
                return _markerSprite;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DynamicMapsHideMarkerIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, IsMarkerPixel(x, y) ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            _markerSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _markerSprite;
        }

        private static bool IsMarkerPixel(int x, int y)
        {
            var dx = x + 0.5f - 16f;
            var circleDy = y + 0.5f - 20f;
            var inCircle = dx * dx + circleDy * circleDy <= 64f;
            var inPoint = y >= 4 && y <= 17 && Mathf.Abs(dx) <= (y - 4f) * 0.45f + 1f;
            var inHole = dx * dx + circleDy * circleDy <= 9f;
            return (inCircle || inPoint) && !inHole;
        }

        private static GameObject GetButtonRoot(InventoryScreen screen, ComplexStashPanel complexStash)
        {
            return screen != null ? screen.gameObject : complexStash?.gameObject;
        }

        private static MonoBehaviour GetCoroutineHost(InventoryScreen screen, ComplexStashPanel complexStash)
        {
            return screen != null ? screen : complexStash;
        }

        private static void DestroyExistingButton(GameObject root)
        {
            if (root is null)
            {
                return;
            }

            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)
                         .Where(rect => rect.name == ButtonName))
            {
                rect.gameObject.SetActive(false);
                Object.Destroy(rect.gameObject);
            }
        }

        private static string DescribeLootItem(CompoundItem lootItem)
        {
            return lootItem is null
                ? "null"
                : $"{lootItem.GetType().Name}:id={lootItem.Id ?? "null"}:template={lootItem.TemplateId}:ownerRoot={DescribeRootItemId(lootItem)}";
        }

        private static string DescribeRootItemId(CompoundItem lootItem)
        {
            return lootItem?.Owner?.RootItem is null ? "null" : lootItem.Owner.RootItem.Id.ToString();
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform is null)
            {
                return "null";
            }

            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }

    internal class ComplexStashPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ComplexStashPanel), nameof(ComplexStashPanel.Show));
        }

        [PatchPostfix]
        public static void PatchPostfix(ComplexStashPanel __instance, InventoryEquipment equipment)
        {
            try
            {
                var screen = __instance.GetComponentInParent<InventoryScreen>();
                InventoryScreenShowPatch.ShowOrHideMarkSearchedButton(screen, __instance, equipment, "ComplexStashPanel.Show");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Caught error while handling complex stash corpse marker button");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
    }
}
