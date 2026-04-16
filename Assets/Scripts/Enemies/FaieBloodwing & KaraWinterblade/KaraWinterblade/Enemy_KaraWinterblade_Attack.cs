using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaraWinterblade_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_KaraWinterblade_Health health;
    [SerializeField] private Collider2D enemyCollider;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector3 normalAttackHitBox;
    [SerializeField] private GameObject attackHitEffect;
    [SerializeField] private StatusEffect frostFire;
    [SerializeField] private float frostFireSlow = 0.15f;
    [SerializeField] private float frostFireDuration = 5f;
    [SerializeField] private float frostFireTick = 0.3f;
    [SerializeField] private float frostFireBurnMultiplier = 0.2f;

    [Header("Three Hit Combo")]
    [SerializeField] private Transform comboAttackPoint;
    [SerializeField] private Vector3 comboAttackHitBox;
    [SerializeField] private float firstHitMultiplier = 1f;
    [SerializeField] private float secondHitMultiplier = 1.2f;
    [SerializeField] private float thirdHitMultiplier = 1.5f;

    [Header("Chormatic Cold")]
    [SerializeField] private GameObject chromaticColdEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip attackAudio;
    [SerializeField] private AudioClip attackHitAudio;
    [SerializeField] private float volume = 1f;

    bool hitPlayer = false;

    private DifficultyModifier difficultyModifier;
    private Enemy_KaraWinterblade_Movement movementComponent;
    private StatusEffectManager effectManager;
    private bool isChromaticColdActive = false;
    private float _slowRemainingTime = 0f;
    private Coroutine _slowCoroutine;
    private float playerOriginalMoveSpeed = -1f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_KaraWinterblade_Health>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (movementComponent == null)
            movementComponent = GetComponent<Enemy_KaraWinterblade_Movement>();

        if (effectManager == null)
            effectManager = GetComponent<StatusEffectManager>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void NormalAttack(int num)
    {
        hitPlayer = false;
        PlayAudio(1);
        Collider2D[] hits;
        if (num != 0)
            hits = Physics2D.OverlapBoxAll(comboAttackPoint.position, comboAttackHitBox, playerLayer);
        else
            hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    Instantiate(attackHitEffect, player.position, Quaternion.identity, player.transform);
                    PlayAudio(2);
                    hitPlayer = true;
                }

                switch (num)
                {
                    case 1:
                        DealDamage(false, firstHitMultiplier);
                        break;
                    case 2:
                        DealDamage(false, secondHitMultiplier);
                        break;
                    case 3:
                        DealDamage(false, thirdHitMultiplier);
                        break;
                    default:
                        DealDamage(false, isKnockback: true);
                        break;
                }

                if (isChromaticColdActive)
                {
                    player.TryGetComponent<StatusEffectManager>(out var playerStatusManager);
                    if (playerStatusManager != null)
                    {
                        ApplyStatusFX(1, playerStatusManager);
                        ApplySlowToPlayer(player, frostFireDuration);
                    }
                }
            }
        }
    }

    public void ChromaticCold()
    {
        isChromaticColdActive = true;
        var position = transform.position + new Vector3(0f, 1.5f, 0f);
        GameObject spellInstance = Instantiate(chromaticColdEffect, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaraWinterblade_ChromaticCold>();
        if (spellComponent != null && health != null)
        {
            // Try to get Faie's components for cross-buffing
            Enemy_FaieBloodwing_Health faieHealth = FindObjectOfType<Enemy_FaieBloodwing_Health>();
            StatusEffectManager faieStatusManager = faieHealth != null ? faieHealth.GetComponent<StatusEffectManager>() : null;

            if (faieHealth != null && faieStatusManager != null)
            {
                spellComponent.InitializeWithCrossBuff(health.stats, health, effectManager,
                    faieHealth.stats, faieHealth, faieStatusManager);
            }
            else
            {
                spellComponent.Initialize(health.stats, health, effectManager);
            }
        }
        spellInstance.SetActive(true);
        StartCoroutine(ResetChromaticColdAfterDelay(13f));
    }

    private IEnumerator ResetChromaticColdAfterDelay(float delay)
    {
        health.behavior.UltimateAttackFrequency += 0.35f;
        yield return new WaitForSeconds(delay);
        isChromaticColdActive = false;
    }

    private void ApplyStatusFX(int num, StatusEffectManager manager = null)
    {
        switch (num)
        {
            case 1:
                if (manager != null)
                {
                    manager.ApplyEffect(frostFire, true, frostFireDuration, stackBehavior: StackBehavior.AddDuration)
                        .WithStatLines(new string[] { "-" + (frostFireSlow * 100) + "% speed", "Burn for " + (frostFireBurnMultiplier * health.stats.Magic) + " every " + frostFireTick + "s" });
                }
                break;
            default:
                break;
        }
    }

    private void ApplySlowToPlayer(Transform playerTransform, float duration)
    {
        if (_slowCoroutine != null)
        {
            _slowRemainingTime += duration;
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

        StatsManager.instance.moveSpeed = playerOriginalMoveSpeed * (1f - frostFireSlow);

        float burnDamageTimer = 0f;
        while (_slowRemainingTime > 0f)
        {
            _slowRemainingTime -= Time.deltaTime;
            burnDamageTimer += Time.deltaTime;

            if (burnDamageTimer >= frostFireTick)
            {
                ApplyBurnDamage();
                burnDamageTimer = 0f;
            }

            yield return null;
        }

        StatsManager.instance.moveSpeed = playerOriginalMoveSpeed;
        _slowCoroutine = null;
    }

    private void ApplyBurnDamage()
    {
        float burnDamage = frostFireBurnMultiplier * health.stats.Magic;
        StatsManager.instance.TakeDamage(burnDamage);
    }

    private void DealDamage(bool isMagic, float multiplier = 1f, bool isKnockback = false)
    {
        if (isMagic)
        {
            StatsManager.instance.TakeDamage((health.stats.Magic) * multiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier));
        }
        else
        {
            StatsManager.instance.TakeDamage((health.stats.Strength) * multiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier));
        }
        if (isKnockback)
        {
            player.TryGetComponent<PlayerMovement>(out var playerMovement);
            playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
        }
    }
    private void PlayAudio(int num)
    {
        switch (num)
        {
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(attackAudio, transform, volume);
                break;
            case 2:
                SoundFXManager.Instance.PlaySoundFXClip(attackHitAudio, transform, volume);
                break;
            default:
                break;
        }
    }
}