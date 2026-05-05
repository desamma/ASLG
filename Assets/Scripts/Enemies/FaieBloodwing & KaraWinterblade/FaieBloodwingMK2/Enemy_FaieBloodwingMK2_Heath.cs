using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_FaieBloodwingMK2_Health : MonoBehaviour, IEnemy_Health
{
    [SerializeField] private GameObject monolith;

    [Header("Shared Pool")]
    [SerializeField] private Enemy_Faie_Kara_ShareHealth sharedHealth;

    [Header("Individual Stats")]
    public EnemyStats stats;
    public BehaviorProfile behavior;
    public int currentLevel = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip deathAudio;
    [SerializeField] private float volume = 1f;
    private StatusEffectManager statusEffectManager;
    public bool isDead => sharedHealth.IsDead;

    private Enemy_FaieBloodwingMK2_Movement movementComponent;
    private void Awake()
    {
        if (sharedHealth == null)
            sharedHealth = Enemy_Faie_Kara_ShareHealth.GetOrCreate();

        InitializeStats();
        sharedHealth.SetExpReward(stats.ExpReward);
        sharedHealth.SetGoldReward(stats.GoldReward);

        if (sharedHealth.maxHP == 0)
            sharedHealth.InitPhase2HP(stats.MaxHP);
    }

    private void Start()
    {
        movementComponent = GetComponent<Enemy_FaieBloodwingMK2_Movement>();
        statusEffectManager = GetComponent<StatusEffectManager>();

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
        var data = EnemyDataRepository.LoadEnemy("FaieBloodwingMK2");

        stats = EnemyDataRepository.ApplyScalling(data.Stats, data.Growth, currentLevel);
        behavior = data.Behavior;

        stats.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);
    }

    public void ChangeHealth(float amount) => sharedHealth.ChangeHealth(amount);

    private void HandleDeath()
    {
        statusEffectManager.RemoveAll();
        movementComponent.GetStateManager()?.ChangeState(Enemy_FaieBloodwingMK2_State.Death);
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        SoundFXManager.Instance.PlaySoundFXClip(deathAudio, transform, volume);
        yield return new WaitForSeconds(2f);
        Instantiate(monolith, transform.position + new Vector3(6f, 0f, 0f), Quaternion.identity);
        statusEffectManager.RemoveAll();
        Destroy(gameObject);
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        stats.ApplyDifficulty(newModifier);
        behavior.ApplyDifficulty(newModifier);
    }
}