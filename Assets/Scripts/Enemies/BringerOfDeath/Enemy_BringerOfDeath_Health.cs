using System.Collections;
using UnityEngine;

/// <summary>
/// Bringer of death enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_BringerOfDeath_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public bool isDead;

    private Enemy_BringerOfDeath_Movement movementComponent;

    private void Start()
    {
        InitializeStats();
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_BringerOfDeath_Movement>();
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

        stats.CurrentHP += amount;

        if (stats.CurrentHP > stats.MaxHP)
        {
            stats.CurrentHP = stats.MaxHP;
        }

        else if (stats.CurrentHP <= 0)
        {
            isDead = true;
        }

        // Play hurt animation when taking damage, but not during attack states
        if (amount < 0 && movementComponent != null)
        {
            var manager = movementComponent.GetStateManager();

            // Only play hurt animation if not in attack or cast state
            if (manager.GetCurrentState() != Enemy_BringerOfDeath_State.Attack &&
                manager.GetCurrentState() != Enemy_BringerOfDeath_State.Cast)
            {
                manager.ChangeState(Enemy_BringerOfDeath_State.Hurt);
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 550f,
            CurrentHP = 550f,
            Strength = 50f,
            Magic = 50f,
            Defense = 40f,
            MagicResist = 100f,
            Speed = 2f,
            ExpReward = 50f,
            AttackCooldown = 2.5f,
            AttackRange = 3f
        };
        isDead = false;
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
    }
}