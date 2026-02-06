using UnityEngine;

public class Enemy_ArgeonHighmayne_WarSurge : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float spellMagicDamageMultiplier = 1.2f;
    [SerializeField] private float spellPhysicalDamageMultiplier = 1.2f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip chantSound;
    [SerializeField] private AudioClip swordClash;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float volume = 1f;

    private bool hitPlayer = false;
    private EnemyStats casterStats;
    private bool isInitialized = false;

    /// <summary>
    /// Initialize the spell with the caster's stats
    /// </summary>
    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
        isInitialized = true;
    }

    private void Start()
    {
        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        Destroy(gameObject, destroyTime);
    }

    public void PlayChantSound()
    {
        if (chantSound != null)
            SoundFXManager.Instance.PlaySoundFXClip(chantSound, transform, volume);
    }

    public void PlaySwordClash()
    {
        if (swordClash != null)
            SoundFXManager.Instance.PlaySoundFXClip(swordClash, transform, volume);
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

    private float CalculateMagicDamage()
    {
        if (!isInitialized || casterStats == null)
        {
            Debug.LogWarning("WarSurge: No caster stats found, using default damage");
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
            Debug.LogWarning("WarSurge: No caster stats found, using default damage");
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
                Instantiate(hitEffect, collision.transform.position, Quaternion.identity, collision.transform);
            }

            // Play hit sound attached to the player so it follows them
            if (hitSound != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(hitSound, collision.transform, volume);
            }

            // Calculate damage dynamically based on caster's current stats
            float totalMagicDamage = CalculateMagicDamage();
            float totalPhysicalDamage = CalculatePhysicalDamage();

            Debug.Log($"WarSurge: Dealt {totalMagicDamage} magic damage and {totalPhysicalDamage} physical damage");

            //var playerHealth = collision.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.ChangeHealth(-totalMagicDamage);
            //    playerHealth.ChangeHealth(-totalPhysicalDamage);
            //}
        }
    }
}
