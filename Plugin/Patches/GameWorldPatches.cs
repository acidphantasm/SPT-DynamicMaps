using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        public static List<LootableContainer> HiddenStashes { get; } = [];
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameWorld __instance)
        {
            // Credit to RaiRai for finding the hidden stash names
            var caches = LocationScene.GetAllObjects<LootableContainer>()
                .Where( x => 
                    x.name.StartsWith("scontainer_wood_CAP")  || 
                    x.name.StartsWith("scontainer_Blue_Barrel_Base_Cap"))
                .ToList();


            foreach (var cache in caches)
            {
                HiddenStashes.Add(cache);
            }
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
            // This is dumb but I'm not sure a better way to ensure a single failed invoke doesn't cause the whole stack to fail
            // not a lot of stack overflow questions on doing this.. boo
            var handlers = OnRaidEnd?.GetInvocationList();
            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        ((Action)handler)();
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"End of raid handler {handler.Method.DeclaringType?.FullName}.{handler.Method.Name} failed: {e}");
                    }
                }
            }
            GameStartedPatch.HiddenStashes.Clear();
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

    internal class LootItemInitPatch : ModulePatch
    {
        internal static event Action<LootItem> OnRegisterLoot;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LootItem), nameof(LootItem.Init));
            //return typeof(GameWorld).GetMethod("RegisterLoot").MakeGenericMethod(typeof(LootItem));
        }

        [PatchPostfix]
        public static void PatchPostfix(LootItem __instance, Item item)
        {
            if (item == null) return;
            OnRegisterLoot?.Invoke(__instance); 
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
