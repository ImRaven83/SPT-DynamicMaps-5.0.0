using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPTushonka.Reflection.Patching;
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
            var caches = UnityEngine.Object.FindObjectsOfType<LootableContainer>(true)
                .Where( x => 
                    x.name.StartsWith("scontainer_wood_CAP")  || 
                    x.name.StartsWith("scontainer_Blue_Barrel_Base_Cap"))
                .ToList();


            foreach (var cache in caches)
            {
                HiddenStashes.Add(cache);
            }

            try
            {
                BattleUIScreenShowPatch.TryAttach();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"Caught error while attaching to battle UI: {e}");
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
            try
            {
                OnRaidEnd?.Invoke();
                GameStartedPatch.HiddenStashes.Clear();
            }
            catch(Exception e)
            {
                Plugin.Logger.LogError($"Caught error while doing end of raid calculations");
                Plugin.Logger.LogError($"{e.Message}");
                Plugin.Logger.LogError($"{e.StackTrace}");
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

    internal class LootItemInitPatch : ModulePatch
    {
        internal static event Action<LootItem> OnRegisterLoot;

        protected override MethodBase GetTargetMethod()
        {
            // Init is overloaded, the one that takes the item is the one that's called on spawn
            return typeof(LootItem).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .First(m => m.Name == nameof(LootItem.Init)
                         && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Item)
                         && m.GetParameters().All(p => !p.ParameterType.IsArray));
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
        public static void PatchPrefix(IKillable loot)
        {
            try
            {
                var lootItem = loot?.TryCast<LootItem>();
                if (lootItem != null)
                {
                    OnDestroyLoot?.Invoke(lootItem);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"Caught error while running DestroyLoot patch");
                Plugin.Logger.LogError($"{e.Message}");
                Plugin.Logger.LogError($"{e.StackTrace}");
            }
        }
    }
}
