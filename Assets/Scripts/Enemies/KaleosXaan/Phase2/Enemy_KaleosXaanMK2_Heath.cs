using System.Collections;
using UnityEngine;

/// <summary>
/// KaleosXaanMK2 enemy Stats and Health
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaanMK2_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Enemy Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    public bool isDead;

    [Header("Components")]
    [SerializeField] private GameObject parryEffect;
    [SerializeField] private AudioClip parryAudioClip;
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;

    private Enemy_KaleosXaanMK2_Movement movementComponent;
    private BossHealthUI bossHealthUI;
    private StatusEffectManager effectManager;
    public bool isParry;
    public float parryTimer = 0f;

    private void Awake()
    {
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_KaleosXaanMK2_Movement>();

        effectManager = GetComponent<StatusEffectManager>();

        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            ColorUtility.TryParseHtmlString("#00C19A", out Color topLeft);
            ColorUtility.TryParseHtmlString("#CE2038", out Color bottomRight);

            bossHealthUI.Initialize("Kaleos Xaan", stats.MaxHP, Color.white, topLeft, Color.red, Color.black, bottomRight);
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

    private void Update()
    {
        if (parryTimer > 0)
        {
            parryTimer -= Time.deltaTime;
        }
    }
    public void ChangeHealth(float amount)
    {
        if (isDead) return;

        if (isParry && parryTimer <= 0)
        {
            if (parryEffect != null)
            {
                parryEffect.SetActive(true);
                parryTimer = 1.5f;
            }
            SoundFXManager.Instance.PlaySoundFXClip(parryAudioClip, transform, volume);
            return;
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
                manager.ChangeState(Enemy_KaleosXaanMK2_State.Death);
                StartCoroutine(HandleDeathCoroutine());
            }
        }
    }

    private IEnumerator HandleDeathCoroutine()
    {
        yield return new WaitForSeconds(0.45f);
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
        var data = EnemyDataRepository.LoadEnemy("KaleosXaanMK2");

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
