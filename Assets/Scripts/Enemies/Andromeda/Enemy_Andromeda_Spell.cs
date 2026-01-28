using UnityEngine;

public class Enemy_Andromeda_Spell : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float magicDamage = 40f;
    [SerializeField] private float destroyTime = 0.55f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private Enemy_Andromeda_Spell_State spellState;

    [Header("Audio")]
    [SerializeField] private AudioClip boomAudioClip;
    [SerializeField] private float volume = 1f;
    private bool hitPlayer = false;

    private StateManager<Enemy_Andromeda_Spell_State> stateManager;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        // ensure collider starts disabled so it only detects when explicitly enabled
        if (spellCollider != null)
            spellCollider.enabled = false;

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        magicDamage *= difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

        stateManager = new StateManager<Enemy_Andromeda_Spell_State>(animator, spellState);

        if (boomAudioClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(boomAudioClip, transform, volume);
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
            if (boomAudioClip != null)
                SoundFXManager.Instance.PlaySoundFXClip(boomAudioClip, transform, volume);
            hitPlayer = true;
            //var playerHealth = collision.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.ChangeHealth(-magicDamage);
            //}
        }
    }
}
