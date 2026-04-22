using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemRuntimeCheck : MonoBehaviour
{
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    private void Start()
    {
        var all = FindObjectsOfType<EventSystem>(true);
        Debug.Log($"[EventSystemCheck] EventSystem count = {all.Length}");
        for (int i = 0; i < all.Length; i++)
            Debug.Log($"[EventSystemCheck] ES[{i}] = {all[i].name}, active={all[i].isActiveAndEnabled}");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current == null)
        {
            Debug.LogError("[EventSystemCheck] EventSystem.current is null.");
            return;
        }

        var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _hits.Clear();
        EventSystem.current.RaycastAll(data, _hits);

        if (_hits.Count == 0)
        {
            Debug.LogWarning("[EventSystemCheck] No UI hit.");
            return;
        }

        Debug.Log($"[EventSystemCheck] Top hit = {_hits[0].gameObject.name}");
        for (int i = 0; i < _hits.Count; i++)
            Debug.Log($"[EventSystemCheck] Hit[{i}] = {_hits[i].gameObject.name}");
    }
}