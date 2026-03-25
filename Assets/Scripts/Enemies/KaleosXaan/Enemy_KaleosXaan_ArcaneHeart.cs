using System.Collections;
using UnityEngine;

public class Enemy_KaleosXaan_ArcaneHeart : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float buffDuration = 20f;
    [SerializeField] private float maxHealthIncreaseAmount = 200f;
    [SerializeField] private float healthRegenAmount = 150f;
    [SerializeField] private float defenseIncreaseMultiplier = 1.5f;
    [SerializeField] private float magicResistIncreaseMultiplier = 1.3f;
    [SerializeField] private float strengthDecreaseMultiplier = 0.8f;
    [SerializeField] private float magicDecreaseMultiplier = 0.5f;
    [SerializeField] private float speedMultiplier = 0.85f;
    [SerializeField] private float attackCooldownMultiplier = 1.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip arcaneHeartAudio;
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

        if(casterPhase2Health != null)
            StartCoroutine(ApplyBuffPhase2());

        else if (casterHealth != null)
            StartCoroutine(ApplyBuff());
    }

    public IEnumerator ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("ArcaneHeart: Cannot apply buff - caster stats or health is null!");
            yield break;
        }

        yield return new WaitForSeconds(1.5f);

        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthDecreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicDecreaseMultiplier - 1f);
        float defenseIncrease = casterStats.Defense * (defenseIncreaseMultiplier - 1f);
        float magicResistIncrease = casterStats.MagicResist * (magicResistIncreaseMultiplier - 1f);
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);
        float attackCooldownIncrease = casterStats.AttackCooldown * (attackCooldownMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterStats.Speed += speedIncrease;
        casterStats.AttackCooldown += attackCooldownIncrease;

        // Apply max health increase
        casterStats.MaxHP += maxHealthIncreaseAmount;

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
            casterStats.MaxHP -= maxHealthIncreaseAmount;

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
            Debug.LogError("ArcaneHeart: Cannot apply buff - caster stats or health is null!");
            yield break;
        }
        yield return new WaitForSeconds(1.5f);
        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthDecreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicDecreaseMultiplier - 1f);
        float defenseIncrease = casterStats.Defense * (defenseIncreaseMultiplier - 1f);
        float magicResistIncrease = casterStats.MagicResist * (magicResistIncreaseMultiplier - 1f);
        float speedIncrease = casterStats.Speed * (speedMultiplier - 1f);
        float attackCooldownIncrease = casterStats.AttackCooldown * (attackCooldownMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterStats.Speed += speedIncrease;
        casterStats.AttackCooldown += attackCooldownIncrease;

        // Apply max health increase
        casterStats.MaxHP += maxHealthIncreaseAmount;

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
            casterStats.MaxHP -= maxHealthIncreaseAmount;

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
                if (arcaneHeartAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(arcaneHeartAudio, transform, volume);
                break;
        }
    }
}
