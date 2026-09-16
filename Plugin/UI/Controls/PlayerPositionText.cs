using System;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public class PlayerPositionText : AbstractTextControl
    {
        // IL2CPP interop requires this constructor for classes injected into the Il2Cpp
        // type system (see ClassInjector.RegisterTypeInIl2Cpp in Plugin.Load()). TODO: unverified for SPT 5.0.0.
        public PlayerPositionText(IntPtr ptr) : base(ptr) { }

        public static PlayerPositionText Create(GameObject parent, float fontSize)
        {
            var text = Create<PlayerPositionText>(parent, "PlayerPositionText", fontSize);
            return text;
        }

        private void Update()
        {
            var player = GameUtils.GetMainPlayer();
            if (player == null)
            {
                return;
            }

            var mapPosition = MathUtils.ConvertToMapPosition(((IPlayer)player).Position);
            Text.text = $"Player: {mapPosition.x:F} {mapPosition.y:F} {mapPosition.z:F}";
        }
    }
}
