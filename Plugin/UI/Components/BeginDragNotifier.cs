using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DynamicMaps.UI.Components;

/// <summary>
/// Sits next to a ScrollRect and reports when a drag starts. Subclassing ScrollRect to override OnBeginDrag
/// doesn't work for injected il2cpp types, so this implements IBeginDragHandler instead (registered in
/// <see cref="Il2CppTypes"/>) and Unity calls both handlers on the same GameObject
/// </summary>
public class BeginDragNotifier : MonoBehaviour
{
    public BeginDragNotifier(IntPtr ptr) : base(ptr) { }

    public Action OnBeginDragCallback;

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragCallback?.Invoke();
    }

    private void OnDestroy()
    {
        OnBeginDragCallback = null;
    }
}
