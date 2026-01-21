using UnityEngine;

/// <summary>
/// Change the overall or each individual stats, and optionally modify behavior profiles
/// </summary>
[System.Serializable]
public class DifficultyModifier
{
    // Overall stats multiplier
    public float Overall = 1f;

    // Per-stat overrides (nullable = optional)
    public float? MaxHPMultiplier;
    public float? StrengthMultiplier;
    public float? MagicMultiplier;
    public float? DefenseMultiplier;
    public float? MagicResistMultiplier;
    public float? SpeedMultiplier;
    public float? AttackSpeedMultiplier;

    // Per-behavior overrides (nullable = optional)
    public float? DetectionRangeMultiplier;
    public float? ChaseRangeMultiplier;
    public float? SpecialAttackFrequencyMultiplier;
    public float? UltimateAttackFrequencyMultiplier;
    public float? AggressionMultiplier;
    public float? MinimumDistanceMultiplier;
    public float? MobilityUsageFrequencyMultiplier;
    public float? EnrageThresholdMultiplier;

    public float Resolve(float? statMultiplier)
    {
        return statMultiplier ?? Overall;
    }
}
