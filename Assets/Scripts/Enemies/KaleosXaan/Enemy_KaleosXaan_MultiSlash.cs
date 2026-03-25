using UnityEngine;
using System.Collections;
using System;

public class Enemy_KaleosXaan_MultiSlash : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float disableTime = 3f;
    [SerializeField] private float enableColliderTime = 0.2f;
    [SerializeField] private BoxCollider2D spellCollider;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private AudioClip slashSound;
    [SerializeField] private float volume = 1f;

    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 0.3f;
    [SerializeField] private float damageInterval = 0.2f;
    private float timer = 0f;
    private bool playerInside = false;
    private bool hitEffectPlay;
    private EnemyStats casterStats;
    private int damageStack = 0;
    private int stackIncreaseEachHit = 0;
    private Coroutine disableCoroutine;
    private Action<int> onDamageStackChanged;

    public void Initialize(EnemyStats stats, int initialDamageStack, int stackIncreaseEachHit, Action<int> onDamageStackChanged)
    {
        casterStats = stats;
        damageStack = initialDamageStack;
        this.stackIncreaseEachHit = stackIncreaseEachHit;
        this.onDamageStackChanged = onDamageStackChanged;
    }

    private void OnEnable()
    {
        hitEffectPlay = true;
        StartCoroutine(EnableColliderCoroutine());
        if (disableCoroutine != null) StopCoroutine(disableCoroutine);
        disableCoroutine = StartCoroutine(DisableSpellRoutine());
        timer = damageInterval;
        playerInside = false;
    }

    private IEnumerator EnableColliderCoroutine()
    {
        SoundFXManager.Instance.PlaySoundFXClip(slashSound, transform, volume);
        yield return new WaitForSeconds(enableColliderTime);
        if (spellCollider != null)
        {
            spellCollider.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
        spellCollider.enabled = false;
        hitEffectPlay = true;
    }

    private void Update()
    {
        if (playerInside)
        {
            timer += Time.deltaTime;
            if (timer >= damageInterval)
            {
                DealDamage();
                timer = 0f;
            }
        }
    }

    private IEnumerator DisableSpellRoutine()
    {
        yield return new WaitForSeconds(disableTime);
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = true;
            if (hitEffectPlay)
            {
                Instantiate(hitEffect, collision.transform.position, Quaternion.identity);
                hitEffectPlay = false;
            }
            timer = damageInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = false;
        }
    }

    private void DealDamage()
    {
        float damage = 50f * damageMultiplier;
        if (casterStats != null)
        {
            var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
            damage = (casterStats.Strength + damageStack) * damageMultiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);
            damageStack += stackIncreaseEachHit;
            onDamageStackChanged?.Invoke(damageStack);
        }

        if (StatsManager.instance != null)
        {
            StatsManager.instance.TakeDamage(damage);
        }
    }
}
