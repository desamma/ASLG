using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this on a Canvas UI GameObject below the health bar. <para/>
/// Spawns/destroys <see cref="StatusEffectSlot"/> instances as effects are added/removed.
/// </summary>
public class StatusEffectHUD : MonoBehaviour
{
    [Header("Slot Setup")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Layout")]
    [Tooltip("Buffs shown before debuffs when true.")]
    [SerializeField] private bool separateBuffsAndDebuffs = true;

    // Keyed by InstanceKey — handles both shared and duplicate effects correctly
    private readonly Dictionary<string, StatusEffectSlot> _slots = new();

    public void AddEffect(ActiveStatusEffect active)
    {
        if (_slots.ContainsKey(active.InstanceKey)) return;

        var effectSlot = Instantiate(slotPrefab, slotContainer);
        if (!effectSlot.TryGetComponent<StatusEffectSlot>(out var slot))
        {
            Debug.LogError("[StatusEffectHUD] slotPrefab is missing StatusEffectSlot component.");
            Destroy(effectSlot);
            return;
        }

        slot.Initialize(active);
        _slots[active.InstanceKey] = slot;

        if (separateBuffsAndDebuffs)
            ReorderSlots();
    }

    public void RemoveEffect(ActiveStatusEffect active)
    {
        if (!_slots.TryGetValue(active.InstanceKey, out var slot)) return;
        _slots.Remove(active.InstanceKey);
        slot.PlayRemoveAnimation(() => Destroy(slot.gameObject));
    }

    public void RefreshEffect(ActiveStatusEffect active)
    {
        if (_slots.TryGetValue(active.InstanceKey, out var slot))
            slot.OnRefresh();
    }

    public void Clear()
    {
        foreach (var slot in _slots.Values)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        _slots.Clear();
    }

    private void ReorderSlots()
    {
        int index = 0;
        foreach (var slot in _slots.Values)
        {
            if (slot.EffectType == StatusEffectType.Buff)
                slot.transform.SetSiblingIndex(index++);
        }
        foreach (var slot in _slots.Values)
        {
            if (slot.EffectType == StatusEffectType.Neutral)
                slot.transform.SetSiblingIndex(index++);
        }
        foreach (var slot in _slots.Values)
        {
            if (slot.EffectType == StatusEffectType.Debuff)
                slot.transform.SetSiblingIndex(index++);
        }
    }
}