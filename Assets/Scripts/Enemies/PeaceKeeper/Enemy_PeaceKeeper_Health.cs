using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PeaceKeeper enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_PeaceKeeper_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Components")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;

    private Enemy_PeaceKeeper_Movement movementComponent;
    public event Action IsAttacked;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_PeaceKeeper_Movement>();
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
        if (amount < 0)
        {
            IsAttacked?.Invoke();
        }
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
                manager.ChangeState(Enemy_PeaceKeeper_State.Death);
                StartCoroutine(HandleDeathCoroutine());
            }
        }
    }

    private IEnumerator HandleDeathCoroutine()
    {
        SoundFXManager.Instance.PlaySoundFXClip(deathAudio, transform, volume);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("PeaceKeeper");

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
