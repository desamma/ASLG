using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Attach this to any character GameObject.
/// Manages all active status effects and drives the HUD display.
///
/// Quick usage:
///   statusEffectManager.ApplyEffect(myPoisonSO);
///   statusEffectManager.ApplyEffect(myShieldSO, duration: 10f);
///   statusEffectManager.RemoveEffect("poison");
///   statusEffectManager.HasEffect("shield_buff");
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("HUD Reference")]
    [SerializeField] private StatusEffectHUD hud;

    [Header("VFX Anchor (optional — defaults to this transform)")]
    [SerializeField] private Transform vfxAnchor;

    // ── Events ────────────────────────────────────────────────────────────
    public event Action<ActiveStatusEffect> OnEffectApplied;
    public event Action<ActiveStatusEffect> OnEffectRemoved;
    public event Action<ActiveStatusEffect> OnEffectTick;

    // ── State ─────────────────────────────────────────────────────────────
    private readonly Dictionary<string, ActiveStatusEffect> _activeEffects
        = new Dictionary<string, ActiveStatusEffect>();

    private readonly List<string> _toRemove = new List<string>();

    // ── Unity ─────────────────────────────────────────────────────────────
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

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Apply a status effect to this character.
    /// If <paramref name="duration"/> is -1, uses the SO's baseDuration.
    /// </summary>
    public ActiveStatusEffect ApplyEffect(StatusEffect definition, float duration = -1f)
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
            hud?.RefreshEffect(existing);
            return existing;
        }

        var active = new ActiveStatusEffect(definition, d);
        active.OnTick += HandleTick;
        active.OnExpired += HandleExpired;

        _activeEffects[definition.effectId] = active;

        SpawnVFX(definition);
        hud?.AddEffect(active);

        OnEffectApplied?.Invoke(active);
        return active;
    }

    /// <summary>Remove a specific effect by its effectId immediately.</summary>
    public void RemoveEffect(string effectId)
    {
        if (_activeEffects.ContainsKey(effectId))
            RemoveEffectInternal(effectId);
    }

    /// <summary>Remove all effects of a given type.</summary>
    public void RemoveAllOfType(StatusEffectType type)
    {
        var ids = _activeEffects
            .Where(kvp => kvp.Value.Definition.effectType == type)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in ids)
            RemoveEffectInternal(id);
    }

    /// <summary>Remove every active effect.</summary>
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

    // ── Private ───────────────────────────────────────────────────────────
    private void RemoveEffectInternal(string id)
    {
        if (!_activeEffects.TryGetValue(id, out var active)) return;
        _activeEffects.Remove(id);

        hud?.RemoveEffect(active);
        OnEffectRemoved?.Invoke(active);
    }

    private void HandleTick(ActiveStatusEffect effect)
        => OnEffectTick?.Invoke(effect);

    private void HandleExpired(ActiveStatusEffect effect)
    {
        // Expiry is caught in Update via IsExpired; this is just an early hook.
    }

    private void SpawnVFX(StatusEffect definition)
    {
        if (definition.vfxPrefab == null) return;
        Instantiate(definition.vfxPrefab, vfxAnchor.position, Quaternion.identity, vfxAnchor);
    }
}