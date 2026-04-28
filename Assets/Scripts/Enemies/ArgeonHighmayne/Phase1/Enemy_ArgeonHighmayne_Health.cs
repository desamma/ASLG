using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ArgeonHighmayne enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayne_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
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
        movementComponent = GetComponent<Enemy_ArgeonHighmayne_Movement>();
        effectManager = GetComponent<StatusEffectManager>();

        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            bossHealthUI.Initialize("Argeon Highmayne", stats.MaxHP);
            bossHealthUI.Show();
        }
        var bossBorder = FindAnyObjectByType<BossFightBorder>();
        bossBorder.AddEnemy(gameObject);
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
            StatsManager.instance?.AddExp(Mathf.RoundToInt(stats.ExpReward));
            StatsManager.instance?.AddGold(Mathf.RoundToInt(stats.GoldReward));

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
        var data = EnemyDataRepository.LoadEnemy("ArgeonHighmayne");

        stats = EnemyDataRepository.ApplyScalling(data.Stats, data.Growth, currentLevel);
        behavior = data.Behavior;

        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        isDead = false;
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
        behavior.ApplyDifficulty(newModifier);
    }
}
