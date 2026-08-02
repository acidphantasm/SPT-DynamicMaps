using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.Airdrop;
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
            return AccessTools.Method(typeof(ClientAirDrop), nameof(ClientAirDrop.CloseParachute));
        }

        [PatchPostfix]
        public static void PatchPostfix(ClientAirDrop __instance)
        {
            if (__instance != null && !Airdrops.Contains(__instance._syncObject))
            {
                Airdrops.Add(__instance._syncObject);
                OnAirdropLanded?.Invoke(__instance._syncObject);
            }
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}
