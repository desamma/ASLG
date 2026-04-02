using UnityEngine;

/// <summary>
/// Bringer of death enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_BringerOfDeath_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Audio")]
    [SerializeField] private AudioClip damageAudioClip;
    [SerializeField] private float volume = 1f;

    private Enemy_BringerOfDeath_Movement movementComponent;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_BringerOfDeath_Movement>();
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
            if (damageAudioClip != null)
                SoundFXManager.Instance.PlaySoundFXClip(damageAudioClip, transform, volume);

            var manager = movementComponent.GetStateManager();
            if (manager.GetCurrentState() != Enemy_BringerOfDeath_State.Attack &&
                manager.GetCurrentState() != Enemy_BringerOfDeath_State.Cast)
            {
                manager.ChangeState(Enemy_BringerOfDeath_State.Hurt);
            }
        }
    }
    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("BringerOfDeath");

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