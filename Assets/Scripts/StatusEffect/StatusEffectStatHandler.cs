using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Standalone stat modifier handler.

/// 1. Automatic (with StatusEffectManager on same GameObject)
///    GetStat() reads from the manager's active effects.
///
/// 2. Manual (no StatusEffectManager)
///    Call AddModifier() / RemoveModifier() when applying/removing effects.
///    GetStat() reads from the manual list instead.
/// </summary>
public class StatusEffectStatHandler : MonoBehaviour
{
    private StatusEffectManager manager;

    private void Awake()
    {
        manager = GetComponent<StatusEffectManager>();
    }

    // ── Manual modifier list (used when no manager, e.g. normal enemies) ──────
    private readonly List<ManualModifier> manualModifiers = new();
    private int nextHandle = 0;

    private class ManualModifier
    {
        public int Handle;
        public string StatName;
        public string GroupId;
        public ModifierType Type;
        public float Value;
        public InstanceStackMode StackMode;
    }

    /// <summary>
    /// Manually push a modifier. Returns a handle to remove it later.
    /// </summary>
    public int AddModifier(string groupId, string statName, ModifierType type, float value,
        InstanceStackMode stackMode = InstanceStackMode.TakeStrongest, float removeAfterTime = -1)
    {
        var handle = nextHandle++;
        manualModifiers.Add(new ManualModifier
        {
            Handle = handle,
            StatName = statName,
            GroupId = groupId,
            Type = type,
            Value = value,
            StackMode = stackMode
        });
        return handle;
    }
    /// <summary>
    /// Remove a manual modifier by handle.
    /// </summary>
    public void RemoveModifier(int handle)
        => manualModifiers.RemoveAll(m => m.Handle == handle);

    /// <summary>
    /// Remove all manual modifiers for a given groupId.
    /// </summary>
    public void RemoveAllFromGroup(string groupId)
        => manualModifiers.RemoveAll(m => m.GroupId == groupId);

    /// <summary>
    /// Returns the final stat value after applying all active modifiers.
    /// Sources: manager's active effects (if manager present) + manual modifiers.
    /// </summary>
    public float GetStat(string statName, float baseValue)
    {
        float additive = 0f;
        float multiplicative = 1f;
        float? overrideVal = null;

        // StatusEffectManager (Player / Boss)
        if (manager != null)
        {
            var grouped = manager.GetAllEffects()
                .Where(e => e.ResolvedModifier != null &&
                            e.ResolvedModifier.statName == statName)
                .GroupBy(e => e.Definition.effectId);

            foreach (var group in grouped)
            {
                var mod = group.First().ResolvedModifier;
                var values = group.Select(e => e.ResolvedModifier.value);

                Apply(mod.instanceStackMode, mod.type, values, mod, ref additive, ref multiplicative, ref overrideVal);
            }
        }

        //manual modifiers (normal enemies)
        if (manualModifiers.Count > 0)
        {
            var grouped = manualModifiers
                .Where(m => m.StatName == statName)
                .GroupBy(m => m.GroupId);

            foreach (var group in grouped)
            {
                var first = group.First();
                var values = group.Select(m => m.Value);
                var mod = new StatModifier
                {
                    type = first.Type,
                    value = first.Value
                };

                Apply(first.StackMode, first.Type, values, mod, ref additive, ref multiplicative, ref overrideVal);
            }
        }

        if (overrideVal.HasValue) return overrideVal.Value;
        return (baseValue + additive) * multiplicative;
    }

    private void Apply(InstanceStackMode stackMode, ModifierType type, IEnumerable<float> values, StatModifier modifier,
        ref float additive, ref float multiplicative, ref float? overrideVal)
    {
        float resolved = stackMode switch
        {
            InstanceStackMode.TakeStrongest => GetStrongest(values, modifier),
            InstanceStackMode.StackAdditive => values.Sum(),
            _ => values.Sum()
        };

        switch (type)
        {
            case ModifierType.Additive: additive += resolved; break;
            case ModifierType.Multiplicative: multiplicative *= resolved; break;
            case ModifierType.Override: overrideVal = resolved; break;
        }
    }

    private float GetStrongest(IEnumerable<float> values, StatModifier modifier)
    {
        bool isBuff = modifier.type == ModifierType.Multiplicative
            ? modifier.value > 1f
            : modifier.value > 0f;
        return isBuff ? values.Max() : values.Min();
    }
}