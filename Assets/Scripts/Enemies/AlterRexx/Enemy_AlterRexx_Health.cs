using UnityEngine;

/// <summary>
/// AlterRexx enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_AlterRexx_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public bool isDead;

    private Enemy_AlterRexx_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_AlterRexx_Movement>();
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

        if (amount < 0 && movementComponent != null)
        {
            var manager = movementComponent.GetStateManager();
            if (manager.GetCurrentState() != Enemy_AlterRexx_State.Attack)
            {
                manager.ChangeState(Enemy_AlterRexx_State.Knockback);
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 200f,
            CurrentHP = 200f,
            Strength = 150f,
            Magic = 20f,
            Defense = 35f,
            MagicResist = 20f,
            Speed = 4f,
            ExpReward = 45f,
            AttackCooldown = 5f,
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
