using System.Collections;
using UnityEngine;

public class Enemy_ArgeonHighmayne_SunBloom : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 2f;
    [SerializeField] private float strengthIncreaseMultiplier = 1.2f;
    [SerializeField] private float magicIncreaseMultiplier = 1.2f;
    [SerializeField] private float buffDuration = 10f;

    [Header("Components")]
    [SerializeField] private GameObject buffEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private Enemy_ArgeonHighmayne_Health casterHealth;

    public void Initialize(EnemyStats stats, Enemy_ArgeonHighmayne_Health health)
    {
        casterStats = stats;
        casterHealth = health;
    }

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    public void PlayAudioClip()
    {
        if (audioClip != null)
        {
            SoundFXManager.Instance.PlaySoundFXClip(audioClip, transform, volume);
        }
    }

    public void ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("Sun Bloom: Cannot apply buff - caster stats or health is null!");
            return;
        }

        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthIncreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicIncreaseMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;

        casterHealth.StartCoroutine(RemoveBuffAfterDuration(strengthIncrease, magicIncrease, buffDuration));
    }

    private IEnumerator RemoveBuffAfterDuration(float strengthIncrease, float magicIncrease, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && casterHealth != null)
        {
            casterStats.Strength -= strengthIncrease;
            casterStats.Magic -= magicIncrease;
        }
    }
}