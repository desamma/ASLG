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
    public BehaviorProfile behavior;
    public int currentLevel = 1;
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
            StatsManager.instance?.AddExp(Mathf.RoundToInt(stats.ExpReward));
            StatsManager.instance?.AddGold(Mathf.RoundToInt(stats.GoldReward));

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
        var data = EnemyDataRepository.LoadEnemy("ArrowWhistler");

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

    public void OnDestroyAfterDeath(float seconds)
    {
        Destroy(gameObject, seconds);
    }
}

