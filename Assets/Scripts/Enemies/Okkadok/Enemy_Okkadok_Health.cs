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
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

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

            if (movementComponent != null)
            {
                var manager = movementComponent.GetStateManager();
                manager.ChangeState(Enemy_Okkadok_State.Death);
            }
            return;
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 100f,
            CurrentHP = 100f,
            Strength = 35f,
            Magic = 0,
            Defense = 25f,
            MagicResist = 20f,
            Speed = 5f,
            ExpReward = 30f,
            AttackCooldown = 1.5f,
            AttackRange = 1.3f
        };
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
    }

    public void OnDestroyAfterDeath(float seconds)
    {
        Destroy(gameObject, seconds);
    }
}
