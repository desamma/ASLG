using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack logic for Andromeda enemy.
/// Attack performs 360° AOE damage around the enemy.
/// Cast is a special ranged spell attack.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_Andromeda_Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;

    [Header("360 AOE Attack")]
    [SerializeField] private float aoeRadius = 3f;

    [Header("Spell Cast")]
    [SerializeField] private GameObject spellPrefab;
    [SerializeField] private List<Transform> players;

    [Header("Audio")]
    [SerializeField] private AudioClip aoeAttackAudioClip;
    [SerializeField] private AudioClip castAudioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats stats;

    private void Start()
    {
        stats = GetComponent<Enemy_Andromeda_Health>().stats;

        if (players == null || players.Count == 0)
        {
            players = new List<Transform>();
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            
            if (playerObj != null)
            {
                players.Add(playerObj.transform);
            }
        }

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        if (spellPrefab == null)
        {
            Debug.LogWarning("Spell prefab not assigned for Andromeda enemy!");
        }
    }

    /// <summary>
    /// Performs a 360° AOE attack around the enemy's position
    /// Called by animation event
    /// </summary>
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
                // TODO: Hook into player damage system
                // Apply physical damage based on stats.Strength
                Debug.Log($"Andromeda AOE hit player: {hit.name}");
            }
        }
    }

    /// <summary>
    /// Casts a spell at the player's location
    /// Called by animation event
    /// </summary>
    public void CastSpell()
    {
        if (players == null || players.Count == 0)
        {
            return;
        }

        if (spellPrefab == null)
        {
            Debug.LogWarning("Cannot cast spell - spell prefab is null!");
            return;
        }

        foreach (var player in players)
        {
            if (player == null) continue;

            // Spawn spell at player's exact position
            var spawnPosition = new Vector3(
                player.position.x, 
                player.position.y, 
                player.position.z
            );

            if (castAudioClip != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(castAudioClip, transform, volume);
            }

            Instantiate(spellPrefab, spawnPosition, Quaternion.identity);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw 360 AOE attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);

        // Draw cast detection range
        if (stats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.AttackRange * 2.5f);
        }
    }
}
