using UnityEngine;

/// <summary>
/// Lightning bolt spell for AlterRexx enemy.
/// Deals magic damage to player on contact.
/// </summary>
public class Enemy_AlterRexx_Spell : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float magicDamage = 25f;
    [SerializeField] private float destroyTime = 0.42f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D spellCollider;

    [Header("Audio")]
    [SerializeField] private AudioClip strikeAudioClip;
    [SerializeField] private float volume = 1f;
    private bool hitPlayer = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        if (spellCollider != null)
            spellCollider.enabled = false;

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        magicDamage *= difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);
    }

    public void EnableTrigger()
    {
        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = true;
        }

        SoundFXManager.Instance.PlaySoundFXClip(strikeAudioClip, transform, volume);

        Destroy(gameObject, destroyTime);
    }
    public void DestroyGameObject()
    {
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (collision.CompareTag("Player") && !hitPlayer)
        {
            hitPlayer = true;
            // TODO: Hook into player damage system
            //var playerHealth = collision.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.ChangeHealth(-magicDamage);
            //}
        }
    }
}
