using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Kaleos Xaan Phase 2 Attack.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaanMK2_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_KaleosXaanMK2_Health health;
    [SerializeField] private Collider2D enemyCollider;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private int damageStack;
    [SerializeField] private int stackIncreaseEachHit = 2;
    [SerializeField] private int maxStack = 9999;
    [SerializeField] private StatusEffect stackStatusFX;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector3 normalAttackHitBox;
    [SerializeField] private GameObject attackHitEffect;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Saw Attack")]
    [SerializeField] private Transform sawAttackPoint;
    [SerializeField] private float sawAttackRadius;
    [SerializeField] private float sawDamageMultiplier = 0.3f;
    [SerializeField] private float sawTime = 2.5f;
    [SerializeField] private float damageInterval = 0.2f;

    [Header("Three Hit Combo")]
    [SerializeField] private float comboSawTime = 0.5f;

    [Header("Disappear")]
    [SerializeField] private GameObject disappearEffect;
    [SerializeField] private GameObject multiSlash;
    [SerializeField] private float multiSlashWaitDuration = 1.2f;
    [SerializeField] private float multiSlashTime = 10f;
    [SerializeField] private float multiSlashCooldown = 1.5f;

    [Header("Arcane Heart")]
    [SerializeField] private GameObject arcaneHeartPrefab;
    [SerializeField] private StatusEffect arcaneHeartStatusFX;

    [Header("Blink Enhance")]
    [SerializeField] private GameObject blinkEnhancePrefab;
    [SerializeField] private StatusEffect blinkEnhanceStatusFX;

    [Header("Daemonic Lure")]
    [SerializeField] private GameObject daemonicLurePrefab;

    [Header("Summon Companion")]
    [SerializeField] private GameObject companionPrefab;
    [SerializeField] private Transform[] companionSpawnPoints;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSwingAudioClip;
    [SerializeField] private AudioClip[] attackHitAudioClip;
    [SerializeField] private AudioClip sawSpinningAudioClip;
    [SerializeField] private AudioClip spinningParryAudioClip;
    [SerializeField] private AudioClip summonSound;
    [SerializeField] private float volume = 1f;

    private Coroutine sawCoroutine;
    private Coroutine parryCoroutine;
    private Coroutine multiSlashCoroutine;
    private AudioSource sawAudioSource;
    bool hitPlayer = false;

    private DifficultyModifier difficultyModifier;
    private Enemy_KaleosXaanMK2_Movement movementComponent;
    private StatusEffectManager effectManager;
    private bool isFirstTimeSpawnFX = true;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_KaleosXaanMK2_Health>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (movementComponent == null)
            movementComponent = GetComponent<Enemy_KaleosXaanMK2_Movement>();

        if (effectManager == null)
            effectManager = GetComponent<StatusEffectManager>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;

        var spellComponent = multiSlash.GetComponent<Enemy_KaleosXaan_MultiSlash>();

        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, damageStack, stackIncreaseEachHit, (int newStack) =>
            {
                AddStatusFX(0);
                damageStack = newStack;
            });
        }

        if (damageStack > 0)
        {
            AddStatusFX(0);
        }
    }

    public void Disappear()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (multiSlashCoroutine != null)
        {
            StopCoroutine(multiSlashCoroutine);
        }

        if (disappearEffect != null)
        {
            disappearEffect.SetActive(true);
        }

        SummonCompanion();
        multiSlashCoroutine = StartCoroutine(MultiSlashRoutine(multiSlashWaitDuration));
    }

    private IEnumerator MultiSlashRoutine(float waitDuration)
    {
        enemyCollider.enabled = false;
        yield return new WaitForSeconds(waitDuration);
        float elapsed = 0f;
        float cooldownTimer = 0f;
        float daemonicLureTimer = 0f;

        while (elapsed < multiSlashTime)
        {
            if (!multiSlash.activeSelf && cooldownTimer <= 0f)
            {
                multiSlash.transform.position = player.position;
                multiSlash.SetActive(true);
                cooldownTimer = multiSlashCooldown;
            }

            if (daemonicLureTimer <= 0f)
            {
                DaemonicLure();
                daemonicLureTimer = multiSlashCooldown * 3.3f;
            }

            elapsed += Time.deltaTime;
            cooldownTimer -= Time.deltaTime;
            daemonicLureTimer -= Time.deltaTime;
            yield return null;
        }

        if (multiSlash != null)
        {
            multiSlash.SetActive(false);
        }

        enemyCollider.enabled = true;
        multiSlash.transform.localPosition = Vector3.zero;
        multiSlashCoroutine = null;
    }

    public void NormalAttack()
    {
        PlayAudio(1);

        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    PlayAudio(0, volume * 0.4f);
                    hitPlayer = true;
                }
                Instantiate(attackHitEffect, player.position, Quaternion.identity, player.transform);
                DealDamage(false, withKnockback: true);
            }
        }
        movementComponent.FacingDirection = TransformHelper.FlipTowards(transform, player, movementComponent.FacingDirection);
        hitPlayer = false;
    }

    public void StartFlashUp()
    {
        var flashDirection = player.position.x > transform.position.x ? 1f : -1f;
        StartCoroutine(FlashUp(new Vector3(1.5f * flashDirection, 0f, 0f)));
    }

    private IEnumerator FlashUp(Vector3 flashDistance)
    {
        float timer = 0f;
        var currentPosition = transform.position;
        while (timer < flashDuration)
        {
            transform.position = Vector3.Lerp(currentPosition, currentPosition + flashDistance, timer / flashDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = currentPosition + flashDistance;
    }

    public void ThreeHitCombo(int num)
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
        StartFlashUp();
        switch (num)
        {
            case 0:
                NormalAttack();
                break;
            case 1:
                StartSaw(comboSawTime);
                break;
        }
    }

    public void StartSaw(float duration = -1f)
    {
        if (duration <= 0f)
        {
            duration = sawTime;
        }

        if (sawCoroutine != null)
            StopCoroutine(sawCoroutine);

        hitPlayer = false;
        PlayAudio(2);
        sawCoroutine = StartCoroutine(SawLoop(duration));
    }

    public void StopSaw()
    {
        if (sawCoroutine != null)
        {
            StopCoroutine(sawCoroutine);
            sawCoroutine = null;
        }
        SoundFXManager.Instance.StopAndDestroyAudioSource(sawAudioSource);
        sawAudioSource = null;
    }

    private IEnumerator SawLoop(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            SawTick();
            yield return new WaitForSeconds(damageInterval);
            timer += damageInterval;
        }

        StopSaw();
    }

    private void SawTick()
    {
        var hits = Physics2D.OverlapCircleAll(sawAttackPoint.position, sawAttackRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    PlayAudio(0, volume * 0.3f);
                    Instantiate(attackHitEffect, player.position, Quaternion.identity, player.transform);
                    hitPlayer = true;
                }
                Instantiate(attackSwingAudioClip, player.position, Quaternion.identity, player.transform);
                DealDamage(false, sawDamageMultiplier, false);
            }
        }
        hitPlayer = false;
    }

    public void ActiveParry(float duration)
    {
        PlayAudio(4);
        if (health != null)
        {
            health.isParry = true;
            if (parryCoroutine != null)
            {
                StopCoroutine(parryCoroutine);
            }
            parryCoroutine = StartCoroutine(DisableCounterAfterSeconds(duration));
        }
    }

    private IEnumerator DisableCounterAfterSeconds(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (health != null)
        {
            health.isParry = false;
        }
        parryCoroutine = null;
    }

    public void ArcaneHeart()
    {
        var position = transform.position + new Vector3(0f, 0.5f, 0f);
        GameObject spellInstance = Instantiate(arcaneHeartPrefab, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_ArcaneHeart>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, health);
        }

        spellInstance.SetActive(true);
        StartCoroutine(DelayStatusEffect(1f, () =>
        {
            AddStatusFX(1);
        }
        ));
    }

    public void BinkEnhance()
    {
        var position = transform.position + new Vector3(0f, 0.5f, 0f);
        GameObject spellInstance = Instantiate(blinkEnhancePrefab, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_BlinkEnhance>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, health);
        }

        spellInstance.SetActive(true);
        StartCoroutine(DelayStatusEffect(1f, () =>
        {
            AddStatusFX(2);
        }
        ));
    }

    private IEnumerator DelayStatusEffect(float duration, Action action)
    {
        yield return new WaitForSeconds(duration);
        action?.Invoke();
    }

    public void DaemonicLure()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        float playerFacingDirection = player.localScale.x > 0 ? 1f : -1f;
        float offsetDistance = 0.8f;
        Vector3 targetPosition = player.position + new Vector3(-playerFacingDirection * offsetDistance, 0f, 0f);

        GameObject spellInstance = Instantiate(daemonicLurePrefab, targetPosition, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_DaemonicLure>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats);
        }

        Vector3 directionToPlayer = player.position - spellInstance.transform.position;
        Vector3 localScale = spellInstance.transform.localScale;
        if (directionToPlayer.x < 0)
        {
            localScale.x = -Mathf.Abs(localScale.x);
        }
        else
        {
            localScale.x = Mathf.Abs(localScale.x);
        }
        spellInstance.transform.localScale = localScale;

        spellInstance.SetActive(true);
    }

    public void SummonCompanion()
    {
        if (movementComponent.IsCompanionActive()) return;
        var overlapChecker = new OverlapChecker2D();

        foreach (var spawnPoint in companionSpawnPoints)
        {
            if (spawnPoint == null) continue;

            if (!overlapChecker.IsOverlappingAnything(spawnPoint, 0.5f))
            {
                GameObject companion = Instantiate(companionPrefab, spawnPoint.position, spawnPoint.rotation);
                PlayAudio(3);

                if (movementComponent != null)
                {
                    movementComponent.RegisterCompanion(companion);
                }

                return;
            }
        }
    }

    private void DealDamage(bool isMagic, float damageMultiplier = 1f, bool withKnockback = true)
    {
        if (isMagic)
        {
            StatsManager.instance.TakeDamage((health.stats.Magic + damageStack) * damageMultiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier));
        }
        else
        {
            StatsManager.instance.TakeDamage((health.stats.Strength + damageStack) * damageMultiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier));
        }

        damageStack += stackIncreaseEachHit;

        if (damageStack > maxStack)
        {
            damageStack = maxStack;
        }
        else
        {
            AddStatusFX(0);
        }

        player.TryGetComponent<PlayerMovement>(out var playerMovement);

        if (playerMovement != null && withKnockback)
        {
            playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
        }
    }

    private void AddStatusFX(int num)
    {
        switch (num)
        {
            case 0:
                if (isFirstTimeSpawnFX)
                {
                    isFirstTimeSpawnFX = false;
                    effectManager.ApplyEffect(stackStatusFX, false, stackCount: stackIncreaseEachHit, stackBehavior: StackBehavior.AddStack, maxStacks: maxStack, isPermanent: true)
                    .WithTooltip(
                        description: "Add a damage stack with each successful hit to increase strength",
                        statLines: new string[] { "+ 1 Strength each stack" });
                }
                else
                {
                    effectManager.ApplyEffect(stackStatusFX, false, stackCount: stackIncreaseEachHit, stackBehavior: StackBehavior.AddStack, maxStacks: maxStack, isPermanent: true);
                }
                break;
            case 1:
                effectManager.ApplyEffect(arcaneHeartStatusFX, false, 22f);
                break;
            case 2:
                effectManager.ApplyEffect(blinkEnhanceStatusFX, false, 22f);
                break;
        }
    }

    public void PlayAudio(int num, float volumeOverride = -1)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlayRandomSoundFXClips(attackHitAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(attackSwingAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 2:
                sawAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(sawSpinningAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 3:
                SoundFXManager.Instance.PlaySoundFXClip(summonSound, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 4:
                SoundFXManager.Instance.PlaySoundFXClip(spinningParryAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}