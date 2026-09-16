using System;
namespace DynamicMaps.UI.Components;

using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapScrollRect : ScrollRect
{
    // IL2CPP interop requires this constructor for classes injected into the Il2Cpp
    // type system (see ClassInjector.RegisterTypeInIl2Cpp in Plugin.Load()). TODO: unverified for SPT 5.0.0.
    public MapScrollRect(IntPtr ptr) : base(ptr) { }

    public System.Action OnBeginDragCallback;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        OnBeginDragCallback?.Invoke();
    }
}