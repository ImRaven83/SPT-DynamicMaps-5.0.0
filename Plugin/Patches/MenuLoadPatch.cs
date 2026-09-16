using DynamicMaps.UI;
using DynamicMaps.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;
using Newtonsoft.Json;
using SPTushonka.Common.Http;
using SPTushonka.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Communications;
using static DynamicMaps.UI.ModdedMapScreen;

namespace DynamicMaps.Patches
{
    internal class MenuLoadPatch : ModulePatch
    {
        private static bool serverConfigLoaded = false;

        // TODO: unverified for SPT 5.0.0 (IL2CPP) - the original target was found via
        // SPT.Reflection.Utils.PatchConstants (a GClass-obfuscation workaround that no longer exists).
        // Retargeted onto TarkovApplication.MainMenu, which is directly nameable now and fires once the
        // main menu is reachable, same ordering intent as the original "run once early" hook.
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TarkovApplication), nameof(TarkovApplication.MainMenu));
        }

        [PatchPrefix]
        public static async void PatchPostfix()
        {
            try
            {
                if (serverConfigLoaded == false)
                {
                    serverConfigLoaded = true;
                    ModdedMapScreen.ServerConfig = await LoadFromServer();

                    Plugin.Log.LogInfo($"Loaded server config");
                }
                else return;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Caught error while trying to load config");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");
            }
        }
        private static async Task<DmServerConfig> LoadFromServer()
        {
            try
            {
                string payload = await RequestHandler.GetJsonAsync("/dynamicmaps/load");
                return JsonConvert.DeserializeObject<DmServerConfig>(payload);

            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to load: " + ex.ToString());
                NotificationManager.DisplayWarningNotification("Failed to load Dynamic Maps server config - check the server");
                return null;
            }
        }
    }
}
