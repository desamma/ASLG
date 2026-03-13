using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Enemy_KaleosXaanMK2_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_KaleosXaanMK2_Health health;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector3 normalAttackHitBox;
    [SerializeField] private GameObject normalAttackHitEffect;

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

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (health == null)
            health = GetComponent<Enemy_KaleosXaanMK2_Health>();
    }
    public void NormalAttack()
    {
        bool hitPlayer = false;
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox , playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    hitPlayer = true;
                }

                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
                //TODO: Deal Damage to Player
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
    }

    public void BinkEnhance()
    {
        var position = transform.position + new Vector3(-0.2f, 1.7f, 0f);
        GameObject spellInstance = Instantiate(blinkEnhancePrefab, position, Quaternion.identity);

        var spellComponent = spellInstance.GetComponent<Enemy_KaleosXaan_BlinkEnhance>();
        if (spellComponent != null && health != null)
        {
            //spellComponent.Initialize(health.stats, health);
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
}