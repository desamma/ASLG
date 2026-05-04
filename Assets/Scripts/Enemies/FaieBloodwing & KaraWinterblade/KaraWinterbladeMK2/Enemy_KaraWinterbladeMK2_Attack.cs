using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaraWinterbladeMK2_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D enemyCollider;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private StatusEffect chromaticColdEffect;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector3 normalAttackHitBox;
    [SerializeField] private GameObject attackHitEffect;
    [SerializeField] private StatusEffect frostFire;
    [SerializeField] private float frostFireSlow = 0.15f;
    [SerializeField] private float frostFireDuration = 5f;
    [SerializeField] private float frostFireTick = 0.3f;
    [SerializeField] private float frostFireBurnMultiplier = 0.2f;

    [Header("CryoGenesis")]
    [SerializeField] private GameObject cryoGenesisPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip attackAudio;
    [SerializeField] private AudioClip chargeAudio;
    [SerializeField] private AudioClip attackHitAudio;
    [SerializeField] private AudioClip cryoShoot;
    [SerializeField] private float volume = 1f;

    bool hitPlayer = false;

    private DifficultyModifier difficultyModifier;
    private Enemy_KaraWinterbladeMK2_Movement movementComponent;
    private Enemy_KaraWinterbladeMK2_Health health;
    private StatusEffectManager effectManager;
    private float _slowRemainingTime = 0f;
    private Coroutine _slowCoroutine;
    private float playerOriginalMoveSpeed = -1f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (movementComponent == null)
            movementComponent = GetComponent<Enemy_KaraWinterbladeMK2_Movement>();

        if (health == null)
            health = GetComponent<Enemy_KaraWinterbladeMK2_Health>();

        if (effectManager == null)
            effectManager = GetComponent<StatusEffectManager>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;

        if(chromaticColdEffect != null)
        {
            effectManager.ApplyEffect(chromaticColdEffect, duration: 0f, spawnVFX: false, isPermanent: true, stackBehavior: StackBehavior.Ignore)
                .WithStatLines(new string[] { "Kara Winterblade attack will apply <color=#25a5db>FrostFire</color> effect" });
        }
    }

    public void NormalAttack()
    {
        hitPlayer = false;
        PlayAudio(1);

        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, 0f, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    Instantiate(attackHitEffect, player.position, Quaternion.identity, player.transform);
                    PlayAudio(2);
                    hitPlayer = true;

                    DealDamage(false, isKnockback: true);
                }

                player.TryGetComponent<StatusEffectManager>(out var playerStatusManager);
                if (playerStatusManager != null)
                {
                    ApplyStatusFX(1, playerStatusManager);
                    ApplySlowToPlayer(player, frostFireDuration);
                }

            }
        }
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
    public void CryoGenesis()
    {
        PlayAudio(4);
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

        var position = player.position + new Vector3(0f, 0f, 0f);
        GameObject spellInstance = Instantiate(cryoGenesisPrefab, position, Quaternion.identity);
        if (spellInstance.TryGetComponent<Enemy_KaraWinterbladeMK2_Cryogenesis>(out var spellComponent))
        {
            spellComponent.Initialize(health.stats);
        }
        spellInstance.SetActive(true);
    }
    private void DealDamage(bool isMagic, float multiplier = 1f, bool isKnockback = false)
    {
        if (isMagic)
        {
            StatsManager.instance.TakeDamage((health.stats.Magic) * multiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier));
        }
        else
        {
            var thing = health;
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
            case 3:
                SoundFXManager.Instance.PlaySoundFXClip(chargeAudio, transform, volume);
                break;
            case 4:
                SoundFXManager.Instance.PlaySoundFXClip(cryoShoot, transform, volume);
                break;
            default:
                break;
        }
    }
}