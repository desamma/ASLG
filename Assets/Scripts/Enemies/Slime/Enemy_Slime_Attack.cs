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
    [SerializeField] private LayerMask playerLayer;
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
    private Transform player;
    private float lastDamageTime = 0f;

    private Vector2 velocity;
    private BehaviorProfile behaviorProfile;
    private void Start()
    {
        stats = GetComponent<Enemy_Slime_Health>().stats;
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }
        behaviorProfile = GetComponent<Enemy_Slime_Movement>().GetBehavior();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryDealDamageToPlayer();
        }
        else
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TryDealDamageToPlayer();
        }
    }

    private void TryDealDamageToPlayer()
    {
        if (Time.time - lastDamageTime >= damageInterval)
        {
            if (hitAudioClip != null)
            {
                SoundFXManager.Instance.PlayRandomSoundFXClips(hitAudioClip, transform, volume);
            }

            // TODO: Apply damage to player
            // Example: player.GetComponent<PlayerHealth>()?.TakeDamage(stats.Strength);

            lastDamageTime = Time.time;
        }
    }

    public void JumpAttack()
    {
        if (player == null) return;

        if (jumpAttackAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(jumpAttackAudioClip, transform, volume);
        }

        Vector2 direction = (player.position - transform.position).normalized;
        direction.x *= jumpForce * behaviorProfile.Aggression;
        direction.y *= jumpForce * behaviorProfile.Aggression;

        rb.velocity = direction;
    }

    public void StartSpinAttack()
    {
        if (spinAttackAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(spinAttackAudioClip, transform, volume);
        }

        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * spinSpeed * behaviorProfile.Aggression;
        }
        velocity = rb.velocity;
    }
}