using System;
using System.Reflection;
using BepInEx.Logging;
using DynamicMaps.UI.Components;
using Il2CppInterop.Runtime.Injection;
using SPTushonka.Reflection.Il2Cpp;
using UnityEngine.EventSystems;

namespace DynamicMaps
{
    /// <summary>
    /// Registers every injected MonoBehaviour with il2cpp, which has to happen before any of them are added to a GameObject
    /// </summary>
    internal static class Il2CppTypes
    {
        public static void Register(ManualLogSource logger)
        {
            // types implementing Unity's event interfaces have to declare them when they're injected,
            // these need to be registered before RegisterAll so that subclasses inherit the interfaces
            RegisterWithInterfaces<MapMarker>(typeof(IPointerEnterHandler), typeof(IPointerExitHandler));
            RegisterWithInterfaces<BeginDragNotifier>(typeof(IBeginDragHandler));

            Il2CppInjection.RegisterAll(Assembly.GetExecutingAssembly(), logger);
        }

        private static void RegisterWithInterfaces<T>(params Type[] interfaces) where T : class
        {
            if (ClassInjector.IsTypeRegisteredInIl2Cpp<T>())
            {
                return;
            }

            ClassInjector.RegisterTypeInIl2Cpp<T>(new RegisterTypeOptions
            {
                Interfaces = interfaces
            });
        }
    }
}
