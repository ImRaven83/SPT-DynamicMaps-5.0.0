using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using DynamicMaps.Config;
using DynamicMaps.Utils;
using SPTushonka.Reflection.Patching;
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

        // Show is declared on the generic BattleUIScreen<,> base, so the battle UI is fetched from CommonUI
        // instead of relying on __instance being wrapped as EftBattleUIScreen
        [PatchPostfix]
        public static void PatchPostfix()
        {
            TryAttach();
        }

        /// <summary>
        /// Also called when the raid starts, in case patching the generic base method isn't possible
        /// </summary>
        internal static void TryAttach()
        {
            var battleUI = Singleton<CommonUI>.Instance?.EftBattleUIScreen;
            if (battleUI == null) return;

            IsAttached = GameUtils.ShouldShowMapInRaid();
            if (!IsAttached) return;

            Plugin.Instance.TryAttachToBattleUIScreen(battleUI);
        }
    }
}
