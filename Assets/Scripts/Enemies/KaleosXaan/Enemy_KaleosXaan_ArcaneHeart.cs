using System.Collections;
using UnityEngine;

public class Enemy_KaleosXaan_ArcaneHeart : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float buffDuration = 20f;
    [SerializeField] private float maxHealthIncreaseAmount = 150f;
    [SerializeField] private float healthRegenAmount = 100f;
    [SerializeField] private float defenseIncreaseMultiplier = 1.3f;
    [SerializeField] private float magicResistIncreaseMultiplier = 1.3f;
    [SerializeField] private float strengthDecreaseMultiplier = 0.9f;
    [SerializeField] private float magicDecreaseMultiplier = 0.8f;
    [SerializeField] private float speedMultiplier = 0.7f;
    [SerializeField] private float attackCooldownMultiplier = 1.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip arcaneHeartAudio;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private Enemy_KaleosXaan_Health casterHealth;
    public void Initialize(EnemyStats stats, Enemy_KaleosXaan_Health health)
    {
        casterStats = stats;
        casterHealth = health;
    }

    private void Start()
    {
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
        ApplyBuff();
    }

    public void ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("ArcaneHeart: Cannot apply buff - caster stats or health is null!");
            return;
        }

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

        casterHealth.StartCoroutine(RemoveBuffAfterDuration(
            strengthIncrease,
            magicIncrease,
            defenseIncrease,
            magicResistIncrease,
            speedIncrease,
            attackCooldownIncrease,
            buffDuration));
    }

    private IEnumerator RemoveBuffAfterDuration(
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
