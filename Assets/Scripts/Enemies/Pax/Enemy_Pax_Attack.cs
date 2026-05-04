using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pax attack logic, including melee attack and jump forward attack.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Pax_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackMultiplier = 0.3f;
    [SerializeField] private Vector2 attackBoxSize = new(1.7f, 2f);

    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float damageInterval = 0.3f;

    [Header("Audio")]
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
        player = movementComponent.PlayerTransform;
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
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                if (!hitEffectAnimated)
                {
                    Instantiate(attackHitEffect, hit.transform.position, Quaternion.identity, hit.transform);
                    hitEffectAnimated = true;
                }
                if (Time.time - lastDamageTime >= damageInterval)
                {
                    if (hit.CompareTag("Player"))
                    {
                        StatsManager.instance.TakeDamage(stats.Strength * attackMultiplier);
                        if (hit.TryGetComponent<PlayerMovement>(out var playerMovement))
                        {
                            playerMovement.KnockBack(transform, stats.KnockbackForce, stats.KnockbackTime, stats.StunTime);
                        }
                    }
                    else if (hit.CompareTag("NPC"))
                    {
                        if (hit.TryGetComponent<NPCCompanion>(out var npc))
                        {
                            npc.TakeDamage(stats.Strength * attackMultiplier, false);
                        }
                    }

                    lastDamageTime = Time.time;
                }
            }
        }
    }

    public void ChangeCollider()
    {
        normalCollider.enabled = !normalCollider.enabled;
        attackCollider.enabled = !attackCollider.enabled;
    }
}
