using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Andromeda_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;

    [Header("360 AOE Attack")]
    [SerializeField] private float aoeRadius = 3f;

    [Header("Spell Cast")]
    [SerializeField] private GameObject spellPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip aoeAttackAudioClip;
    [SerializeField] private AudioClip castAudioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats stats;
    private Enemy_Andromeda_Movement movement;

    private void Start()
    {
        stats = GetComponent<Enemy_Andromeda_Health>().stats;
        movement = GetComponent<Enemy_Andromeda_Movement>();

        if (spellPrefab == null)
        {
            Debug.LogWarning("Spell prefab not assigned for Andromeda enemy!");
        }
    }

    public void AOEAttack()
    {
        if (aoeAttackAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(aoeAttackAudioClip, transform, volume);
        }

        // Check for all players in a circle around the enemy
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                StatsManager.instance.TakeDamage(stats.Magic);

                if (hit.TryGetComponent<PlayerMovement>(out var playerMovement))
                {
                    playerMovement.KnockBack(transform, stats.KnockbackForce, stats.KnockbackTime, stats.StunTime);
                }
            }
            else if (hit.CompareTag("NPC"))
            {
                if (hit.TryGetComponent<NPCCompanion>(out var npc))
                {
                    npc.TakeDamage(stats.Magic, false);
                }
            }
        }
    }

    public void CastSpell()
    {
        Transform target = movement.PlayerTransform;
        if (target == null) return;

        if (spellPrefab == null)
        {
            Debug.LogWarning("Cannot cast spell - spell prefab is null!");
            return;
        }

        var spawnPosition = new Vector3(
            target.position.x, 
            target.position.y, 
            target.position.z
        );

        if (castAudioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(castAudioClip, transform, volume);
        }

        Instantiate(spellPrefab, spawnPosition, Quaternion.identity);
    }
}
