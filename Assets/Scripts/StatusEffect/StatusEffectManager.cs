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
            if (kvp.Value.IsExpired)
                toRemove.Add(kvp.Key);
        }

        foreach (var id in toRemove)
            RemoveEffectInternal(id);
    }
    /// <summary>
    /// Apply a status effect to this character.
    /// </summary>
    /// <returns>New active status object</returns>
    public ActiveStatusEffect ApplyEffect(StatusEffect definition, bool spawnVFX = true, float duration = -1f, bool isPermanent = false, int stackCount = -1
        , StackBehavior stackBehavior = StackBehavior.Ignore, int maxStacks = 1)
    {
        if (definition == null)
        {
            Debug.LogWarning("[StatusEffectManager] ApplyEffect called with null definition.");
            return null;
        }
        // Handle overrides
        var dur = duration < 0f ? definition.baseDuration : duration;

        if (definition.isPermanent != isPermanent)
        {
            if (isPermanent)
            {
                definition.isPermanent = true;
            }
            else if (!isPermanent && dur > 0f)
            {
                definition.isPermanent = false;
            }
        }

        var stacks = stackCount < 0 ? 1 : stackCount;
        definition.maxStacks = maxStacks <= 1 ? definition.maxStacks : maxStacks;

        if (stackBehavior != StackBehavior.Ignore)
            definition.stackBehavior = stackBehavior;

        if (activeEffects.TryGetValue(definition.effectId, out var existing))
        {
            existing.Reapply(dur, stacks);
            hud.RefreshEffect(existing);
            return existing;
        }

        var active = new ActiveStatusEffect(definition, dur, stacks);

        activeEffects[definition.effectId] = active;

        if (spawnVFX)
            SpawnVFX(definition);

        hud.AddEffect(active);

        OnEffectApplied?.Invoke(active);
        return active;
    }

    /// <summary>
    /// Remove a specific effect by its effectId immediately.
    /// </summary>
    public void RemoveEffect(string effectId)
    {
        if (activeEffects.ContainsKey(effectId))
            RemoveEffectInternal(effectId);
    }

    /// <summary>
    /// Remove all effects of a given type.
    /// </summary>
    public void RemoveAllOfType(StatusEffectType type)
    {
        var ids = activeEffects
            .Where(kvp => kvp.Value.Definition.effectType == type)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in ids)
            RemoveEffectInternal(id);
    }

    /// <summary>
    /// Remove every active effect.
    /// </summary>
    public void RemoveAll()
    {
        foreach (var id in activeEffects.Keys.ToList())
            RemoveEffectInternal(id);
    }

    public bool HasEffect(string effectId)
        => activeEffects.ContainsKey(effectId);

    public ActiveStatusEffect GetEffect(string effectId)
        => activeEffects.TryGetValue(effectId, out var e) ? e : null;

    public IReadOnlyCollection<ActiveStatusEffect> GetAllEffects()
        => activeEffects.Values;

    private void RemoveEffectInternal(string id)
    {
        if (!activeEffects.TryGetValue(id, out var active)) return;
        activeEffects.Remove(id);

        hud.RemoveEffect(active);
        OnEffectRemoved?.Invoke(active);
    }

    private void SpawnVFX(StatusEffect definition)
    {
        if (definition.vfxPrefab == null) return;
        var effect = Instantiate(definition.vfxPrefab, vfxAnchor.position, Quaternion.identity, vfxAnchor);
        Vector3 originalScale = definition.vfxPrefab.transform.localScale;

        var parentScale = transform.localScale;

        // Compensate for parent scale
        effect.transform.localScale = new Vector3(
            originalScale.x / parentScale.x,
            originalScale.y / parentScale.y,
            originalScale.z / parentScale.z
        );

        Destroy(effect, destroyDelay);
    }
}