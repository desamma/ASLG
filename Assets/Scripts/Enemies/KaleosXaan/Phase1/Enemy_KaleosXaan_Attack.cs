using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_KaleosXaan_Health health;
    [SerializeField] private StatusEffect stackStatusFX;
    [SerializeField] private int maxStack = 9999;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private NormalAttackPhase[] normalAttackPhases;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float normalAttackDamageMultiplier = 0.3f;
    [SerializeField] private float damageInterval = 0.3f;

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
    [SerializeField] private AudioClip normalAttackSwingAudioClip;
    [SerializeField] private AudioClip normalAttackHitAudioClip;
    [SerializeField] private AudioClip summonSound;
    [SerializeField] private float volume = 1f;

    [Header("Damage Stack")]
    [SerializeField] private int damageStack;
    [SerializeField] private int stackIncreaseEachHit = 2;

    private DifficultyModifier difficultyModifier;
    private StatusEffectManager effectManager;
    private Coroutine attackCoroutine;
    private NormalAttackPhase currentPhase;
    private bool hitSoundPlayed = false;
    private bool isFirstTimeSpawnFX = true;

    [Serializable]
    private class NormalAttackPhase
    {
        public Transform attackPoint;
        public float attackRadius = 1f;
        public float duration = 0.4f;
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_KaleosXaan_Health>();

        if (effectManager == null)
            effectManager = GetComponent<StatusEffectManager>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void StartNormalAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        hitSoundPlayed = false;
        attackCoroutine = StartCoroutine(NormalAttackPhaseLoop());
    }

    public void StopNormalAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        currentPhase = null;
        hitSoundPlayed = false;
    }

    private IEnumerator NormalAttackPhaseLoop()
    {
        if (normalAttackPhases == null || normalAttackPhases.Length == 0)
        {
            Debug.LogWarning("No attack phases defined!");
            yield break;
        }

        foreach (var phase in normalAttackPhases)
        {
            currentPhase = phase;

            float elapsed = 0f;

            while (elapsed < phase.duration)
            {
                NormalAttackTick(phase);
                yield return new WaitForSeconds(damageInterval);
                elapsed += damageInterval;
            }
        }

        // All phases done — stop naturally
        StopNormalAttack();
    }

    private void NormalAttackTick(NormalAttackPhase phase)
    {
        var hits = Physics2D.OverlapCircleAll(
            phase.attackPoint.position,
            phase.attackRadius,
            playerLayer
        );

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player") && !hit.CompareTag("NPC")) continue;

            player = hit.transform;

            if (!hitSoundPlayed)
            {
                PlayAudio(1);
                hitSoundPlayed = true;
            }

            if (hitEffect != null)
                Instantiate(hitEffect, player.position, Quaternion.identity, player.transform);

            if (hit.CompareTag("Player"))
            {
                DealDamage(false, normalAttackDamageMultiplier);
            }
            else if (hit.CompareTag("NPC"))
            {
                float damage = health.stats.Strength * normalAttackDamageMultiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);
                if (hit.TryGetComponent<NPCCompanion>(out var npc)) npc.TakeDamage(damage, false);
            }
        }
    }

    public void ArcaneHeart()
    {
        var position = transform.position + new Vector3(-0.2f, 1.7f, 0f);
        GameObject spellInstance = Instantiate(arcaneHeartPrefab, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_ArcaneHeart>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, health);
        }

        spellInstance.SetActive(true);
        StartCoroutine(DelayStatusEffect(1f, () =>
        {
            if (TryGetComponent<StatusEffectManager>(out var effectManager))
            {
                AddStatusFX(1);
            }
        }
        ));
    }

    public void BinkEnhance()
    {
        var position = transform.position + new Vector3(-0.2f, 1.7f, 0f);
        GameObject spellInstance = Instantiate(blinkEnhancePrefab, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_BlinkEnhance>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, health);
        }

        spellInstance.SetActive(true);
        StartCoroutine(DelayStatusEffect(1f, () =>
        {
            if (TryGetComponent<StatusEffectManager>(out var effectManager))
            {
                AddStatusFX(2);
            }
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

        // Flip the spell to face toward the player
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
        var overlapChecker = new OverlapChecker2D();

        foreach (var spawnPoint in companionSpawnPoints)
        {
            if (spawnPoint == null) continue;

            if (!overlapChecker.IsOverlappingAnything(spawnPoint, 0.5f))
            {
                Instantiate(companionPrefab, spawnPoint.position, spawnPoint.rotation);
                PlayAudio(2);
                return;
            }
        }
    }

    private void DealDamage(bool isMagic, float damageMultiplier = 1f)
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
    public void PlayAudio(int num, float volumeOverride = -1f)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 2:
                SoundFXManager.Instance.PlaySoundFXClip(summonSound, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }

    //ths so peak hmu
    //private void OnDrawGizmos()
    //{
    //    if (normalAttackPhases == null) return;

    //    Color[] phaseColors = { Color.red, Color.yellow, Color.cyan, Color.magenta };

    //    for (int i = 0; i < normalAttackPhases.Length; i++)
    //    {
    //        var phase = normalAttackPhases[i];
    //        if (phase.attackPoint == null) continue;

    //        // Highlight the active phase brighter at runtime
    //        bool isActive = Application.isPlaying && _currentPhase == phase;
    //        Gizmos.color = isActive
    //            ? phaseColors[i % phaseColors.Length]
    //            : new Color(phaseColors[i % phaseColors.Length].r,
    //                        phaseColors[i % phaseColors.Length].g,
    //                        phaseColors[i % phaseColors.Length].b, 0.3f);

    //        Gizmos.DrawWireSphere(phase.attackPoint.position, phase.attackRadius);
    //    }
    //}
}