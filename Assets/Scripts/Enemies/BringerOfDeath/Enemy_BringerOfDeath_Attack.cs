using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack logic for Bringer of Death enemy.
/// Handles both melee attacks and spell casting.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_BringerOfDeath_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector2 attackBoxSize;

    [Header("Spell Cast")]
    [SerializeField] private GameObject spellPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip meleeAttackMissAudioClip;
    [SerializeField] private AudioClip meleeAttackHitAudioClip;
    [SerializeField] private AudioClip CastAudioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats stats;
    private Enemy_BringerOfDeath_Movement movement;

    private void Start()
    {
        stats = GetComponent<Enemy_BringerOfDeath_Health>().stats;
        movement = GetComponent<Enemy_BringerOfDeath_Movement>();
        attackBoxSize = new Vector2(4, 3);
    }

    public void MeleeAttack()
    {
        bool audioPlayed = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, playerLayer);

        foreach (var hit in hits)
        {
            if ((hit.CompareTag("Player") || hit.CompareTag("NPC")) && !audioPlayed)
            {
                SoundFXManager.Instance.PlaySoundFXClip(meleeAttackHitAudioClip, transform, volume);

                if (hit.CompareTag("Player"))
                {
                    StatsManager.instance.TakeDamage(stats.Strength);
                    if (hit.TryGetComponent<PlayerMovement>(out var playerMovement))
                    {
                        playerMovement.KnockBack(transform, stats.KnockbackForce, stats.KnockbackTime, stats.StunTime);
                    }
                }
                else if (hit.CompareTag("NPC"))
                {
                    if (hit.TryGetComponent<NPCCompanion>(out var npc))
                    {
                        npc.TakeDamage(stats.Strength, false);
                    }
                }

                audioPlayed = true;
            }
        }
        if (!audioPlayed)
        {
            SoundFXManager.Instance.PlaySoundFXClip(meleeAttackMissAudioClip, transform, volume);
        }
    }

    public void CastSpell()
    {
        Transform target = movement.PlayerTransform;
        if (target == null) return;

        float xOffset = target.position.x < transform.position.x ? -2.7f : 2.7f;
        float yOffset = -0.8f;
        var spawnPosition = new Vector3(target.position.x + xOffset, target.position.y + yOffset, target.position.z);

        SoundFXManager.Instance.PlaySoundFXClip(CastAudioClip, transform, volume);
        Instantiate(spellPrefab, spawnPosition, Quaternion.identity);
    }
}
