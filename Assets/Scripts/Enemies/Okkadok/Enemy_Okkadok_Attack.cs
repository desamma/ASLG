using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Okkadok_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_Okkadok_Health health;

    [Header("Attack Settings")]
    [SerializeField] private float firstAttackMultiplier = 0.8f;
    [SerializeField] private float secondAttackMultiplier = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackHitBox;
    [SerializeField] private GameObject attackHitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip[] attackAudio;
    [SerializeField] private AudioClip[] screamAudio;
    [SerializeField] private AudioClip hitAudio;
    [SerializeField] private float volume = 0.8f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_Okkadok_Health>();
    }

    public void NormalAttack(int attackNumber)
    {
        bool hitPlayer = false;
        var hits = Physics2D.OverlapBoxAll(attackPoint.position, attackHitBox, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    switch (attackNumber)
                    {
                        case 1:        
                            StatsManager.instance.TakeDamage(health.stats.Strength * firstAttackMultiplier);
                            break;
                        case 2:
                            StatsManager.instance.TakeDamage(health.stats.Strength * secondAttackMultiplier);
                            break;
                        default:
                            StatsManager.instance.TakeDamage(health.stats.Strength);
                            break;
                    }

                    if (hitPlayer == false)
                    {
                        if (attackHitEffect != null)
                            Instantiate(attackHitEffect, hit.transform.position, Quaternion.identity);

                        if (hitAudio != null)
                            SoundFXManager.Instance.PlaySoundFXClip(hitAudio, transform, volume);
                        hitPlayer = true;
                    }
                    break;
                }
            }
        }
        SoundFXManager.Instance.PlayRandomSoundFXClips(attackAudio, transform, volume);
    }

    public void PlayScreamAudio()
    {
        SoundFXManager.Instance.PlayRandomSoundFXClips(screamAudio, transform, volume);
    }
}
