using System.Collections;
using UnityEngine;

public class Enemy_ArgeonHighmayne_DualCast : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5f;
    [SerializeField] private float strengthIncreaseMultiplier = 1.3f;
    [SerializeField] private float magicIncreaseMultiplier = 1.3f;
    [SerializeField] private float defenseIncreaseMultiplier = 1.5f;
    [SerializeField] private float magicResistIncreaseMultiplier = 1.5f;

    [SerializeField] private float damageReductionPercentage = 0.4f;
    [SerializeField] private float buffDuration = 20f;

    [Header("Effect Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private AH_DualCast_State state;
    private StateManager<AH_DualCast_State> stateManager;

    [Header("Audio")]
    [SerializeField] private AudioClip sunBloomAudio;
    [SerializeField] private AudioClip aurynAudio;
    [SerializeField] private float volume = 0.7f;

    private EnemyStats casterStats;
    private Enemy_ArgeonHighmayneMK2_Health casterHealth;

    public void Initialize(EnemyStats stats, Enemy_ArgeonHighmayneMK2_Health health)
    {
        casterStats = stats;
        casterHealth = health;
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        stateManager = new StateManager<AH_DualCast_State>(animator, AH_DualCast_State.Empty);

        StartCoroutine(AnimationDelay());

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private IEnumerator AnimationDelay()
    {
        var delayTime = GetDelayTime(state);
        yield return new WaitForSeconds(delayTime);
        stateManager.ChangeState(state);
    }

    private float GetDelayTime(AH_DualCast_State state)
    {
        return state switch
        {
            AH_DualCast_State.AurynNexus => 0f,
            AH_DualCast_State.Smoke => 0.5f,
            AH_DualCast_State.BladeStorm => 0.85f,
            AH_DualCast_State.SunBloom => 0.2f,
            _ => 0f,
        };
    }

    public void PlayAudioClip(int num)
    {
        if (sunBloomAudio == null || aurynAudio == null) return;    
        if(num == 0)
            SoundFXManager.Instance.PlaySoundFXClip(sunBloomAudio, transform, volume);
        else if(num == 1)
            SoundFXManager.Instance.PlaySoundFXClip(aurynAudio, transform, volume);
    }

    public void ApplyBuff()
    {
        if (casterStats == null || casterHealth == null)
        {
            Debug.LogError("AurynNexus: Cannot apply buff - caster stats or health is null!");
            return;
        }

        // Calculate buff amounts based on current stats
        float strengthIncrease = casterStats.Strength * (strengthIncreaseMultiplier - 1f);
        float magicIncrease = casterStats.Magic * (magicIncreaseMultiplier - 1f);
        float defenseIncrease = casterStats.Defense * (defenseIncreaseMultiplier - 1f);
        float magicResistIncrease = casterStats.MagicResist * (magicResistIncreaseMultiplier - 1f);

        // Apply the buff
        casterStats.Strength += strengthIncrease;
        casterStats.Magic += magicIncrease;
        casterStats.Defense += defenseIncrease;
        casterStats.MagicResist += magicResistIncrease;
        casterHealth.damageReductionPercentage = damageReductionPercentage;

        casterHealth.StartCoroutine(RemoveBuffAfterDuration(strengthIncrease, magicIncrease, defenseIncrease, magicResistIncrease, buffDuration));
    }

    private IEnumerator RemoveBuffAfterDuration(float strengthIncrease, float magicIncrease, float defenseIncrease, float magicResistIncrease, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (casterStats != null && casterHealth != null)
        {
            casterStats.Strength -= strengthIncrease;
            casterStats.Magic -= magicIncrease;
            casterStats.Defense -= defenseIncrease;
            casterStats.MagicResist -= magicResistIncrease;
            casterHealth.damageReductionPercentage = 0f;
        }
    }
}