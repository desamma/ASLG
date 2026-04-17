using System.Collections;
using UnityEngine;

public class EnemyFaieBloodwingMK2_Warbird : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 3f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private float stunDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip birdAudio;
    [SerializeField] private AudioClip slamAudio;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    private bool hitPlayer = false;
    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
    }
    private void Start()
    {
        PlayAudio();

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
            if (collision.TryGetComponent<PlayerMovement>(out var playerMovement))
            {
                playerMovement.StopMovement(stunDuration);
            }
            hitPlayer = true;
        }
    }

    public void ChangeCollider()
    {
        spellCollider.enabled = !spellCollider.enabled;
    }

    private void PlayAudio()
    {
        if (birdAudio != null)
            SoundFXManager.Instance.PlaySoundFXClip(birdAudio, transform, volume);
        StartCoroutine(WaitForAudio());
    }

    private IEnumerator WaitForAudio()
    {
        yield return new WaitForSeconds(0.5f);
        if (slamAudio != null)
            SoundFXManager.Instance.PlaySoundFXClip(slamAudio, transform, volume);
    }
}


