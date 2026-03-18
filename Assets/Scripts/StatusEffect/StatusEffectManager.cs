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
    [Header("HUD Reference")]
    [SerializeField] private StatusEffectHUD hud;

    [Header("VFX Anchor (optional — defaults to this transform)")]
    [SerializeField] private Transform vfxAnchor;

    public event Action<ActiveStatusEffect> OnEffectApplied;
    public event Action<ActiveStatusEffect> OnEffectRemoved;
    public event Action<ActiveStatusEffect> OnEffectTick;

    private readonly Dictionary<string, ActiveStatusEffect> _activeEffects = new();

    private readonly List<string> _toRemove = new();

    private void Awake()
    {
        if (vfxAnchor == null) vfxAnchor = transform;
    }

    private void Update()
    {
        _toRemove.Clear();

        foreach (var kvp in _activeEffects)
        {
            kvp.Value.Tick(Time.deltaTime);
            if (kvp.Value.IsExpired)
                _toRemove.Add(kvp.Key);
        }

        foreach (var id in _toRemove)
            RemoveEffectInternal(id);
    }

    /// <summary>
    /// Apply a status effect to this character. <para/>
    /// If <paramref name="duration"/> is not set, uses the SO's baseDuration.
    /// </summary>
    /// <returns>New active status object</returns>
    public ActiveStatusEffect ApplyEffect(StatusEffect definition, bool spawnVFX = true ,  float duration = -1f)
    {
        if (definition == null)
        {
            Debug.LogWarning("[StatusEffectManager] ApplyEffect called with null definition.");
            return null;
        }

        float d = duration < 0f ? definition.baseDuration : duration;

        if (_activeEffects.TryGetValue(definition.effectId, out var existing))
        {
            existing.Reapply(d);
            hud.RefreshEffect(existing);
            return existing;
        }

        var active = new ActiveStatusEffect(definition, d);
        active.OnTick += HandleTick;

        _activeEffects[definition.effectId] = active;

        if(spawnVFX)
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
        if (_activeEffects.ContainsKey(effectId))
            RemoveEffectInternal(effectId);
    }

    /// <summary>
    /// Remove all effects of a given type.
    /// </summary>
    public void RemoveAllOfType(StatusEffectType type)
    {
        var ids = _activeEffects
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
        foreach (var id in _activeEffects.Keys.ToList())
            RemoveEffectInternal(id);
    }

    public bool HasEffect(string effectId)
        => _activeEffects.ContainsKey(effectId);

    public ActiveStatusEffect GetEffect(string effectId)
        => _activeEffects.TryGetValue(effectId, out var e) ? e : null;

    public IReadOnlyCollection<ActiveStatusEffect> GetAllEffects()
        => _activeEffects.Values;

    private void RemoveEffectInternal(string id)
    {
        if (!_activeEffects.TryGetValue(id, out var active)) return;
        _activeEffects.Remove(id);

        hud.RemoveEffect(active);
        OnEffectRemoved?.Invoke(active);
    }

    private void HandleTick(ActiveStatusEffect effect)
        => OnEffectTick?.Invoke(effect);

    private void SpawnVFX(StatusEffect definition)
    {
        if (definition.vfxPrefab == null) return;
        Instantiate(definition.vfxPrefab, vfxAnchor.position, Quaternion.identity, vfxAnchor);
    }
}