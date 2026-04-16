using UnityEngine;
using System;

public class ActiveStatusEffect
{
    public StatusEffect Definition { get; private set; }
    public float RemainingDuration { get; private set; }
    public float TotalDuration { get; private set; }
    public int StackCount { get; private set; }
    public string InstanceKey { get; private set; }

    // Runtime values — resolved at construction, never read back from SO
    public bool IsPermanent { get; private set; }
    public int MaxStacks { get; private set; }
    public StackBehavior StackBehavior { get; private set; }

    public bool IsExpired => !IsPermanent && RemainingDuration <= 0f;
    public float NormalizedTimeLeft => IsPermanent ? 1f : Mathf.Clamp01(RemainingDuration / TotalDuration);

    public event Action<ActiveStatusEffect> OnStackChanged;

    #region Overrides
    public string DescriptionOverride { get; private set; }
    public string FlavourOverride { get; private set; }
    public string[] StatLinesOverride { get; private set; }

    public string ResolvedDescription => string.IsNullOrEmpty(DescriptionOverride) ? Definition.description : DescriptionOverride;
    public string ResolvedFlavour => string.IsNullOrEmpty(FlavourOverride) ? Definition.flavourText : FlavourOverride;
    public string[] ResolvedStatLines => StatLinesOverride ?? Definition.statLines;

    #endregion

    public ActiveStatusEffect(StatusEffect definition, float duration,
                               int stack = 1,
                               bool isPermanent = false,
                               int maxStacks = -1,
                               StackBehavior stackBehavior = StackBehavior.Ignore,
                               bool allowDuplicate = false)
    {
        Definition = definition;
        TotalDuration = duration;
        RemainingDuration = duration;
        StackCount = Mathf.Max(1, stack);

        // Store resolved runtime values — SO fields are never touched after this
        IsPermanent = isPermanent || definition.isPermanent;
        MaxStacks = maxStacks < 1 ? definition.maxStacks : maxStacks;
        StackBehavior = stackBehavior == StackBehavior.Ignore
                        ? definition.stackBehavior : stackBehavior;

        InstanceKey = allowDuplicate
            ? $"{definition.effectId}_{Guid.NewGuid().ToString("N")[..6]}"
            : definition.effectId;
    }

    public void Tick(float deltaTime)
    {
        if (IsExpired) return;
        if (!IsPermanent)
        {
            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f) RemainingDuration = 0f;
        }
    }

    public void Reapply(float newDuration, int stackCount = 1)
    {
        switch (StackBehavior)
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
                if (StackCount < MaxStacks)
                {
                    StackCount += stackCount;
                    OnStackChanged?.Invoke(this);
                }
                RemainingDuration = newDuration;
                TotalDuration = newDuration;
                break;
            case StackBehavior.Ignore:
                break;
        }
    }

    public void ForceExpire() => RemainingDuration = 0f;

    public void SetStacks(int stacks)
    {
        StackCount = Mathf.Clamp(stacks, 1, MaxStacks);
        OnStackChanged?.Invoke(this);
    }

    #region Fluent setters
    public ActiveStatusEffect WithDescription(string description)
    {
        DescriptionOverride = description; return this;
    }

    public ActiveStatusEffect WithFlavour(string flavour)
    {
        FlavourOverride = flavour;
        return this;
    }

    public ActiveStatusEffect WithStatLines(params string[] lines)
    {
        StatLinesOverride = lines;
        return this;
    }

    public ActiveStatusEffect WithTooltip(string description = null, string flavour = null, params string[] statLines)
    {
        if (description != null) DescriptionOverride = description;
        if (flavour != null) FlavourOverride = flavour;
        if (statLines != null && statLines.Length > 0) StatLinesOverride = statLines;
        return this;
    }

    #endregion
}