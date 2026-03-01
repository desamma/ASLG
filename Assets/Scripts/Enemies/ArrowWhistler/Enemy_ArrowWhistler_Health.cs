using System;
using UnityEngine;
/// <summary>
/// Arrow Whistler enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_ArrowWhistler_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public float damageReductionPercentage = 0f;
    public bool isDead = false;
    public Action OnEnraged;
    private bool enragedTriggered = false;

    [Header("Components")]
    private Enemy_ArrowWhistler_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_ArrowWhistler_Movement>();
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
                manager.ChangeState(Enemy_ArrowWhistler_State.Death);
            }
            return;
        }

        if (stats.CurrentHP <= stats.MaxHP * 0.5f && !enragedTriggered && OnEnraged != null)
        {
            OnEnraged.Invoke();
            enragedTriggered = true;
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 200f,
            CurrentHP = 200f,
            Strength = 20,
            Magic = 0,
            Defense = 20f,
            MagicResist = 30f,
            Speed = 2.5f,
            ExpReward = 30f,
            AttackCooldown = 2.5f,
            AttackRange = 7f
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

