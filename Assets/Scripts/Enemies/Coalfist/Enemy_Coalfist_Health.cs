using System.Collections;
using UnityEngine;

/// <summary>
/// Coalfist enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Coalfist_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Components")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;

    private Enemy_Coalfist_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_Coalfist_Movement>();
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

            if (movementComponent == null)
                movementComponent = GetComponent<Enemy_Coalfist_Movement>();

            if (movementComponent != null)
            {
                var manager = movementComponent.GetStateManager();
                manager?.ChangeState(Enemy_Coalfist_State.Death);
            }

            StartCoroutine(HandleDeathCoroutine());
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
        var data = EnemyDataRepository.LoadEnemy("Coalfist");

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
