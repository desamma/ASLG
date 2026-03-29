using UnityEngine;

/// <summary>
/// ArgeonHighmayne enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayne_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public float damageReductionPercentage = 0f;
    public bool isDead;
    private BossHealthUI bossHealthUI;

    private Enemy_ArgeonHighmayne_Movement movementComponent;
    private StatusEffectManager effectManager;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_ArgeonHighmayne_Movement>();
        effectManager = GetComponent<StatusEffectManager>();

        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            bossHealthUI.Initialize("Argeon Highmayne", stats.MaxHP);
            bossHealthUI.Show();
        }
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

        bossHealthUI.UpdateHealth(stats.CurrentHP);

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
                manager.ChangeState(Enemy_ArgeonHighmayne_State.Death);

                if (effectManager != null)
                {
                    effectManager.RemoveAll();
                }
            }
        }
    }

    public void InitializeStats()
    {
        stats = new EnemyStats
        {
            MaxHP = 1000f,
            CurrentHP = 1000f,
            Strength = 100f,
            Magic = 150f,
            Defense = 150f,
            MagicResist = 100f,
            Speed = 3.5f,
            ExpReward = 0f,
            AttackCooldown = 2f,
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
