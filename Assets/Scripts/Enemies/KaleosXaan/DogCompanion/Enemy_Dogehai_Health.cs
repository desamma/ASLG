using UnityEngine;

/// <summary>
/// Dogehai enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Dogehai_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public bool isDead;

    [Header("Components")]
    private Enemy_Dogehai_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_Dogehai_Movement>();
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
                manager.ChangeState(Enemy_Dogehai_State.Death);
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 200,
            CurrentHP = 200f,
            Strength = 20f,
            Magic = 0f,
            Defense = 60f,
            MagicResist = 30f,
            Speed = 3f,
            ExpReward = 100f,
            AttackCooldown = 2.5f,
            AttackRange = 1.5f
        };
        isDead = false;
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
