using System;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps
{
    public class PlayerDotSpawner : MonoBehaviour
    {
        // IL2CPP interop requires this constructor for classes injected into the Il2Cpp
        // type system (see ClassInjector.RegisterTypeInIl2Cpp in Plugin.Load()). TODO: unverified for SPT 5.0.0.
        public PlayerDotSpawner(IntPtr ptr) : base(ptr) { }

        private static float _spawnTime = 0.25f;
        private float _timeAccumulator = 0f;

        public MapView MapView { get; set; }

        private void Update()
        {
            if (MapView == null)
            {
                return;
            }

            if (!Input.GetKey(KeyCode.M) || !Input.GetKey(KeyCode.LeftShift))
            {
                return;
            }

            _timeAccumulator += Time.deltaTime;
            if (_timeAccumulator <= _spawnTime && !Input.GetKeyDown(KeyCode.M))
            {
                return;
            }

            var markerDef = new MapMarkerDef
            {
                ImagePath = "Markers/dot.png",
                Position = MathUtils.ConvertToMapPosition(((IPlayer)GameUtils.GetMainPlayer()).Position)
            };

            MapView.AddMapMarker(markerDef);
            _timeAccumulator = 0f;
        }
    }
}
