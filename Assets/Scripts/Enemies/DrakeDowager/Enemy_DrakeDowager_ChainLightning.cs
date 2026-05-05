﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_DrakeDowager_ChainLightning : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 2f;
    [SerializeField] private float warningDelay = 0.5f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;

    private List<Collider2D> hitTargets = new List<Collider2D>();
    private List<Collider2D> overlapResults = new List<Collider2D>();
    private EnemyStats casterStats;
    private Vector2 direction;

    public void Initialize(EnemyStats stats, Vector2 targetDirection)
    {
        casterStats = stats;
        direction = targetDirection.normalized;

        // Rotate arrow to face the direction of movement
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    private void Start()
    {
        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        // Disable collider initially
        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = false;
        }

        StartCoroutine(ActivationDelay());

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private IEnumerator ActivationDelay()
    {
        yield return new WaitForSeconds(warningDelay);
        
        if (spellCollider != null)
            spellCollider.enabled = true;
    }

    private void Update()
    {
        // Quét thủ công để vượt qua mọi giới hạn của bảng Layer Collision Matrix trong Unity
        if (spellCollider != null && spellCollider.enabled)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            filter.layerMask = Physics2D.AllLayers; // Cho phép quét mọi Layer!
            filter.useTriggers = true;

            Physics2D.OverlapCollider(spellCollider, filter, overlapResults);
            foreach (var hit in overlapResults)
            {
                OnTriggerEnter2D(hit);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if ((collision.CompareTag("Player") || collision.CompareTag("NPC")) && !hitTargets.Contains(collision))
        {
            hitTargets.Add(collision);

            var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;

            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, collision.transform.position, Quaternion.identity);
                effect.transform.localScale = new Vector3(2f, 2f, 1f);
            }

            // SỬA Ở ĐÂY: Tia sét phải dùng sát thương phép (Magic) thay vì sát thương vật lý (Strength)
            float damage = casterStats.Magic * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

            if (collision.CompareTag("Player"))
            {
                StatsManager.instance.TakeDamage(damage);
            }
            else if (collision.CompareTag("NPC"))
            {
                if (collision.TryGetComponent<NPCCompanion>(out var npc))
                {
                    npc.TakeDamage(damage, false);
                }
            }
        }
    }
}
