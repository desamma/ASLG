using System.Collections;
using UnityEngine;

public class Enemy_KaleosXaan_BlinkEnhance : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float buffDuration = 20f;
    [SerializeField] private float healthRegenAmount = 50f;
    [SerializeField] private float defenseDecreaseMultiplier = 0.75f;
    [SerializeField] private float magicResistDecreaseMultiplier = 0.65f;
    [SerializeField] private float strengthIncreaseMultiplier = 1.3f;
    [SerializeField] private float magicIncreaseMultiplier = 1.2f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float attackCooldownMultiplier = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioClip blinkEnhanceAudio;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private Enemy_KaleosXaan_Health casterHealth = null;
    private Enemy_KaleosXaanMK2_Health casterPhase2Health = null;

    public void Initialize(EnemyStats stats, Enemy_KaleosXaan_Health health)
    {
        casterStats = stats;
        casterHealth = health;
    }

    public void Initialize(EnemyStats stats, Enemy_KaleosXaanMK2_Health health)
    {
        casterStats = stats;
        casterPhase2Health = health;
    }

    private void Start()
    {
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
            
        if (casterPhase2Health != null)
            StartCoroutine(ApplyBuffPhase2());
        else if (casterHealth != null)
            StartCoroutine(ApplyBuff());
    }

    public IEnumerator ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("BlinkEnhance: Cannot apply buff - caster stats or health is null!");
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthIncreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicIncreaseMultiplier - 1f);
        float defenseIncrease = casterStats.Defense * (defenseDecreaseMultiplier - 1f);
        float magicResistIncrease = casterStats.MagicResist * (magicResistDecreaseMultiplier - 1f);
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);
        float attackCooldownIncrease = casterStats.AttackCooldown * (attackCooldownMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterStats.Speed += speedIncrease;
        casterStats.AttackCooldown += attackCooldownIncrease;

        // Heal the caster
        casterHealth.ChangeHealth(healthRegenAmount);

        casterHealth.StartCoroutine(RemoveBuffAfterDurationPhase1(
            strengthIncrease,
            magicIncrease,
            defenseIncrease,
            magicResistIncrease,
            speedIncrease,
            attackCooldownIncrease,
            buffDuration));
    }

    private IEnumerator RemoveBuffAfterDurationPhase1(
        float strengthIncrease,
        float magicIncrease,
        float defenseIncrease,
        float magicResistIncrease,
        float speedIncrease,
        float attackCooldownIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && casterHealth != null)
        {
            casterStats.Strength -= strengthIncrease;
            casterStats.Magic -= magicIncrease;
            casterStats.Defense -= defenseIncrease;
            casterStats.MagicResist -= magicResistIncrease;
            casterStats.Speed -= speedIncrease;
            casterStats.AttackCooldown -= attackCooldownIncrease;

            // Ensure current health doesn't exceed new max health
            if (casterStats.CurrentHP > casterStats.MaxHP)
            {
                casterStats.CurrentHP = casterStats.MaxHP;
            }
        }
    }

    public IEnumerator ApplyBuffPhase2()
    {
        if (casterStats == null || casterPhase2Health == null)
        {
            Debug.LogError("BlinkEnhance: Cannot apply buff - caster stats or health is null!");
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthIncreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicIncreaseMultiplier - 1f);
        float defenseIncrease = casterStats.Defense * (defenseDecreaseMultiplier - 1f);
        float magicResistIncrease = casterStats.MagicResist * (magicResistDecreaseMultiplier - 1f);
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);
        float attackCooldownIncrease = casterStats.AttackCooldown * (attackCooldownMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterStats.Speed += speedIncrease;
        casterStats.AttackCooldown += attackCooldownIncrease;

        // Heal the caster
        casterPhase2Health.ChangeHealth(healthRegenAmount);

        casterPhase2Health.StartCoroutine(RemoveBuffAfterDurationPhase2(
            strengthIncrease,
            magicIncrease,
            defenseIncrease,
            magicResistIncrease,
            speedIncrease,
            attackCooldownIncrease,
            buffDuration));
    }

    private IEnumerator RemoveBuffAfterDurationPhase2(
        float strengthIncrease,
        float magicIncrease,
        float defenseIncrease,
        float magicResistIncrease,
        float speedIncrease,
        float attackCooldownIncrease,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && casterPhase2Health != null)
        {
            casterStats.Strength -= strengthIncrease;
            casterStats.Magic -= magicIncrease;
            casterStats.Defense -= defenseIncrease;
            casterStats.MagicResist -= magicResistIncrease;
            casterStats.Speed -= speedIncrease;
            casterStats.AttackCooldown -= attackCooldownIncrease;

            // Ensure current health doesn't exceed new max health
            if (casterStats.CurrentHP > casterStats.MaxHP)
            {
                casterStats.CurrentHP = casterStats.MaxHP;
            }
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
                if (blinkEnhanceAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(blinkEnhanceAudio, transform, volume);
                break;
        }
    }
}
