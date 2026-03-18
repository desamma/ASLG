using UnityEngine;
using System;

/// <summary>
/// Runtime instance of a StatusEffect currently applied to a character. <para/>
/// Tracks remaining duration, stack count, and per-instance description overrides.
/// The base <see cref="StatusEffect"/> SO is never modified.
/// </summary>
public class ActiveStatusEffect
{
    public StatusEffect Definition { get; private set; }
    public float RemainingDuration { get; private set; }
    public float TotalDuration { get; private set; }
    public int StackCount { get; private set; }
    public bool IsExpired => !Definition.isPermanent && RemainingDuration <= 0f;

    /// <summary>
    /// 0–1 fill for the timer ring.
    /// </summary>
    public float NormalizedTimeLeft => Definition.isPermanent ? 1f : Mathf.Clamp01(RemainingDuration / TotalDuration);

    public event Action<ActiveStatusEffect> OnStackChanged;

    #region Override fields
    // These do not modify the SO.
    public string DescriptionOverride { get; private set; }

    public string FlavourOverride { get; private set; }

    public string[] StatLinesOverride { get; private set; }

    // Read properties
    public string ResolvedDescription => string.IsNullOrEmpty(DescriptionOverride) ? Definition.description : DescriptionOverride;
    public string ResolvedFlavour => string.IsNullOrEmpty(FlavourOverride) ? Definition.flavourText : FlavourOverride;
    public string[] ResolvedStatLines => StatLinesOverride ?? Definition.statLines;
    #endregion

    public ActiveStatusEffect(StatusEffect definition, float duration)
    {
        Definition = definition;
        TotalDuration = duration;
        RemainingDuration = duration;
        StackCount = 1;
    }

    public void Tick(float deltaTime)
    {
        if (IsExpired) return;
        if (!Definition.isPermanent)
        {
            RemainingDuration -= deltaTime;
            if (RemainingDuration <= 0f)
                RemainingDuration = 0f;
        }
    }

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

    #region Override setters

    public ActiveStatusEffect WithTooltip(string description = null, string flavour = null, params string[] statLines)
    {
        if (description != null) DescriptionOverride = description;
        if (flavour != null) FlavourOverride = flavour;
        if (statLines != null && statLines.Length > 0) StatLinesOverride = statLines;
        return this;
    }
    public ActiveStatusEffect WithDescription(string description)
    {
        DescriptionOverride = description;
        return this;
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
    #endregion
}