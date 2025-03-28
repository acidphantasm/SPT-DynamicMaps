using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class AirdropBoxOnBoxLandPatch : ModulePatch
    {
        internal static event Action<AirdropSynchronizableObject> OnAirdropLanded;
        internal static List<AirdropSynchronizableObject> Airdrops = [];

        private bool _hasRegisteredEvents = false;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                _hasRegisteredEvents = true;
            }
            // thanks to TechHappy for the breadcrumb of what method to patch
            return AccessTools.Method(typeof(AirdropLogicClass), nameof(AirdropLogicClass.method_3));
        }

        [PatchPostfix]
        public static void PatchPostfix(AirdropLogicClass __instance)
        {
            if (__instance != null && !Airdrops.Contains(__instance.airdropSynchronizableObject_0))
            {
                Airdrops.Add(__instance.airdropSynchronizableObject_0);
                OnAirdropLanded?.Invoke(__instance.airdropSynchronizableObject_0);
            }
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}
