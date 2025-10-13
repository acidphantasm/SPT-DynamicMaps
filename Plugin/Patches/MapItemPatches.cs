using DynamicMaps.Config;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace DynamicMaps.Patches;

public class ShowViewButtonPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ContextInteractionSwitcherClass),
            nameof(ContextInteractionSwitcherClass.IsActive));
    }

    private static readonly HashSet<string> _mapsToIgnore = [
        "6738033eb7305d3bdafe9518",
        "673803448cb3819668d77b1b",
        "6738034a9713b5f42b4a8b78",
        "6738034e9d22459ad7cd1b81",
        "6738035350b24a4ae4a57997",
    ];

    [PatchPostfix]
    public static void PatchPrefix(ContextInteractionSwitcherClass __instance, EItemInfoButton button, ref bool __result)
    {
        if (button is not EItemInfoButton.ViewMap || Settings.ReplaceMapScreen.Value) return;

        var item = __instance.Item_0;

        __result = !_mapsToIgnore.Contains(item.TemplateId);
    }
}