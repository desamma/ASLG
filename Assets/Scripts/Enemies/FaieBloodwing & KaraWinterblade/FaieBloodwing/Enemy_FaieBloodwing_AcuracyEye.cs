using System.Collections;
using UnityEngine;

public class Enemy_FaieBloodwing_AccuracyEye : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private StatusEffect buffEffect;

    [Header("Buff phase 1")]
    [SerializeField] private float magicMultiplier = 1.3f;
    [SerializeField] private float defenseIncrease = 100f;
    [SerializeField] private float magicResistIncrease = 150f;
    [SerializeField] private float speedMultiplier = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip accurateEyeAudio;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private EnemyStats otherEnemyStats;
    private Enemy_FaieBloodwing_Health casterHealth = null;
    private Enemy_KaraWinterblade_Health karaHealth = null;
    private Enemy_KaraWinterblade_Health otherKaraHealth = null;
    private Enemy_FaieBloodwingMK2_Health casterPhase2Health = null;
    private StatusEffectManager statusFXManager;
    private StatusEffectManager otherStatusFXManager;

    public void Initialize(EnemyStats stats, Enemy_FaieBloodwing_Health health, StatusEffectManager manager)
    {
        casterStats = stats;
        casterHealth = health;
        statusFXManager = manager;
    }

    public void Initialize(EnemyStats stats, Enemy_FaieBloodwingMK2_Health health, StatusEffectManager manager)
    {
        casterStats = stats;
        casterPhase2Health = health;
        statusFXManager = manager;
    }

    public void InitializeWithCrossBuff(EnemyStats stats, Enemy_FaieBloodwing_Health fHealth, StatusEffectManager manager,
        EnemyStats otherStats, Enemy_KaraWinterblade_Health otherHealth, StatusEffectManager otherManager)
    {
        casterStats = stats;
        casterHealth = fHealth;
        statusFXManager = manager;
        otherEnemyStats = otherStats;
        otherKaraHealth = otherHealth;
        otherStatusFXManager = otherManager;
    }

    private void Start()
    {
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);

    }

    public void ApplyBuff()
    {
        if (casterStats == null || (casterHealth == null && karaHealth == null && casterPhase2Health == null))
        {
            Debug.LogError("AcuracyEye: Cannot apply buff - caster stats or health is null!");
            return;
        }

        ApplyBuffInternal();
    }

    private void ApplyCrossBuff()
    {
        // Calculate cross-buff amounts for Kara based on Faie's stats
        float karaDefenseIncrease = defenseIncrease * 0.6f;  // 60% of Faie's defense buff
        float karaMagicResistIncrease = magicResistIncrease * 0.6f;  // 60% of Faie's magic resist buff
        float karaSpeedIncrease = otherEnemyStats.Speed * (speedMultiplier - 1f) * 0.5f;  // 50% of speed multiplier effect

        // Apply cross-buff to Kara
        otherEnemyStats.Defense += karaDefenseIncrease;
        otherEnemyStats.MagicResist += karaMagicResistIncrease;
        otherEnemyStats.Speed += karaSpeedIncrease;

        if (otherStatusFXManager != null)
        {
            otherStatusFXManager.ApplyEffect(buffEffect, false, duration: buffDuration);
        }

        otherKaraHealth.StartCoroutine(RemoveBuffAfterDurationCrossBuff(
            karaDefenseIncrease,
            karaMagicResistIncrease,
            karaSpeedIncrease,
            buffDuration));
    }

    private IEnumerator RemoveBuffAfterDurationPhase1(
        float magicIncrease,
        float defenseIncrease,
        float magicResistIncrease,
        float speedIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && (casterHealth != null || karaHealth != null || casterPhase2Health != null))
        {
            casterStats.Magic -= magicIncrease;
            casterStats.Defense -= defenseIncrease;
            casterStats.MagicResist -= magicResistIncrease;
            casterStats.Speed -= speedIncrease;
        }
    }

    private IEnumerator RemoveBuffAfterDurationCrossBuff(
        float defenseIncrease,
        float magicResistIncrease,
        float speedIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (otherEnemyStats != null && otherKaraHealth != null)
        {
            otherEnemyStats.Defense -= defenseIncrease;
            otherEnemyStats.MagicResist -= magicResistIncrease;
            otherEnemyStats.Speed -= speedIncrease;
        }
    }

    private void ApplyBuffInternal()
    {
        // Calculate buff amounts based on current stats
        float magicIncrease = casterStats.Magic * (magicMultiplier - 1f);
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);

        // Apply the buff
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterStats.Speed += speedIncrease;

        statusFXManager.ApplyEffect(buffEffect, false, duration: buffDuration);

        // Start coroutine to remove buff using the appropriate health component
        if (casterHealth != null)
        {
            casterHealth.StartCoroutine(RemoveBuffAfterDurationPhase1(
                magicIncrease,
                defenseIncrease,
                magicResistIncrease,
                speedIncrease,
                buffDuration));
        }
        else if (karaHealth != null)
        {
            karaHealth.StartCoroutine(RemoveBuffAfterDurationPhase1(
                magicIncrease,
                defenseIncrease,
                magicResistIncrease,
                speedIncrease,
                buffDuration));
        }
        else if (casterPhase2Health != null)
        {
            casterPhase2Health.StartCoroutine(RemoveBuffAfterDurationPhase1(
                magicIncrease,
                defenseIncrease,
                magicResistIncrease,
                speedIncrease,
                buffDuration));
        }

        // Apply cross-buff to the other enemy (Kara) if available
        if (otherEnemyStats != null && otherKaraHealth != null)
        {
            ApplyCrossBuff();
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
                if (accurateEyeAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(accurateEyeAudio, transform, volume);
                break;
        }
    }
}
