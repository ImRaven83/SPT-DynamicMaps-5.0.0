using System;
using UnityEngine;

namespace DynamicMaps.UI.Controls
{
    public class CursorPositionText : AbstractTextControl
    {
        // IL2CPP interop requires this constructor for classes injected into the Il2Cpp
        // type system (see ClassInjector.RegisterTypeInIl2Cpp in Plugin.Load()). TODO: unverified for SPT 5.0.0.
        public CursorPositionText(IntPtr ptr) : base(ptr) { }

        private RectTransform _mapViewTransform;

        public static CursorPositionText Create(GameObject parent, RectTransform mapViewTransform, float fontSize)
        {
            var text = Create<CursorPositionText>(parent, "CursorPositionText", fontSize);
            text._mapViewTransform = mapViewTransform;

            return text;
        }

        private void Update()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapViewTransform, Input.mousePosition, null, out Vector2 mouseRelative);
            Text.text = $"Cursor: {mouseRelative.x:F} {mouseRelative.y:F}";
        }
    }
}
