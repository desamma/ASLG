using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayne_Decimate : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float spellMagicDamageMultiplier = 1.2f;
    [SerializeField] private float spellPhysicalDamageMultiplier = 1.2f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;

    [Header("Effect Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private AH_Decimate_State state;
    private StateManager<AH_Decimate_State> stateManager;

    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 1f;

    private bool hitPlayer = false;
    private EnemyStats casterStats;
    private bool isInitialized = false;

    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
        isInitialized = true;
    }

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        stateManager = new StateManager<AH_Decimate_State>(animator, AH_Decimate_State.Empty);

        StartCoroutine(AnimationDelay());

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    public void PlayAudio()
    {
        if (audioClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(audioClip, transform, volume);
    }

    public void EnableTrigger()
    {
        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = true;
        }

        Destroy(gameObject, destroyTime);
    }

    private IEnumerator AnimationDelay()
    {
        var delayTime = GetDelayTime(state);
        yield return new WaitForSeconds(delayTime);
        stateManager.ChangeState(state);
    }

    private float GetDelayTime(AH_Decimate_State state)
    {
        return state switch
        {
            AH_Decimate_State.Empty => 0f,
            AH_Decimate_State.Decimate => 0f,
            AH_Decimate_State.EnergyHaloGround => 0.4f,
            AH_Decimate_State.Disintegrate => 0.8f,
            AH_Decimate_State.Swirl => 0.8f,
            _ => 0f,
        };
    }

    private float CalculateMagicDamage()
    {
        if (!isInitialized || casterStats == null)
        {
            Debug.LogWarning("Decimate: No caster stats found, using default damage");
            return 50f;
        }

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        float magicDamage = casterStats.Magic * spellMagicDamageMultiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

        return magicDamage;
    }

    private float CalculatePhysicalDamage()
    {
        if (!isInitialized || casterStats == null)
        {
            Debug.LogWarning("Decimate: No caster stats found, using default damage");
            return 50f;
        }

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        float physicalDamage = casterStats.Strength * spellPhysicalDamageMultiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);

        return physicalDamage;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (collision.CompareTag("Player") && !hitPlayer)
        {
            hitPlayer = true;

            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, collision.transform.position, Quaternion.identity);
                effect.transform.localScale = new Vector3(2f, 2f, 1f);
            }

            float totalMagicDamage = CalculateMagicDamage();
            float totalPhysicalDamage = CalculatePhysicalDamage();

            var totalDamage = totalMagicDamage + totalPhysicalDamage;

            StatsManager.instance.TakeDamage(totalDamage);
        }
    }
}
