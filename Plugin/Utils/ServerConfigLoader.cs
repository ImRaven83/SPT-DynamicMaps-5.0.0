using System;
using System.Threading.Tasks;
using SPTushonka.Common.Http;
using DynamicMaps.UI;

namespace DynamicMaps.Utils
{
    /// <summary>
    /// Fetches the server side config (which marker types the server allows), this used to be done from a patch on
    /// menu load but the request doesn't depend on any game state so it's started as soon as the plugin loads
    /// </summary>
    internal static class ServerConfigLoader
    {
        private const string Route = "/dynamicmaps/load";

        public static void LoadInBackground()
        {
            Task.Run(async () =>
            {
                try
                {
                    var payload = await RequestHandler.GetJsonAsync(Route);
                    ModdedMapScreen.ServerConfig = JsonUtils.Deserialize<ModdedMapScreen.DmServerConfig>(payload) ?? ModdedMapScreen.ServerConfig;
                    Plugin.Logger.LogInfo("Loaded server config");
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Failed to load Dynamic Maps server config, is the server mod installed? {ex}");
                }
            });
        }
    }
}
