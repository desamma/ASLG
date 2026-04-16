 using System.Collections;
using UnityEngine;

public class Enemy_KaraWinterblade_ChromaticCold : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private StatusEffect buffEffect;

    [Header("Buff phase 1")]
    [SerializeField] private float healthRegenAmount = 200f;
    [SerializeField] private float speedMultiplier = 1.3f;
    [SerializeField] private float cooldownMultiplier = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip chromaticColdAudio;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private EnemyStats otherEnemyStats;
    private Enemy_KaraWinterblade_Health casterHealth = null;
    private Enemy_FaieBloodwing_Health otherFaieHealth = null;
    private StatusEffectManager statusFXManager;
    private StatusEffectManager otherStatusFXManager;

    public void Initialize(EnemyStats stats, Enemy_KaraWinterblade_Health health, StatusEffectManager manager)
    {
        casterStats = stats;
        casterHealth = health;
        statusFXManager = manager;
    }

    public void InitializeWithCrossBuff(EnemyStats stats, Enemy_KaraWinterblade_Health kHealth, StatusEffectManager manager,
        EnemyStats otherStats, Enemy_FaieBloodwing_Health otherHealth, StatusEffectManager otherManager)
    {
        casterStats = stats;
        casterHealth = kHealth;
        statusFXManager = manager;
        otherEnemyStats = otherStats;
        otherFaieHealth = otherHealth;
        otherStatusFXManager = otherManager;
    }

    private void Start()
    {
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    public void ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("ChromaticCold: Cannot apply buff - caster stats or health is null!");
            return;
        }

        // Calculate buff amounts based on current stats
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);
        float cooldownReduction = casterStats.AttackCooldown * (cooldownMultiplier - 1f);

        // Apply the buff
        casterStats.AttackCooldown += cooldownReduction;
        casterStats.Speed += speedIncrease;

        casterHealth.ChangeHealth(healthRegenAmount);

        if (statusFXManager != null)
        {
            statusFXManager.ApplyEffect(buffEffect, false, duration: buffDuration);
        }

        casterHealth.StartCoroutine(RemoveBuffAfterDurationPhase1(
            cooldownReduction,
            speedIncrease,
            buffDuration));

        // Apply cross-buff to the other enemy (Faie) if available
        if (otherEnemyStats != null && otherFaieHealth != null)
        {
            ApplyCrossBuff();
        }
    }

    private void ApplyCrossBuff()
    {
        // Calculate cross-buff amounts for Faie based on Kara's stats
        float faieSpeedIncrease = otherEnemyStats.Speed * (speedMultiplier - 1f) * 0.5f;  // 50% of speed multiplier effect
        float faieCooldownReduction = otherEnemyStats.AttackCooldown * (cooldownMultiplier - 1f) * 0.5f;  // 50% of cooldown reduction

        // Apply cross-buff to Faie
        otherEnemyStats.Speed += faieSpeedIncrease;
        otherEnemyStats.AttackCooldown += faieCooldownReduction;

        // Also give Faie partial health regen
        float faieHealthRegen = healthRegenAmount * 0.5f;  // 50% of Kara's health regen
        otherFaieHealth.ChangeHealth(faieHealthRegen);

        if (otherStatusFXManager != null)
        {
            otherStatusFXManager.ApplyEffect(buffEffect, false, duration: buffDuration);
        }

        otherFaieHealth.StartCoroutine(RemoveBuffAfterDurationCrossBuff(
            faieCooldownReduction,
            faieSpeedIncrease,
            buffDuration));
    }

    private IEnumerator RemoveBuffAfterDurationPhase1(
        float cooldownReduction,
        float speedIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && casterHealth != null)
        {
            casterStats.AttackCooldown -= cooldownReduction;
            casterStats.Speed -= speedIncrease;
        }
    }

    private IEnumerator RemoveBuffAfterDurationCrossBuff(
        float cooldownReduction,
        float speedIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (otherEnemyStats != null && otherFaieHealth != null)
        {
            otherEnemyStats.AttackCooldown -= cooldownReduction;
            otherEnemyStats.Speed -= speedIncrease;
        }
    }
    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 0:
                if (explosionAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(explosionAudio, transform, volume);
                break;
            case 1:
                if (chromaticColdAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(chromaticColdAudio, transform, volume);
                break;
        }
    }
}
