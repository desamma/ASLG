﻿using System.Collections;
using UnityEngine;

public class Enemy_ArgeonHighmayneMK2_HeavenlyStrike : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private float setInactiveTime;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamageMultiplier = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector3 attackHitBox;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private GameObject attackHitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 0.2f;

    private EnemyStats casterStats;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
    }

    public void HeavenlyStrike()
    {
        bool hitPlayer = false;
        var hits = Physics2D.OverlapBoxAll(centerPoint.position, attackHitBox, 0f, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if ((hit.CompareTag("Player") || hit.CompareTag("NPC")) && !hitPlayer)
                {
                    if (attackHitEffect != null)
                        Instantiate(attackHitEffect, hit.transform.position, Quaternion.identity);
                    
                    float damage = casterStats.Magic * attackDamageMultiplier;
                    
                    if (hit.CompareTag("Player"))
                    {
                        StatsManager.instance.TakeDamage(damage);
                    }
                    else if (hit.CompareTag("NPC"))
                    {
                        if (hit.TryGetComponent<NPCCompanion>(out var npc))
                        {
                            npc.TakeDamage(damage, false);
                        }
                    }
                    hitPlayer = true;
                }
            }
        }
        SoundFXManager.Instance.PlaySoundFXClip(audioClip, transform, volume);
    }

    private void OnEnable()
    {
        StartCoroutine(SetInactiveAfterDelay());
    }

    private IEnumerator SetInactiveAfterDelay()
    {
        yield return new WaitForSeconds(setInactiveTime);
        gameObject.SetActive(false);
    }
}