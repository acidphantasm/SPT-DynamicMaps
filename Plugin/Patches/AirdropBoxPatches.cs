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
            return AccessTools.Method(typeof(AirdropLogicClass), nameof(AirdropLogicClass.method_13));
        }

        [PatchPostfix]
        public static void PatchPostfix(AirdropSynchronizableObject container)
        {
            Airdrops.Add(container);
            OnAirdropLanded?.Invoke(container);
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}
