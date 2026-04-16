using UnityEngine;

public class Enemy_KaraWinterbladeMK2_Cryogenesis : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 3f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip fireAudio;
    [SerializeField] private float volume = 0.8f;

    private EnemyStats casterStats;
    private bool hitPlayer = false;

    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
    } 

    private void Start()
    {
        if (fireAudio != null)
            SoundFXManager.Instance.PlaySoundFXClip(fireAudio, transform, volume);

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitPlayer) return;
        if (collision.CompareTag("Player"))
        {
            var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;

            float damage = casterStats.Magic * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

            StatsManager.instance.TakeDamage(damage);
            hitPlayer = true;
            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
    }

    public void ChangeCollider()
    {
        spellCollider.enabled = !spellCollider.enabled;
    }
} 
