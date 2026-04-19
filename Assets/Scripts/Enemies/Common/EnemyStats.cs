using UnityEngine;

/// <summary>
/// Define a set of general stats for enemies
/// </summary>
[System.Serializable]
public class EnemyStats
{
    [Header("General Stats")]
    public float MaxHP;
    public float CurrentHP;
    public float Strength;
    public float Magic;
    public float Defense;
    public float MagicResist;
    public float Speed;
    public float ExpReward;
    public float GoldReward;

    [Header("Attacking")]
    public float AttackRange;
    public float AttackCooldown;

    [Header("Knockback")]
    public float KnockbackForce;
    public float KnockbackTime;
    public float StunTime;

    /// <summary>
    /// Apply difficulty modifiers to the enemy stats
    /// </summary>
    /// <param name="difficulty"></param>
    public void ApplyDifficulty(DifficultyModifier difficulty)
    {
        MaxHP *= difficulty.Resolve(difficulty.MaxHPMultiplier);
        Strength *= difficulty.Resolve(difficulty.StrengthMultiplier);
        Magic *= difficulty.Resolve(difficulty.MagicMultiplier);
        Defense *= difficulty.Resolve(difficulty.DefenseMultiplier);
        MagicResist *= difficulty.Resolve(difficulty.MagicResistMultiplier);
        Speed *= difficulty.Resolve(difficulty.SpeedMultiplier);
        GoldReward *= difficulty.Resolve(difficulty.GoldRewardMultiplier);
        
        CurrentHP = MaxHP;
    }
}

