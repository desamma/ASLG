using UnityEngine;

/// <summary>
/// Andromeda enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Andromeda_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Components")]
    private Enemy_Andromeda_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_Andromeda_Movement>();
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
            StatsManager.instance?.AddExp(Mathf.RoundToInt(stats.ExpReward));
            StatsManager.instance?.AddGold(Mathf.RoundToInt(stats.GoldReward));
            
            if (movementComponent != null)
            {
                var manager = movementComponent.GetStateManager();
                manager.ChangeState(Enemy_Andromeda_State.Death);
            }
        }
    }

    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("Andromeda");

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
