using UnityEngine;
using System.Collections;

/// <summary>
/// Attack logic for Slime enemy.
/// Handles Jump (normal) and Spin (special mobility) attacks.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Slime_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float damageInterval = 1f;

    [Header("Jump Attack")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Spin Attack")]
    [SerializeField] private float spinSpeed = 10f;
    [Header("Audio")]
    [SerializeField] private AudioClip jumpAttackAudioClip;
    [SerializeField] private AudioClip spinAttackAudioClip;
    [SerializeField] private AudioClip[] hitAudioClip;
    [SerializeField] private AudioClip[] bounceAudioClip;

    [SerializeField] private float volume = 1f;

    private EnemyStats stats;
    private Rigidbody2D rb;
    private float lastDamageTime = 0f;

    private Vector2 velocity;
    private BehaviorProfile behaviorProfile;
    private Enemy_Slime_Movement movement;

    private void Start()
    {
        stats = GetComponent<Enemy_Slime_Health>().stats;
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<Enemy_Slime_Movement>();
        behaviorProfile = movement.GetBehavior();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("NPC"))
        {
            TryDealDamageToTarget(collision.transform);
        }
        else
        {
            if (collision.contacts.Length > 0)
            {
                Vector2 bounceDirection = Vector2.Reflect(velocity.normalized, collision.contacts[0].normal);
                rb.velocity = bounceDirection * spinSpeed;
                velocity = rb.velocity;

                if (bounceAudioClip != null)
                {
                    SoundFXManager.Instance.PlayRandomSoundFXClips(bounceAudioClip, transform, volume);
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("NPC"))
        {
            TryDealDamageToTarget(collision.transform);
        }
    }

    private void TryDealDamageToTarget(Transform target)
    {
        if (Time.time - lastDamageTime >= damageInterval)
        {
            if (hitAudioClip != null)
            {
                SoundFXManager.Instance.PlayRandomSoundFXClips(hitAudioClip, transform, volume);
            }

            if (target.CompareTag("Player"))
            {
                StatsManager.instance.TakeDamage(stats.Strength);

                if (target.TryGetComponent<PlayerMovement>(out var playerMovement))
                {
                    playerMovement.KnockBack(transform, stats.KnockbackForce, stats.KnockbackTime, stats.StunTime);
                }
            }
            else if (target.CompareTag("NPC"))
            {
                if (target.TryGetComponent<NPCCompanion>(out var npc))
                {
                    npc.TakeDamage(stats.Strength, false); // isFromPlayer = false
                }
            }

            lastDamageTime = Time.time;
        }
    }

    public void JumpAttack()
    {
        Transform target = movement.PlayerTransform;
        if (target == null) return;

        if (jumpAttackAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(jumpAttackAudioClip, transform, volume);
        }

        Vector2 direction = (target.position - transform.position).normalized;
        direction.x *= jumpForce * behaviorProfile.Aggression;
        direction.y *= jumpForce * behaviorProfile.Aggression;

        rb.velocity = direction;
    }

    public void StartSpinAttack()
    {
        Transform target = movement.PlayerTransform;
        if (spinAttackAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(spinAttackAudioClip, transform, volume);
        }

        if (target != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            rb.velocity = direction * spinSpeed * behaviorProfile.Aggression;
        }
        velocity = rb.velocity;
    }
}