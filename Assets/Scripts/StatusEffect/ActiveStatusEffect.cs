using UnityEngine;
using System;

/// <summary>
/// Runtime instance of a StatusEffect currently applied to a character.
/// Tracks remaining duration, stack count, and tick timing.
/// </summary>
public class ActiveStatusEffect
{
    public StatusEffect Definition { get; private set; }
    public float RemainingDuration { get; private set; }
    public float TotalDuration { get; private set; }
    public int StackCount { get; private set; }
    public bool IsExpired => !Definition.isPermanent && RemainingDuration <= 0f;

    /// <summary>
    /// 0–1 fill amount for the icon timer ring.
    /// </summary>
    public float NormalizedTimeLeft => Definition.isPermanent? 1f: Mathf.Clamp01(RemainingDuration / TotalDuration);

    public event Action<ActiveStatusEffect> OnTick;
    public event Action<ActiveStatusEffect> OnStackChanged;

    private float _tickTimer;

    public ActiveStatusEffect(StatusEffect definition, float duration)
    {
        Definition = definition;
        TotalDuration = duration;
        RemainingDuration = duration;
        StackCount = 1;
        _tickTimer = 0f;
    }


    /// <summary>
    /// Called by StatusEffectManager each frame.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (IsExpired) return;

        if (!Definition.isPermanent)
        {
            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f)
            {
                RemainingDuration = 0f;
                return;
            }
        }

        if (Definition.hasTick)
        {
            _tickTimer += deltaTime;
            if (_tickTimer >= Definition.tickInterval)
            {
                _tickTimer -= Definition.tickInterval;
                OnTick?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Reapply behaviour based on StackBehavior setting.
    /// </summary>
    public void Reapply(float newDuration)
    {
        switch (Definition.stackBehavior)
        {
            case StackBehavior.RefreshDuration:
                RemainingDuration = newDuration;
                TotalDuration = newDuration;
                break;

            case StackBehavior.AddDuration:
                RemainingDuration += newDuration;
                TotalDuration = RemainingDuration;
                break;

            case StackBehavior.AddStack:
                if (StackCount < Definition.maxStacks)
                {
                    StackCount++;
                    OnStackChanged?.Invoke(this);
                }
                RemainingDuration = newDuration;
                TotalDuration = newDuration;
                break;

            case StackBehavior.Ignore:
                // Do nothing
                break;
        }
    }

    public void ForceExpire()
    {
        RemainingDuration = 0f;
    }

    public void SetStacks(int stacks)
    {
        StackCount = Mathf.Clamp(stacks, 1, Definition.maxStacks);
        OnStackChanged?.Invoke(this);
    }
}