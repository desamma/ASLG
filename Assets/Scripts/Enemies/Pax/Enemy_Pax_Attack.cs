using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack logic for Andromeda enemy.
/// Attack performs 360° AOE damage around the enemy.
/// Cast is a special ranged spell attack.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Pax_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 attackBoxSize = new(1.7f, 2f);

    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float damageInterval = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip catHiss;
    [SerializeField] private AudioClip scratchSound;
    [SerializeField] private float volume = 1f;

    [Header("Components")]
    [SerializeField] private CapsuleCollider2D normalCollider;
    [SerializeField] private BoxCollider2D attackCollider;
    [SerializeField] private GameObject attackHitEffect;
    private Enemy_Pax_Movement movementComponent;

    private EnemyStats stats;
    private Rigidbody2D rb;
    private Transform player;
    private float lastDamageTime = 0f;
    private bool isAttacking = false;
    private bool soundPlayed = false;
    private bool hitEffectAnimated = false;

    private void Start()
    {
        stats = GetComponent<Enemy_Pax_Health>().stats;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        rb = GetComponent<Rigidbody2D>();
        movementComponent = GetComponent<Enemy_Pax_Movement>();
    }

    private void Update()
    {
        if (isAttacking)
        {
            CheckForHits();
        }
    }

    public void LauchForward()
    {
        if (player == null) return;
        Vector2 direction = (player.position - transform.position).normalized;
        direction.x *= jumpForce * movementComponent.GetBehavior().Aggression;
        direction.y *= jumpForce * movementComponent.GetBehavior().Aggression;

        rb.velocity = direction;
    }

    public void MeleeAttack()
    {
        isAttacking = true;
        soundPlayed = false;
    }

    public void EndMeleeAttack()
    {
        isAttacking = false;
        hitEffectAnimated = false;
    }

    private void CheckForHits()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, playerLayer);

        if (!soundPlayed && hits.Length > 0)
        {
            SoundFXManager.Instance.PlaySoundFXClip(scratchSound, transform, volume);
            soundPlayed = true;
        }

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (!hitEffectAnimated)
                {
                    Instantiate(attackHitEffect, hit.transform.position, Quaternion.identity, hit.transform);
                    hitEffectAnimated = true;
                }
                if (Time.time - lastDamageTime >= damageInterval)
                {
                    // TODO: Apply damage to player
                    // Example: player.GetComponent<PlayerHealth>()?.TakeDamage(stats.Strength);
                    lastDamageTime = Time.time;
                }
            }
        }
    }

    public void PlayCatSound()
    {
        SoundFXManager.Instance.PlaySoundFXClip(catHiss, transform, volume);
    }

    public void ChangeCollider()
    {
        normalCollider.enabled = !normalCollider.enabled;
        attackCollider.enabled = !attackCollider.enabled;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
    }
}
