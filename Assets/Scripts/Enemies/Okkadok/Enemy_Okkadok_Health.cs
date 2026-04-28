using System;
using UnityEngine;

/// <summary>
/// Okkadok enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Okkadok_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public float damageReductionPercentage = 0f;
    public bool isDead = false;

    [Header("Components")]
    private Enemy_Okkadok_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_Okkadok_Movement>();
    }

    private void OnEnable()
    {
        DifficultyManager.Instance.OnDifficultyChanged += OnDifficultyChanged;
    }

    private void OnDisable()
    {
        DifficultyManager.Instance.OnDifficultyChanged -= OnDifficultyChanged;
    }

    public void ChangeHealth(float amount)
    {
        if (isDead) return;

        if (amount < 0)
        {
            float reductionFactor = 1f - (damageReductionPercentage / 100f);
            amount *= reductionFactor;
        }

        stats.CurrentHP += amount;

        if (stats.CurrentHP > stats.MaxHP)
        {
            stats.CurrentHP = stats.MaxHP;
        }

        if (stats.CurrentHP <= 0)
        {
            stats.CurrentHP = 0;
            isDead = true;
            StatsManager.instance.AddExp(Mathf.RoundToInt(stats.ExpReward));
            StatsManager.instance.AddGold(Mathf.RoundToInt(stats.GoldReward));

            if (movementComponent != null)
            {
                var manager = movementComponent.GetStateManager();
                manager.ChangeState(Enemy_Okkadok_State.Death);
            }
            Destroy(gameObject, 2f);
        }
    }

    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("Okkadok");

        stats = EnemyDataRepository.ApplyScalling(data.Stats, data.Growth, currentLevel);
        behavior = data.Behavior;

        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
        behavior.ApplyDifficulty(newModifier);
    }

    public void OnDestroyAfterDeath(float seconds)
    {
        Destroy(gameObject, seconds);
    }
}
