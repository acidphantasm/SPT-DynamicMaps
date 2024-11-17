using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class LocationSceneAwakePatch : ModulePatch
    {
        public static List<LootableContainer> HiddenStashes { get; private set; } = [];
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocationScene), nameof(LocationScene.Awake));
        }

        [PatchPostfix]
        public static void PatchPostfix(LocationScene __instance)
        {
            // Credit to RaiRai for finding the hidden stash names
            var caches = __instance.LootableContainers.Where(x => 
                    x.name.StartsWith("scontainer_wood_CAP") || 
                    x.name.StartsWith("scontainer_Blue_Barrel_Base_Cap"))
                .ToList();
            
            // Add range instead of assigning it direct because
            // LocationScene.Awake() has the potential to run multiple times and
            // stashes may be split between scenes
            HiddenStashes.AddRange(caches);
        }
    }
    
    internal class GameWorldOnDestroyPatch : ModulePatch
    {
        internal static event Action OnRaidEnd;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnDestroy));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            try
            {
                OnRaidEnd?.Invoke();
            }
            catch(Exception e)
            {
                Plugin.Log.LogError($"Caught error while doing end of raid calculations");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
    }

    internal class GameWorldUnregisterPlayerPatch : ModulePatch
    {
        internal static event Action<IPlayer> OnUnregisterPlayer;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.UnregisterPlayer));
        }

        [PatchPostfix]
        public static void PatchPostfix(IPlayer iPlayer)
        {
            OnUnregisterPlayer?.Invoke(iPlayer);
        }
    }

    internal class GameWorldRegisterLootItemPatch : ModulePatch
    {
        internal static event Action<LootItem> OnRegisterLoot;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("RegisterLoot").MakeGenericMethod(typeof(LootItem));
        }

        [PatchPostfix]
        public static void PatchPostfix(LootItem loot)
        {
            OnRegisterLoot?.Invoke(loot);
        }
    }

    internal class GameWorldDestroyLootPatch : ModulePatch
    {
        internal static event Action<LootItem> OnDestroyLoot;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.Name == "DestroyLoot" && m.GetParameters().FirstOrDefault(p => p.Name == "loot") != null);
        }

        [PatchPrefix]
        public static void PatchPrefix(Object loot)
        {
            try
            {
                if (loot is LootItem lootItem)
                {
                    OnDestroyLoot?.Invoke(lootItem);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error while running DestroyLoot patch");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
    }
}
