using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Place this on a Canvas UI GameObject below the health bar.
/// It spawns/destroys StatusEffectSlot instances as effects are added/removed.
///
/// Wire up:
///   • slotPrefab  → the StatusEffectSlot prefab
///   • slotContainer → a HorizontalLayoutGroup transform
/// </summary>
public class StatusEffectHUD : MonoBehaviour
{
    [Header("Slot Setup")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Layout")]
    [Tooltip("Buffs shown before debuffs when true.")]
    [SerializeField] private bool separateBuffsAndDebuffs = true;

    private readonly Dictionary<string, StatusEffectSlot> _slots
        = new Dictionary<string, StatusEffectSlot>();

    // ── Public API called by StatusEffectManager ──────────────────────────

    public void AddEffect(ActiveStatusEffect active)
    {
        if (_slots.ContainsKey(active.Definition.effectId)) return;

        var go = Instantiate(slotPrefab, slotContainer);
        var slot = go.GetComponent<StatusEffectSlot>();

        if (slot == null)
        {
            Debug.LogError("[StatusEffectHUD] slotPrefab is missing StatusEffectSlot component.");
            Destroy(go);
            return;
        }

        slot.Initialise(active);
        _slots[active.Definition.effectId] = slot;

        if (separateBuffsAndDebuffs)
            ReorderSlots();
    }

    public void RemoveEffect(ActiveStatusEffect active)
    {
        if (!_slots.TryGetValue(active.Definition.effectId, out var slot)) return;
        _slots.Remove(active.Definition.effectId);
        slot.PlayRemoveAnimation(() => Destroy(slot.gameObject));
    }

    public void RefreshEffect(ActiveStatusEffect active)
    {
        if (_slots.TryGetValue(active.Definition.effectId, out var slot))
            slot.OnRefresh();
    }

    // ── Private ───────────────────────────────────────────────────────────
    private void ReorderSlots()
    {
        // Buffs first, then debuffs, then neutral
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