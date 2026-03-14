using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayneMK2_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_ArgeonHighmayneMK2_Health health;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    private float castRange;

    [Header("NormalAttack")]
    [SerializeField] private GameObject normalAttackHitEffect;
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector3 normalAttackHitBox;

    [Header("WarSurge")]
    [SerializeField] private Transform warSurgeAttackPoint;
    [SerializeField] private float warSurgeDamageMultiplier = 1.2f;
    [SerializeField] private float warSurgeRadius = 1f;

    [Header("DualCast Settings")]
    [SerializeField] private GameObject dualCastEffect;
    [SerializeField] private GameObject heavlyStrikeEffect;

    [Header("HeavenlyStrike")]
    [SerializeField] private Transform[] heavenlyStrikeSpawnPoints = new Transform[8];

    [Header("Decimate")]
    [SerializeField] private Transform decimateAttackPoint;
    [SerializeField] private Vector3 decimateBoxSize;
    [SerializeField] private GameObject decimateEffect;
    [SerializeField] private GameObject decimateChargeUpEffect;
    [SerializeField] private float decimateDamageMultiplier = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip normalAttackSwingAudioClip;
    [SerializeField] private AudioClip normalAttackHitAudioClip;
    [SerializeField] private AudioClip decimateAudio;
    [SerializeField] private AudioClip aurynNexusAudioClip;
    [SerializeField] private float volume = 1f;

    private bool hitPlayer = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayneMK2_Health>();
        castRange = health.stats.AttackRange * 3f;
        SpawnHeavenlyStrike();
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, 0f, playerLayer);
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

                StatsManager.instance.TakeDamage(health.stats.Strength);
                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
            }
        }
        hitPlayer = false;
    }

    public void DualCast()
    {
        GameObject spellInstance = Instantiate(dualCastEffect, transform.position, Quaternion.identity);

        var dualCastComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_DualCast>();
        if (dualCastComponent != null && health != null)
        {
            dualCastComponent.Initialize(health.stats, health);
        }

        spellInstance.SetActive(true);
    }

    public void WarSurgeAttack()
    {
        var hits = Physics2D.OverlapCircleAll(warSurgeAttackPoint.position, warSurgeRadius, playerLayer);

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
                StatsManager.instance.TakeDamage(health.stats.Strength * warSurgeDamageMultiplier);
                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
            }
        }
        hitPlayer = false;
    }

    public void Decimate()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            var hits = Physics2D.OverlapBoxAll(decimateAttackPoint.position, decimateBoxSize, playerLayer);

            if (hits.Length > 0)
            {
                StatsManager.instance.TakeDamage((health.stats.Strength + health.stats.Magic) * decimateDamageMultiplier);
            }

            GameObject spellInstance = Instantiate(decimateEffect, player.position, Quaternion.identity);

            var decimateComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_Decimate>();
            if (decimateComponent != null && health != null)
            {
                decimateComponent.Initialize(health.stats);
            }
            spellInstance.SetActive(true);
        }
    }

    public void PlayDecimateChargeUp()
    {
        float offsetY = -1.2f;
        Instantiate(decimateChargeUpEffect, transform.position + new Vector3(0, offsetY, 0), Quaternion.identity);
    }

    private void SpawnHeavenlyStrike()
    {
        for (int i = 0; i < heavenlyStrikeSpawnPoints.Length; i++)
        {
            if (heavenlyStrikeSpawnPoints[i] != null)
            {
                GameObject strikeInstance = Instantiate(heavlyStrikeEffect, heavenlyStrikeSpawnPoints[i].position, Quaternion.identity, transform);

                var strikeComponent = strikeInstance.GetComponent<Enemy_ArgeonHighmayneMK2_HeavenlyStrike>();
                if (strikeComponent != null && health != null)
                {
                    strikeComponent.Initialize(health.stats);
                }
            }
        }
    }

    public void ActiveHeavenlyStrikes()
    {
        StartCoroutine(ActivateHeavenlyStrikesCoroutine());
    }

    private IEnumerator ActivateHeavenlyStrikesCoroutine()
    {
        int activatedCount = 0;
        int immediateActivationCount = 4;

        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Enemy_ArgeonHighmayneMK2_HeavenlyStrike>(out var strikeComponent))
            {
                strikeComponent.gameObject.SetActive(true);
                activatedCount++;

                if (activatedCount == immediateActivationCount)
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(normalAttackPoint.position, normalAttackHitBox);
    }
    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 1:
                if (decimateAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(decimateAudio, transform, volume);
                break;
            case 2:
                if (normalAttackSwingAudioClip != null)
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume);
                break;
            case 3:
                if (aurynNexusAudioClip != null)
                    SoundFXManager.Instance.PlaySoundFXClip(aurynNexusAudioClip, transform, volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}
