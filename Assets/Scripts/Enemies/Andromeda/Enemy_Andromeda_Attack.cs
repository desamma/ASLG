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
    [SerializeField] private List<Transform> players;

    [Header("Audio")]
    [SerializeField] private AudioClip aoeAttackAudioClip;
    [SerializeField] private AudioClip castAudioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats stats;
    private GameObject player;

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
                player = hit.gameObject;
                StatsManager.instance.TakeDamage(stats.Magic);

                player.TryGetComponent<PlayerMovement>(out var playerMovement);

                if (playerMovement != null)
                {
                    playerMovement.KnockBack(transform, stats.KnockbackForce, stats.KnockbackTime, stats.StunTime);
                }
            }
        }
    }

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
}
