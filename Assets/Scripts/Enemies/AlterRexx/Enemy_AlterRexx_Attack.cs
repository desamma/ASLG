using UnityEngine;

/// <summary>
/// Attack logic for AlterRexx enemy.
/// Handles melee swing attack followed by lightning bolt summon.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_AlterRexx_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;

    [Header("Lightning Spell")]
    [SerializeField] private GameObject lightningBoltPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip charge;
    [SerializeField] private AudioClip meleeAttackHitAudioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats stats;
    private AudioSource chargeAudioSource;

    private void Start()
    {
        stats = GetComponent<Enemy_AlterRexx_Health>().stats;

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }
    }

    public void PlayChargeSound()
    {
        if (charge != null)
        {
            chargeAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(charge, transform, volume, 1f, 15f);
            if (chargeAudioSource != null)
            {
                chargeAudioSource.transform.SetParent(transform);
            }
        }
    }

    public void MeleeAttack()
    {
        bool audioPlayed = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, stats.AttackRange, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") && !audioPlayed)
            {
                SoundFXManager.Instance.StopAndDestroyAudioSource(chargeAudioSource);
                chargeAudioSource = null;

                SoundFXManager.Instance.PlaySoundFXClip(meleeAttackHitAudioClip, transform, volume);
                // TODO: Hook into player damage system
                // Apply physical damage based on stats.Strength
                audioPlayed = true;
            }
        }

        if (!audioPlayed && chargeAudioSource != null)
        {
            SoundFXManager.Instance.StopAndDestroyAudioSource(chargeAudioSource);
            chargeAudioSource = null;
        }

        SummonLightning();
    }

    public void SummonLightning()
    {
        if (lightningBoltPrefab == null)
        {
            return;
        }

        float xOffset = 2.2f;
        float yOffset = -1.7f;

        int facingDirection = transform.localScale.x > 0 ? 1 : -1;
        var spawnPosition = new Vector3(
            transform.position.x + (xOffset * facingDirection),
            transform.position.y + yOffset,
            transform.position.z
        );
        Instantiate(lightningBoltPrefab, spawnPosition, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null || stats == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, stats.AttackRange);
    }
}
