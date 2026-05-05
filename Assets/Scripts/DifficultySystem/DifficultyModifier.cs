using UnityEngine;

[System.Serializable]
public class DifficultyModifier
{
    public float Overall = 1f;

    public float? MaxHPMultiplier;
    public float? StrengthMultiplier;
    public float? MagicMultiplier;
    public float? DefenseMultiplier;
    public float? MagicResistMultiplier;
    public float? SpeedMultiplier;
    public float? AttackSpeedMultiplier;
    public float? GoldRewardMultiplier;

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
