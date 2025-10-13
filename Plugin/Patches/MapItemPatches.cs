using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DynamicMaps.Config;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace DynamicMaps.Patches;

public class ShowViewButtonPatch : ModulePatch
{
    private static FieldInfo _itemFieldInfo;
    
    protected override MethodBase GetTargetMethod()
    {
        var type = PatchConstants.EftTypes
            .SingleCustom(t => t.GetProperty("EItemViewType_0") != null);

        _itemFieldInfo = AccessTools.Field(type, "Item_0");
        
        return type.GetMethod("IsActive");
    }

    private static readonly HashSet<string> MapsToIgnore = [
        "6738033eb7305d3bdafe9518",
        "673803448cb3819668d77b1b",
        "6738034a9713b5f42b4a8b78",
        "6738034e9d22459ad7cd1b81",
        "6738035350b24a4ae4a57997",
    ];
    
    [PatchPostfix]
    public static void PatchPrefix(object __instance, EItemInfoButton button, ref bool __result)
    {
        if (button is not EItemInfoButton.ViewMap || Settings.ReplaceMapScreen.Value) return;
        
        var item = (Item)_itemFieldInfo.GetValue(__instance);

        __result = !MapsToIgnore.Contains(item.TemplateId);
    }
}