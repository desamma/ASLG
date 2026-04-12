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
    /// Apply a status effect. Chain .WithModifier() / .WithStatLines() etc on the
    /// returned instance for per-instance customisation — the SO is never touched.
    /// </summary>
    /// <param name="allowDuplicate">
    /// false — one slot per effectId; reapplying refreshes/stacks it.
    /// true  — independent slot per call, capped by SO's maxInstances.
    ///         Store the returned reference and call ReapplyEffect() to refresh it.
    /// </param>
    public ActiveStatusEffect ApplyEffect(StatusEffect definition, bool spawnVFX = true, float duration = -1f, bool isPermanent = false, int stackCount = -1,
        StackBehavior stackBehavior = StackBehavior.Ignore, int maxStacks = -1, bool allowDuplicate = false)
    {
        if (definition == null)
        {
            Debug.LogWarning("[StatusEffectManager] ApplyEffect called with null definition.");
            return null;
        }

        // Resolve all runtime values locally — never write back to the SO
        float dur = duration < 0f ? definition.baseDuration : duration;
        bool perm = isPermanent || definition.isPermanent;
        int stacks = stackCount < 1 ? 1 : stackCount;
        int resolvedMaxStacks = maxStacks < 1 ? definition.maxStacks : maxStacks;
        StackBehavior behavior = stackBehavior == StackBehavior.Ignore
                                  ? definition.stackBehavior : stackBehavior;

        // No duplicate: one shared slot
        if (!allowDuplicate)
        {
            if (activeEffects.TryGetValue(definition.effectId, out var existing))
            {
                existing.Reapply(dur, stacks);
                hud.RefreshEffect(existing);
                return existing;
            }

            var single = new ActiveStatusEffect(definition, dur, stacks, perm, resolvedMaxStacks, behavior, false);
            activeEffects[single.InstanceKey] = single;

            if (spawnVFX)
                SpawnVFX(definition);

            hud.AddEffect(single);
            OnEffectApplied?.Invoke(single);

            return single;
        }

        // Duplicate: check the SO's maxInstances cap
        var instances = GetInstances(definition.effectId).ToList();
        if (definition.maxInstances > 0 && instances.Count >= definition.maxInstances)
        {
            var oldest = instances.OrderBy(e => e.RemainingDuration).First();
            oldest.Reapply(dur, stacks);
            hud.RefreshEffect(oldest);
            return oldest;
        }

        var active = new ActiveStatusEffect(definition, dur, stacks, perm, resolvedMaxStacks, behavior, true);
        activeEffects[active.InstanceKey] = active;

        if (spawnVFX)
            SpawnVFX(definition);

        hud.AddEffect(active);
        OnEffectApplied?.Invoke(active);

        return active;
    }

    /// <summary>
    /// Refresh a specific instance you already hold a reference to.
    /// Use instead of ApplyEffect when allowDuplicate = true and you want to
    /// refresh YOUR slot rather than spawn a new one.
    /// </summary>
    public void ReapplyEffect(ActiveStatusEffect instance, float duration = -1f, int stackCount = 1)
    {
        if (instance == null || !activeEffects.ContainsKey(instance.InstanceKey))
        {
            Debug.LogWarning("[StatusEffectManager] ReapplyEffect: instance not found.");
            return;
        }
        float dur = duration < 0f ? instance.Definition.baseDuration : duration;
        instance.Reapply(dur, stackCount);
        hud.RefreshEffect(instance);
    }


    public void RemoveEffect(string effectId)
    {
        foreach (var key in activeEffects.Keys
            .Where(k => k == effectId || k.StartsWith(effectId + "_")).ToList())
            RemoveEffectInternal(key);
    }

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
        => activeEffects
            .Where(kvp => kvp.Key == effectId || kvp.Key.StartsWith(effectId + "_"))
            .Select(kvp => kvp.Value);

    public IReadOnlyCollection<ActiveStatusEffect> GetAllEffects() => activeEffects.Values;

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