using System.Collections;
using UnityEngine;

/// <summary>
/// KaleosXaan enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Components")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;

    private Enemy_KaleosXaan_Movement movementComponent;
    private BossHealthUI bossHealthUI;
    private StatusEffectManager effectManager;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_KaleosXaan_Movement>();

        effectManager = GetComponent<StatusEffectManager>();

        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            ColorUtility.TryParseHtmlString("#CCCCCC", out Color bottom);
            bossHealthUI.Initialize("Kaleos Xaan", stats.MaxHP, Color.white, Color.red, bottom);
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
                manager.ChangeState(Enemy_KaleosXaan_State.Death);
                StartCoroutine(HandleDeathCoroutine());
            }
        }
    }

    private IEnumerator HandleDeathCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SoundFXManager.Instance.PlaySoundFXClip(deathAudio, transform, volume);

        if (effectManager != null)
        {
            effectManager.RemoveAll();
        }

        yield return new WaitForSeconds(5f);
        if (bossHealthUI != null)
        {
            bossHealthUI.Hide();
        }
        Destroy(gameObject);
    }
    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("KaleosXaan");

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
