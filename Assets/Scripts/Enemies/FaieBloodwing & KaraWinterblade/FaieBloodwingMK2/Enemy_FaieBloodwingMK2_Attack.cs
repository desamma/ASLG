using System.Collections;
using UnityEngine;

public class Enemy_FaieBloodwingMK2_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_FaieBloodwingMK2_Health health;
    [SerializeField] private Enemy_FaieBloodwingMK2_Movement movementComponent;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private float closeDistance = 3f;

    [Header("Normal Attack Settings")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private StatusEffect slowStatusFX;
    [SerializeField] private float slowOnHitValue = 0.3f;
    [SerializeField] private float slowTime = 3f;
    [SerializeField] private float stunWhenExceeds = 7f;
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private GameObject knockbackEffect;

    [Header("Blade Shoot Settings")]
    [SerializeField] private Transform bladePoint;
    [SerializeField] private float bladeHitBoxRadius;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float bladeSlowDuration = 1f;

    [Header("Warbird")]
    [SerializeField] private GameObject warbirdPrefab;
    [SerializeField] private StatusEffect azureSummoning;
    [SerializeField] private StatusEffect warbirdEffect;
    [SerializeField] private float warbirdSummonDelay = 5f;
    [SerializeField] private float warbirdChance = 0.05f;

    [Header("Accuracy Eye Settings")]
    [SerializeField] private GameObject accuracyEye;
    [SerializeField] private float accuracyEyeReAttackBonus = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootAudio;
    [SerializeField] private AudioClip swingAudio;
    [SerializeField] private AudioClip[] hitAudio;
    [SerializeField] private float volume = 1f;

    private float _slowRemainingTime = 0f;
    private Coroutine _slowCoroutine;
    private float playerOriginalMoveSpeed = -1f;
    private StatusEffectManager statusFXManager;
    private Coroutine warbirdCoroutine;
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_FaieBloodwingMK2_Health>();

        if (statusFXManager == null)
        {
            statusFXManager = GetComponent<StatusEffectManager>();
            ApplyStatusFX(2, statusFXManager, float.MaxValue);
        }

        if (movementComponent == null)
            movementComponent = GetComponent<Enemy_FaieBloodwingMK2_Movement>();

    }

    public void BladeAttack()
    {
        bool hitPlayer = true;
        PlayAudio(1);
        var hits = Physics2D.OverlapCircleAll(bladePoint.position, bladeHitBoxRadius, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
                {
                    player = hit.transform;

                    if (hit.CompareTag("Player"))
                    {
                        StatsManager.instance.TakeDamage(health.stats.Strength);
                        ApplySlowToPlayer(player, bladeSlowDuration);
                    }
                    else if (hit.CompareTag("NPC"))
                    {
                        if (hit.TryGetComponent<NPCCompanion>(out var npc))
                            npc.TakeDamage(health.stats.Strength, false);
                    }

                    if (player.TryGetComponent<StatusEffectManager>(out var statusEffectManager))
                    {
                        ApplyStatusFX(0, statusEffectManager, bladeSlowDuration);
                    }

                    if (hitPlayer)
                    {
                        Instantiate(hitEffect, hit.transform.position, Quaternion.identity);
                        PlayAudio(2);
                        hitPlayer = false;
                    }
                    if (warbirdCoroutine != null) return;

                    if (Random.value < warbirdChance)
                    {
                        ApplyWarbirdMark(player, statusEffectManager);
                    }
                }
            }
        }
    }

    public void NormalAttack()
    {
        PlayAudio(0);
        var hits = Physics2D.OverlapCircleAll(transform.position, health.stats.AttackRange, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
                {
                    player = hit.transform;
                    break;
                }
            }
        }

        if (player == null)
        {
            player = movementComponent.PlayerTransform;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= closeDistance)
        {
            if (player.CompareTag("Player"))
            {
                StatsManager.instance.TakeDamage(health.stats.Strength);

                if (player.TryGetComponent<PlayerMovement>(out var playerMovement))
                {
                    playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
                }

                ApplySlowToPlayer(player, slowTime);
            }
            else if (player.CompareTag("NPC"))
            {
                if (player.TryGetComponent<NPCCompanion>(out var npc))
                    npc.TakeDamage(health.stats.Strength, false);
            }

            if (knockbackEffect != null)
            {
                knockbackEffect.SetActive(true);
            }

            if (player.TryGetComponent<StatusEffectManager>(out var statusEffectManager))
            {
                ApplyStatusFX(0, statusEffectManager, slowTime);
                if(warbirdCoroutine != null) return;

                if (Random.value < warbirdChance)
                {
                    ApplyWarbirdMark(player, statusEffectManager);
                }
            }
        }

        else
        {
            Vector2 direction = (player.position - projectileSpawnPoint.position).normalized;
            GameObject proj = Instantiate(projectile, projectileSpawnPoint.position, Quaternion.identity);

            var spellComponent = proj.GetComponent<Enemy_ArrowWhistler_Arrow>();
            if (spellComponent != null && health != null)
            {
                spellComponent.Initialize(health.stats, direction, true,
                    extraEffect: playerTransform =>
                    {
                        if (playerTransform.CompareTag("Player"))
                        {
                            if (playerTransform.TryGetComponent<StatusEffectManager>(out var statusEffectManager))
                            {
                                ApplyStatusFX(0, statusEffectManager, slowTime);
                            }

                            ApplySlowToPlayer(playerTransform, slowTime);
                        }
                    });
            }

            proj.SetActive(true);
        }
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
        if (playerTransform.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            playerMovement.StopMovement(stunDuration);
        }
    }

    public void AccuracyEye()
    {
        var position = transform.position + new Vector3(0f, 1.4f, 0f);
        GameObject spellInstance = Instantiate(accuracyEye, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_FaieBloodwing_AccuracyEye>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, health, statusFXManager);
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

    private void ApplyWarbirdMark(Transform playerTransform, StatusEffectManager statusEffectManager)
    {
        ApplyStatusFX(1, statusEffectManager, warbirdSummonDelay);

        warbirdCoroutine = StartCoroutine(WarbirdSummonCoroutine(playerTransform, warbirdSummonDelay));
    }

    private IEnumerator WarbirdSummonCoroutine(Transform playerTransform, float delay)
    {
        yield return new WaitForSeconds(delay);
        var position = playerTransform.position + new Vector3(0f, -0.4f, 0f);
        GameObject warbirdInstance = Instantiate(warbirdPrefab, position, Quaternion.identity);
        var warbirdComponent = warbirdInstance.GetComponent<EnemyFaieBloodwingMK2_Warbird>();
        if (warbirdComponent != null && health != null)
        {
            warbirdComponent.Initialize(health.stats);
        }
        warbirdInstance.SetActive(true);
        warbirdCoroutine = null;
    }

    private void ApplyStatusFX(int num, StatusEffectManager statusEffectManager, float time)
    {
        switch (num)
        {
            case 0:
                statusEffectManager.ApplyEffect(slowStatusFX, false, duration: time, stackBehavior: StackBehavior.AddDuration)
                        .WithTooltip(
                            description: "Slowed by Faie Bloodwing's attack!",
                            statLines: new[] {
                                "- " + (slowOnHitValue * 100) + "% Move Speed for " + slowTime + " seconds",
                                "  Add duration each time hit",
                                "  Stuns if duration exceeds " + stunWhenExceeds + " seconds"
                            }
                        );
                break;
            case 1:
                statusEffectManager.ApplyEffect(azureSummoning, true, duration: time)
                .WithStatLines(new string[] { "A warbird will strike you in " + time + " seconds" });
                break;
            case 2:
                statusEffectManager.ApplyEffect(warbirdEffect, false, duration: time, isPermanent: true)
                    .WithStatLines(new string[] { (warbirdChance * 100) + "% Chance to call out a warbird when being hit by her blade" });
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
            case 1:
                if (swingAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(swingAudio, transform, volume * 0.7f);
                break;
            case 2:
                if (hitAudio != null && hitAudio.Length > 0)
                    SoundFXManager.Instance.PlayRandomSoundFXClips(hitAudio, transform, volume * 0.7f);
                break;
            default:
                Debug.LogWarning("Invalid audio number passed to PlayAudio: " + num);
                break;
        }
    }
}
