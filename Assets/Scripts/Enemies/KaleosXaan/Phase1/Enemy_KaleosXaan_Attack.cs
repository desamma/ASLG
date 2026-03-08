using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_KaleosXaan_Health health;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private NormalAttackPhase[] normalAttackPhases;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float normalAttackDamage = 10f;
    [SerializeField] private float damageInterval = 0.3f;

    [Header("Arcane Heart")]
    [SerializeField] private GameObject arcaneHeartPrefab;

    [Header("Blink Enhance")]
    [SerializeField] private GameObject blinkEnhancePrefab;

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

    private Coroutine attackCoroutine;
    private bool hitSoundPlayed = false;
    private NormalAttackPhase currentPhase;

    [System.Serializable]
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
        if (phase.attackPoint == null) return;

        var hits = Physics2D.OverlapCircleAll(
            phase.attackPoint.position,
            phase.attackRadius,
            playerLayer
        );

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            player = hit.transform;

            if (!hitSoundPlayed)
            {
                SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume * 0.2f);
                hitSoundPlayed = true;
            }

            if (hitEffect != null)
                Instantiate(hitEffect, player.position, Quaternion.identity, player.transform);

            //if (hit.TryGetComponent<PlayerHealth>(out var playerHealth))
            //    playerHealth.TakeDamage(normalAttackDamage);
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
                SoundFXManager.Instance.PlaySoundFXClip(summonSound, transform, volume);
                return;
            }
        }
    }
    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume);
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