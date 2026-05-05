using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaraWinterblade_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Shared Pool")]
    [SerializeField] private Enemy_Faie_Kara_ShareHealth sharedHealth;

    [Header("Individual Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    [SerializeField] private GameObject faiePhase2Prefab;

    [Header("Audio")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;

    public bool isDead => sharedHealth.IsDead;

    private StatusEffectManager statusEffectManager;
    private Enemy_KaraWinterblade_Movement movementComponent;
    private void Awake()
    {
        if (sharedHealth == null)
            sharedHealth = Enemy_Faie_Kara_ShareHealth.GetOrCreate();
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_KaraWinterblade_Movement>();
        statusEffectManager = GetComponent<StatusEffectManager>();

        if (sharedHealth.currentHP == 0)
            sharedHealth.InitHP(stats.MaxHP);

        sharedHealth.OnDeath += HandleDeath;
        var bossBorder = FindAnyObjectByType<BossFightBorder>();
        bossBorder.AddEnemy(gameObject);
    }

    private void OnDestroy()
    {
        if (sharedHealth != null)
            sharedHealth.OnDeath -= HandleDeath;
    }

    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("KaraWinterblade");

        stats = EnemyDataRepository.ApplyScalling(data.Stats, data.Growth, currentLevel);
        behavior = data.Behavior;

        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
    }

    public void ChangeHealth(float amount) => sharedHealth.ChangeHealth(amount);

    private void HandleDeath()
    {
        statusEffectManager.RemoveAll();
        movementComponent.GetStateManager()?.ChangeState(Enemy_KaraWinterblade_State.Death);
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        SoundFXManager.Instance.PlaySoundFXClip(deathAudio, transform, volume);
        yield return new WaitForSeconds(2f);

        StartCoroutine(movementComponent.DeathMovingCoroutine());
        statusEffectManager.RemoveAll();

        sharedHealth.RegisterPosition(isFaie: false, transform.position, faiePhase2Prefab, transform);
        Destroy(gameObject, 10f);
    }

    public void SpawnPhase2() => sharedHealth.SpawnPhase2();

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
        behavior.ApplyDifficulty(newModifier);
    }
}