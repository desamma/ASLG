using UnityEngine;

/// <summary>
/// Slime enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Slime_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public bool isDead;

    [Header("Components")]
    private Enemy_Slime_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_Slime_Movement>();
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
            stats.CurrentHP = 0;
            isDead = true;
            
            if (movementComponent != null)
            {
                var manager = movementComponent.GetStateManager();
                manager.ChangeState(Enemy_Slime_State.Knockback);
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 100f,
            CurrentHP = 100f,
            Strength = 10f,
            Magic = 0f,
            Defense = 15f,
            MagicResist = 10f,
            Speed = 2f,
            ExpReward = 10f,
            AttackCooldown = 2f,
            AttackRange = 5f
        };
        isDead = false;
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
    }
}