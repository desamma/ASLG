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
    [SerializeField] private List<Transform> players;
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

    private void Start()
    {
        stats = GetComponent<Enemy_BringerOfDeath_Health>().stats;

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
        attackBoxSize = new Vector2(4, 3);
    }

    public void MeleeAttack()
    {
        bool audioPlayed = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") && !audioPlayed)
            {
                SoundFXManager.Instance.PlaySoundFXClip(meleeAttackHitAudioClip, transform, volume);
                // TODO: Hook into player damage system
                // Apply physical damage based on stats.Strength
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
        if (players == null || players.Count == 0)
        {
            return;
        }

        foreach (var player in players)
        {
            float xOffset = player.position.x < transform.position.x ? -2.7f : 2.7f;
            float yOffset = -0.8f;
            var spawnPosition = new Vector3(player.position.x + xOffset, player.position.y + yOffset, player.position.z);

            SoundFXManager.Instance.PlaySoundFXClip(CastAudioClip, transform, volume);
            Instantiate(spellPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
