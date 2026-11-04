using System;
using System.Reflection;
using DynamicMaps.Utils;
using SPT.Reflection.Patching;
using EFT;
using EFT.UI;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class BattleUIScreenShowPatch : ModulePatch
    {
        public static bool IsAttached = false;
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EftBattleUIScreen),
                                      nameof(EftBattleUIScreen.Show),
                                      new Type[] { typeof(GamePlayerOwner) });
        }

        [PatchPostfix]
        public static void PatchPostfix(EftBattleUIScreen __instance)
        {
            IsAttached = GameUtils.ShouldShowMapInRaid();
            if (!IsAttached) return;
            
            Plugin.Instance.TryAttachToBattleUIScreen(__instance);
        }
    }
}
