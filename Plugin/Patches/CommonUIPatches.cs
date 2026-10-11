using System.Reflection;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using SPTushonka.Reflection.Patching;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class CommonUIAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CommonUI), nameof(CommonUI.Awake));
        }

        [PatchPostfix]
        public static void PatchPostfix(CommonUI __instance)
        {
            // il2cpp interop exposes private fields as properties
            var mapScreen = __instance.InventoryScreen?._mapScreen;
            if (mapScreen == null)
            {
                Plugin.Logger.LogError("Could not find MapScreen on CommonUI.InventoryScreen");
                return;
            }

            Plugin.Instance.TryAttachToMapScreen(mapScreen);
        }
    }
}
