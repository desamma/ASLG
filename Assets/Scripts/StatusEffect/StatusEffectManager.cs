using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Attach this to only PLAYER and BOSS GameObjects.
/// Manages all active status effects and drives the HUD display.
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private bool isPlayer = false;
    private StatusEffectHUD hud;

    [Header("VFX Anchor")]
    [Tooltip("Leave empty to use this transform")]
    [SerializeField] private Transform vfxAnchor;
    [SerializeField] private float destroyDelay = 2f;

    public event Action<ActiveStatusEffect> OnEffectApplied;
    public event Action<ActiveStatusEffect> OnEffectRemoved;

    private readonly Dictionary<string, ActiveStatusEffect> activeEffects = new();
    private readonly List<string> toRemove = new();

    private void Awake()
    {
        if (vfxAnchor == null) vfxAnchor = transform;

        string containerName = isPlayer ? "PlayerStatusEffectContainer" : "BossStatusEffectContainer";
        var container = GameObject.Find(containerName);

        if (container == null)
            Debug.LogWarning($"[StatusEffectManager] Could not find GameObject named '{containerName}'.");
        else
            hud = container.GetComponent<StatusEffectHUD>();
    }

    private void Update()
    {
        toRemove.Clear();
        foreach (var kvp in activeEffects)
        {
            kvp.Value.Tick(Time.deltaTime);
            if (kvp.Value.IsExpired) toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove) RemoveEffectInternal(id);
    }

    /// <summary>
    /// Apply a status effect. Returns the ActiveStatusEffect — store this reference
    /// if you need to reapply or remove it later (especially when allowDuplicate = true).
    /// </summary>
    /// <param name="allowDuplicate">
    /// false (default) — one slot per effectId; reapplying the same SO refreshes/stacks it.
    /// true            — each call creates an independent slot with its own timer.
    ///                   To refresh a specific duplicate, call ReapplyEffect(existingRef) instead.
    /// </param>
    public ActiveStatusEffect ApplyEffect(StatusEffect definition, bool spawnVFX = true, float duration = -1f, bool isPermanent = false, int stackCount = -1,
        StackBehavior stackBehavior = StackBehavior.Ignore, int maxStacks = 1, bool allowDuplicate = false)
    {
        if (definition == null)
        {
            Debug.LogWarning("[StatusEffectManager] ApplyEffect called with null definition.");
            return null;
        }

        if (definition.isPermanent != isPermanent)
        {
            if (isPermanent) definition.isPermanent = true;
            else if (!isPermanent && duration > 0f) definition.isPermanent = false;
        }

        var stacks = stackCount < 0 ? 1 : stackCount;
        definition.maxStacks = maxStacks <= 1 ? definition.maxStacks : maxStacks;
        if (stackBehavior != StackBehavior.Ignore) definition.stackBehavior = stackBehavior;

        // Non-duplicate: merge into the existing shared slot
        if (!allowDuplicate && activeEffects.TryGetValue(definition.effectId, out var existing))
        {
            existing.Reapply(duration, stacks);
            hud.RefreshEffect(existing);
            return existing;
        }

        var active = new ActiveStatusEffect(definition, duration, stacks, allowDuplicate);
        activeEffects[active.InstanceKey] = active;

        if (spawnVFX) SpawnVFX(definition);
        hud.AddEffect(active);
        OnEffectApplied?.Invoke(active);
        return active;
    }

    /// <summary>
    /// Reapply (refresh/stack) a specific existing instance by reference.
    /// Use this when allowDuplicate = true
    /// </summary>
    public void ReapplyEffect(ActiveStatusEffect instance, float duration = -1f, int stackCount = 1)
    {
        if (instance == null || !activeEffects.ContainsKey(instance.InstanceKey))
        {
            Debug.LogWarning("[StatusEffectManager] ReapplyEffect: instance not found — apply a new one instead.");
            return;
        }

        float dur = duration < 0f ? instance.Definition.baseDuration : duration;
        instance.Reapply(dur, stackCount);
        hud.RefreshEffect(instance);
    }

    /// <summary>
    /// Remove ALL instances of an effect by effectId (including duplicates).
    /// </summary>
    public void RemoveEffect(string effectId)
    {
        foreach (var key in activeEffects.Keys
            .Where(k => k == effectId || k.StartsWith(effectId + "_")).ToList())
            RemoveEffectInternal(key);
    }

    /// <summary>
    /// Remove one specific instance by reference. Safe to call with a stale ref.
    /// </summary>
    public void RemoveInstance(ActiveStatusEffect instance)
    {
        if (instance != null && activeEffects.ContainsKey(instance.InstanceKey))
            RemoveEffectInternal(instance.InstanceKey);
    }

    public void RemoveAllOfType(StatusEffectType type)
    {
        foreach (var id in activeEffects
            .Where(kvp => kvp.Value.Definition.effectType == type)
            .Select(kvp => kvp.Key).ToList())
            RemoveEffectInternal(id);
    }

    public void RemoveAll()
    {
        foreach (var id in activeEffects.Keys.ToList()) RemoveEffectInternal(id);
    }

    public bool HasEffect(string effectId)
        => activeEffects.Keys.Any(k => k == effectId || k.StartsWith(effectId + "_"));

    public ActiveStatusEffect GetEffect(string effectId)
        => activeEffects.TryGetValue(effectId, out var e) ? e : null;

    public IEnumerable<ActiveStatusEffect> GetInstances(string effectId)
        => activeEffects.Where(kvp => kvp.Key == effectId || kvp.Key.StartsWith(effectId + "_"))
                        .Select(kvp => kvp.Value);

    public IReadOnlyCollection<ActiveStatusEffect> GetAllEffects() => activeEffects.Values;


    //Internal

    private void RemoveEffectInternal(string key)
    {
        if (!activeEffects.TryGetValue(key, out var active)) return;
        activeEffects.Remove(key);
        hud.RemoveEffect(active);
        OnEffectRemoved?.Invoke(active);
    }

    private void SpawnVFX(StatusEffect definition)
    {
        if (definition.vfxPrefab == null) return;
        var effect = Instantiate(definition.vfxPrefab, vfxAnchor.position, Quaternion.identity, vfxAnchor);
        var parentScale = transform.localScale;
        var originalScale = definition.vfxPrefab.transform.localScale;
        effect.transform.localScale = new Vector3(
            originalScale.x / parentScale.x,
            originalScale.y / parentScale.y,
            originalScale.z / parentScale.z);
        Destroy(effect, destroyDelay);
    }
}