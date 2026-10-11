using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using DynamicMaps.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using System;
using System.Reflection;

namespace DynamicMaps
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.dynamicmaps", "DynamicMaps", BuildInfo.Version)]
    [BepInDependency("com.SamSWAT.HeliCrash.ArysReloaded", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BasePlugin
    {
        public const int TarkovVersion = 47965;
        public static Plugin Instance;
        public static ManualLogSource Logger => Instance.Log;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ModdedMapScreen Map;

        public override void Load()
        {
            Instance = this;

            if (!VersionChecker.CheckEftVersion(Log, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            // every MonoBehaviour in this assembly has to exist in il2cpp before it can be added to a GameObject
            Il2CppTypes.Register(Log);

            ExternalModSupport.ModDetection.CheckforMods();

            Settings.Init(Config);
            ServerConfigLoader.LoadInBackground();
            Config.SettingChanged += (x, y) => Map?.ReadConfig();

            // patches, a failing patch is logged and skipped so the rest of the mod keeps working
            TryEnable(new BattleUIScreenShowPatch()); // also attaches on raid start, in case this can't be patched
            TryEnable(new CommonUIAwakePatch());
            TryEnable(new MapScreenShowPatch());
            TryEnable(new MapScreenClosePatch());
            TryEnable(new GameStartedPatch());
            TryEnable(new GameWorldOnDestroyPatch());
            TryEnable(new GameWorldUnregisterPlayerPatch());
            TryEnable(new LootItemInitPatch());
            TryEnable(new GameWorldDestroyLootPatch());
            TryEnable(new AirdropBoxOnBoxLandPatch());
            TryEnable(new PlayerOnDeadPatch());
            TryEnable(new PlayerInventoryThrowItemPatch());
            TryEnable(new ShowViewButtonPatch());
        }

        private void TryEnable(SPTushonka.Reflection.Patching.ModulePatch patch)
        {
            try
            {
                patch.Enable();
            }
            catch (Exception e)
            {
                Log.LogError($"Could not enable patch {patch.GetType().Name}: {e.InnerException?.Message ?? e.Message}");
            }
        }

        /// <summary>
        /// Attach to the map screen
        /// </summary>
        internal void TryAttachToMapScreen(MapScreen mapScreen)
        {
            if (Map is not null) return;

            Log.LogInfo("Trying to attach to MapScreen");

            // attach to common UI first to call awake and set things up, then attach to sleeping map screen
            Map = ModdedMapScreen.Create(Singleton<CommonUI>.Instance.gameObject);
            Map.transform.SetParent(mapScreen.transform);
        }

        /// <summary>
        /// Attach the peek component
        /// </summary>
        internal void TryAttachToBattleUIScreen(EftBattleUIScreen battleUI)
        {
            if (Map is null || GameUtils.GetMainPlayer()?.TryCast<HideoutPlayer>() != null)
            {
                return;
            }

            Map.TryAddPeekComponent(battleUI);
        }
    }
}
