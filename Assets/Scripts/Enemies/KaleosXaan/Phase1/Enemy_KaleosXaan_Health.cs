using UnityEngine;

/// <summary>
/// KaleosXaan enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public bool isDead;

    [Header("Components")]
    private Enemy_KaleosXaan_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_KaleosXaan_Movement>();
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
                manager.ChangeState(Enemy_KaleosXaan_State.Death);
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 2000f,
            CurrentHP = 2000f,
            Strength = 200f,
            Magic = 100f,
            Defense = 120f,
            MagicResist = 120f,
            Speed = 3f,
            ExpReward = 0f,
            AttackCooldown = 2.5f,
            AttackRange = 2f
        };
        isDead = false;
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
    }
}
