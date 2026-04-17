using System.Collections;
using UnityEngine;

public class Enemy_FaieBloodwing_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_FaieBloodwing_Health health;
    [SerializeField] private Enemy_FaieBloodwing_Movement movementComponent;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack Settings")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject frozen;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private StatusEffect slowStatusFX;
    [SerializeField] private float slowOnHitValue = 0.3f;
    [SerializeField] private float slowTime = 3f;
    [SerializeField] private float stunWhenExceeds = 7f;
    [SerializeField] private float stunDuration = 2f;

    [Header("Accuracy Eye Settings")]
    [SerializeField] private GameObject accuracyEye;
    [SerializeField] private float accuracyEyeReAttackBonus = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootAudio;
    [SerializeField] private float volume = 1f;

    private float _slowRemainingTime = 0f;
    private Coroutine _slowCoroutine;
    private float playerOriginalMoveSpeed = -1f;
    private StatusEffectManager statusFXManager;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_FaieBloodwing_Health>();

        if(statusFXManager == null)
            statusFXManager = GetComponent<StatusEffectManager>();

        if (movementComponent == null)
            movementComponent = GetComponent<Enemy_FaieBloodwing_Movement>();
    }

    public void NormalAttack()
    {
        PlayAudio(0);
        var hits = Physics2D.OverlapCircleAll(transform.position, health.stats.AttackRange, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    player = hit.transform;
                    break;
                }
            }
        }

        if(player == null)
        {
            player = movementComponent.PlayerTransform;
        }

        Vector2 direction = (player.position - projectileSpawnPoint.position).normalized;
        GameObject proj = Instantiate(projectile, projectileSpawnPoint.position, Quaternion.identity);

        var spellComponent = proj.GetComponent<Enemy_ArrowWhistler_Arrow>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, direction, true,
                extraEffect: playerTransform =>
                {
                    if (playerTransform.TryGetComponent<StatusEffectManager>(out var statusEffectManager))
                    {
                        ApplyStatusFX(0, statusEffectManager);
                    }

                    ApplySlowToPlayer(playerTransform, slowTime);
                });
        }

        proj.SetActive(true);
    }

    private void ApplySlowToPlayer(Transform playerTransform, float duration)
    {
        if (_slowCoroutine != null)
        {
            _slowRemainingTime += duration;

            if (_slowRemainingTime > stunWhenExceeds)
            {
                ApplyStunToPlayer(playerTransform);
            }
            return;
        }

        _slowRemainingTime = duration;
        _slowCoroutine = StartCoroutine(SlowCoroutine(playerTransform));
    }

    private IEnumerator SlowCoroutine(Transform playerTransform)
    {
        if (playerOriginalMoveSpeed < 0f)
        {
            playerOriginalMoveSpeed = StatsManager.instance.moveSpeed;
        }

        StatsManager.instance.moveSpeed = playerOriginalMoveSpeed * (1f - slowOnHitValue);

        // Check if stun should be applied on initial slow
        if (_slowRemainingTime > 10f)
        {
            ApplyStunToPlayer(playerTransform);
        }

        while (_slowRemainingTime > 0f)
        {
            _slowRemainingTime -= Time.deltaTime;
            yield return null;
        }

        StatsManager.instance.moveSpeed = playerOriginalMoveSpeed;
        _slowCoroutine = null;
    }

    private void ApplyStunToPlayer(Transform playerTransform)
    {
        var position = playerTransform.position + new Vector3(0f, -1.1f, 0f);
        Instantiate(frozen, position, Quaternion.identity);
        if (playerTransform.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            playerMovement.StopMovement(stunDuration);
        }
    }

    public void AccuracyEye()
    {
        var position = transform.position + new Vector3(-0.2f, 1.7f, 0f);
        GameObject spellInstance = Instantiate(accuracyEye, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_FaieBloodwing_AccuracyEye>();
        if (spellComponent != null && health != null)
        {
            // Try to get Kara's components for cross-buffing
            Enemy_KaraWinterblade_Health karaHealth = FindObjectOfType<Enemy_KaraWinterblade_Health>();
            StatusEffectManager karaStatusManager = karaHealth != null ? karaHealth.GetComponent<StatusEffectManager>() : null;

            if (karaHealth != null && karaStatusManager != null)
            {
                spellComponent.InitializeWithCrossBuff(health.stats, health, statusFXManager, 
                    karaHealth.stats, karaHealth, karaStatusManager);
            }
            else
            {
                spellComponent.Initialize(health.stats, health, statusFXManager);
            }
        }

        spellInstance.SetActive(true);
        var reAttackChance = movementComponent != null ? movementComponent.GetReAttackChance() : 0f;

        if (movementComponent != null)
        {
            movementComponent.SetReAttackChance(reAttackChance + accuracyEyeReAttackBonus);
            StartCoroutine(ResetReAttackChanceAfterDelay(reAttackChance, 12f));
        }
    }

    private IEnumerator ResetReAttackChanceAfterDelay(float originalValue, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movementComponent != null)
        {
            movementComponent.SetReAttackChance(originalValue);
        }
    }

    private void ApplyStatusFX(int num, StatusEffectManager statusEffectManager)
    {
        switch (num)
        {
            case 0:
                statusEffectManager.ApplyEffect(slowStatusFX, true, duration: slowTime, stackBehavior: StackBehavior.AddDuration)
                        .WithTooltip(
                            description: "Slowed by Faie Bloodwing's attack!",
                            statLines: new[] { 
                                "- " + (slowOnHitValue * 100) + "% Move Speed for " + slowTime + " seconds",
                                "  Add duration each time hit",
                                "  Stuns if duration exceeds " + stunWhenExceeds + " seconds"
                            }
                        );
                break;
            default:
                Debug.LogWarning("Invalid status effect number passed to ApplyStatusFX: " + num);
                break;
        }
    }
    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 0:
                if (shootAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(shootAudio, transform, volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number passed to PlayAudio: " + num);
                break;
        }
    }
}
