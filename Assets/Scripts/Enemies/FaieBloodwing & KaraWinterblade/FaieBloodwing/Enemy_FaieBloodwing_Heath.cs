using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_FaieBloodwing_Health : MonoBehaviour, IEnemy_Health
{
    [Header("Shared Pool")]
    [SerializeField] private Enemy_Faie_Kara_ShareHealth sharedHealth;

    [Header("Individual Stats")]
    public EnemyStats stats;       // Faie's own stats — Strength, AttackRange, etc.
    public BehaviorProfile behavior;
    public int currentLevel = 1;
    [SerializeField] private GameObject faiePhase2Prefab;

    [Header("Audio")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;
    private StatusEffectManager statusEffectManager;
    public bool isDead => sharedHealth.IsDead;

    private Enemy_FaieBloodwing_Movement movementComponent;
    private void Awake()
    {
        if (sharedHealth == null)
            sharedHealth = Enemy_Faie_Kara_ShareHealth.GetOrCreate();

        if (sharedHealth.maxHP == 0)
            sharedHealth.InitHP(stats.MaxHP);
        InitializeStats();
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_FaieBloodwing_Movement>();
        statusEffectManager = GetComponent<StatusEffectManager>();

        sharedHealth.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (sharedHealth != null)
            sharedHealth.OnDeath -= HandleDeath;
    }

    public void InitializeStats()
    {
        var data = EnemyDataRepository.LoadEnemy("FaieBloodwing");

        stats = EnemyDataRepository.ApplyScalling(data.Stats, data.Growth, currentLevel);
        behavior = data.Behavior;

        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
    }

    public void ChangeHealth(float amount) => sharedHealth.ChangeHealth(amount);

    private void HandleDeath()
    {
        statusEffectManager.RemoveAll();
        movementComponent.GetStateManager()?.ChangeState(Enemy_FaieBloodwing_State.Death);
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        SoundFXManager.Instance.PlaySoundFXClip(deathAudio, transform, volume);
        yield return new WaitForSeconds(2f);

        sharedHealth.RegisterPosition(isFaie: true, transform.position, faiePhase2Prefab);
        statusEffectManager.RemoveAll();
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
        behavior.ApplyDifficulty(newModifier);
    }
}