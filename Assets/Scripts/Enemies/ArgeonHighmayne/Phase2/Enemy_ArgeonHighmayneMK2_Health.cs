using UnityEngine;

/// <summary>
/// Andromeda enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayneMK2_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public float damageReductionPercentage = 0f;
    public bool isDead;
    private BossHealthUI bossHealthUI;

    private Enemy_ArgeonHighmayneMK2_Movement movementComponent;
    private StatusEffectManager effectManager;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        movementComponent = GetComponent<Enemy_ArgeonHighmayneMK2_Movement>();
        effectManager = GetComponent<StatusEffectManager>();

        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            bossHealthUI.Initialize("Argeon Highmayne", stats.MaxHP, Color.yellow, Color.red, Color.white);
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
                manager.ChangeState(Enemy_ArgeonHighmayneMK2_State.Death);

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
            MaxHP = 2400f,
            CurrentHP = 2400f,
            Strength = 150f,
            Magic = 200f,
            Defense = 300f,
            MagicResist = 150f,
            Speed = 4f,
            ExpReward = 2000f,
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
    public void DestroyEnemy()
    {
        Destroy(gameObject);
        bossHealthUI.Hide();
    }
}
