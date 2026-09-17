using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using DynamicMaps.Config;
using DynamicMaps.Patches;
using DynamicMaps.UI;
using DynamicMaps.UI.Components;
using DynamicMaps.UI.Controls;
using DynamicMaps.Utils;
using EFT;
using EFT.UI;
using EFT.UI.Map;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Reflection;

namespace DynamicMaps
{
    // the version number here is generated on build and may have a warning if not yet built
    [BepInPlugin("com.mpstark.dynamicmaps", "DynamicMaps", BuildInfo.Version)]
    [BepInDependency("sptushonka.custom", "5.0.0")]
    [BepInDependency("com.SamSWAT.HeliCrash.ArysReloaded", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BasePlugin
    {
        // TODO: unknown for SPT 5.0.0 (IL2CPP) - the DrakiaXYZ.VersionChecker library vendored below was
        // written for the Mono client and is unverified here. Needs the real EFT build number to be useful again.
        public const int TarkovVersion = 40743;
        public static Plugin Instance;
        public static ManualLogSource Log => Instance.Log;
        public static string Path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public ModdedMapScreen Map;

        public override void Load()
        {
            Instance = this;

            RegisterIl2CppComponents();

            if (!VersionChecker.CheckEftVersion(Log, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            ExternalModSupport.ModDetection.CheckforMods();

            Settings.Init(Config);
            Config.SettingChanged += (x, y) => Map?.ReadConfig();

            // Log.LogWarning("TEST BUILD OF DYNAMIC MAPS, NO SUPPORT OFFERED.");

            // patches
            new BattleUIScreenShowPatch().Enable();
            new CommonUIAwakePatch().Enable();
            new MapScreenShowPatch().Enable();
            new MapScreenClosePatch().Enable();
            new GameStartedPatch().Enable();
            new GameWorldOnDestroyPatch().Enable();
            new GameWorldUnregisterPlayerPatch().Enable();
            new LootItemInitPatch().Enable();
            new GameWorldDestroyLootPatch().Enable();
            new AirdropBoxOnBoxLandPatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new PlayerInventoryThrowItemPatch().Enable();
            new ShowViewButtonPatch().Enable();
            new MenuLoadPatch().Enable();
        }

        // TODO: unverified for SPT 5.0.0 (IL2CPP) - every custom MonoBehaviour-derived type this plugin
        // instantiates via AddComponent<T>() must be registered with Il2Cpp's type system first.
        private static void RegisterIl2CppComponents()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ModdedMapScreen>();
            ClassInjector.RegisterTypeInIl2Cpp<MapLabel>();
            ClassInjector.RegisterTypeInIl2Cpp<MapLayer>();
            ClassInjector.RegisterTypeInIl2Cpp<MapMarker>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerMapMarker>();
            ClassInjector.RegisterTypeInIl2Cpp<TransformMapMarker>();
            ClassInjector.RegisterTypeInIl2Cpp<MapPeekComponent>();
            ClassInjector.RegisterTypeInIl2Cpp<MapView>();
            ClassInjector.RegisterTypeInIl2Cpp<MapScrollRect>();
            ClassInjector.RegisterTypeInIl2Cpp<CursorPositionText>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerPositionText>();
            ClassInjector.RegisterTypeInIl2Cpp<LevelSelectSlider>();
            ClassInjector.RegisterTypeInIl2Cpp<MapSelectDropdown>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerDotSpawner>();
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
            if (Map is null || GameUtils.GetMainPlayer() is HideoutPlayer)
            {
                return;
            }
            
            Map.TryAddPeekComponent(battleUI);
        }
    }
}
