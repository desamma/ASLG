using System.IO;
using UnityEngine;

/// <summary>
/// Reads and writes EnemyData from JSON files.
/// Files live in: Application.streamingAssetsPath/EnemyData/
/// </summary>
public static class EnemyDataRepository
{
    private static string RootPath => Path.Combine(Application.streamingAssetsPath, "EnemyData");

    public static EnemyData LoadEnemy(string enemyId)
        => JsonHelper.Load<EnemyData>(Path.Combine(RootPath, enemyId + ".json"));

    public static void SaveEnemy(string enemyId, EnemyData data)
        => JsonHelper.Save(Path.Combine(RootPath, enemyId + ".json"), data);

    /// <summary>
    /// Returns a new EnemyStats scaled to the given level.
    /// Level 1 = base stats unchanged.
    /// </summary>
    public static EnemyStats ApplyScalling(EnemyStats baseStats, EnemyGrowthProfile growth, int level)
    {
        if (level <= 1) return baseStats;

        int levelsGained = level - 1;

        return new EnemyStats
        {
            ExpReward =         baseStats.ExpReward        +   (growth.Exp <= 0 ? 10 : growth.Exp) *   levelsGained,    // 10 exp per level
            GoldReward =        baseStats.GoldReward       +   (growth.Gold <= 0 ? 5 : growth.Gold) *   levelsGained,     // 5 gold per level

            MaxHP =             baseStats.MaxHP            +   growth.MaxHP            *   levelsGained,
            CurrentHP =         baseStats.MaxHP            +   growth.MaxHP            *   levelsGained,
            Strength =          baseStats.Strength         +   growth.Strength         *   levelsGained,
            Magic =             baseStats.Magic            +   growth.Magic            *   levelsGained,
            Defense =           baseStats.Defense          +   growth.Defense          *   levelsGained,
            MagicResist =       baseStats.MagicResist      +   growth.MagicResist      *   levelsGained,
            Speed =             baseStats.Speed            +   growth.Speed            *   levelsGained,
            AttackRange =       baseStats.AttackRange,
            AttackCooldown =    baseStats.AttackCooldown   +   growth.AttackCooldown   *   levelsGained,

            KnockbackForce =    baseStats.KnockbackForce,
            KnockbackTime =     baseStats.KnockbackTime,
            StunTime =          baseStats.StunTime,
        };
    }
}