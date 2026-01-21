using UnityEngine;

/// <summary>
/// Defines behavioral parameters for enemies that scale with difficulty.
/// These values dynamically change how an enemy makes decisions.
/// </summary>
[System.Serializable]
public class BehaviorProfile
{
    [Header("Detection & Engagement")]
    [Tooltip("How far the enemy can detect the player (multiplier)")]
    public float DetectionRange = 1f;

    [Tooltip("How far the enemy will chase before giving up (multiplier)")]
    public float ChaseRange = 1f;

    [Header("Combat Behavior")]
    [Tooltip("How likely to use special attacks (0-1, where 1 = always use when available)")]
    [Range(0f, 1f)]
    public float SpecialAttackFrequency = 0.3f;

    [Tooltip("How likely to use ultimate attacks (0-1)")]
    [Range(0f, 1f)]
    public float UltimateAttackFrequency = 0.1f;

    [Tooltip("How aggressively the enemy pursues the player (affects decision timing)")]
    [Range(0f, 2f)]
    public float Aggression = 1f;

    [Tooltip("Minimum distance to maintain from player (defensive spacing)")]
    public float MinimumDistance = 0f;

    [Header("Pattern Complexity")]

    [Tooltip("How likely to use movement abilities/teleports (0-1)")]
    [Range(0f, 1f)]
    public float MobilityUsageFrequency = 0.2f;

    [Header("Health-Based Behavior")]
    [Tooltip("HP percentage threshold to trigger enraged/phase 2 behavior (0-1)")]
    [Range(0f, 1f)]
    public float EnrageThreshold = 0.5f;
    
    /// <summary>
    /// Apply difficulty scaling to this behavior profile
    /// </summary>
    public void ApplyDifficulty(DifficultyModifier modifier)
    {
        DetectionRange *= modifier.Resolve(modifier.DetectionRangeMultiplier);
        ChaseRange *= modifier.Resolve(modifier.ChaseRangeMultiplier);

        SpecialAttackFrequency = Mathf.Clamp01(SpecialAttackFrequency * modifier.Resolve(modifier.SpecialAttackFrequencyMultiplier));
        UltimateAttackFrequency = Mathf.Clamp01(UltimateAttackFrequency * modifier.Resolve(modifier.UltimateAttackFrequencyMultiplier));

        Aggression *= modifier.Resolve(modifier.AggressionMultiplier);
        MinimumDistance *= modifier.Resolve(modifier.MinimumDistanceMultiplier);

        MobilityUsageFrequency = Mathf.Clamp01(MobilityUsageFrequency * modifier.Resolve(modifier.MobilityUsageFrequencyMultiplier));

        EnrageThreshold = Mathf.Clamp01(EnrageThreshold * modifier.Resolve(modifier.EnrageThresholdMultiplier));
    }
}
