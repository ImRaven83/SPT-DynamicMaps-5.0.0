using System;
using System.Collections.Generic;
using System.Reflection;
using SPTushonka.Reflection.Patching;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class PlayerOnDeadPatch : ModulePatch
    {
        internal static event Action<Player> OnDead;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnDead));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance)
        {
            OnDead?.Invoke(__instance);
        }
    }

    internal class PlayerInventoryThrowItemPatch : ModulePatch
    {
        internal static event Action<int, Item> OnThrowItem;
        internal static Dictionary<int, Item> ThrownItems = [];

        private bool _hasRegisteredEvents;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                GameWorldDestroyLootPatch.OnDestroyLoot += OnDestroyLoot;
                _hasRegisteredEvents = true;
            }

            return AccessTools.Method(typeof(Player.PlayerInventoryController),
                                      nameof(Player.PlayerInventoryController.ThrowItem));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player.PlayerInventoryController __instance, Item item)
        {
            HandleThrownItem(__instance.Player, item);
        }

        internal static void HandleThrownItem(Player player, Item item)
        {
            // only look at main player's dropped items
            if (player == null || item == null || player != GameUtils.GetMainPlayer())
            {
                return;
            }

            var itemNetId = item.Id.GetHashCode();
            if (ThrownItems.TryGetValue(itemNetId, out var existing) && existing == item)
            {
                return;
            }

            ThrownItems[itemNetId] = item;
            OnThrowItem?.Invoke(itemNetId, item);
        }

        internal static void OnDestroyLoot(LootItem lootItem)
        {
            if (lootItem == null || lootItem.Item == null)
            {
                return;
            }

            ThrownItems.Remove(lootItem.GetNetId());
        }

        internal static void OnRaidEnd()
        {
            ThrownItems.Clear();
        }
    }
}
