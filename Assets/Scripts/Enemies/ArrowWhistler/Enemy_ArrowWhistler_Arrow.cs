using UnityEngine;

public class Enemy_ArrowWhistler_Arrow : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 10f;
    [SerializeField] private float moveSpeed = 10f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Rigidbody2D rb;

    [Header("Audio")]
    [SerializeField] private AudioClip hitAudio;
    [SerializeField] private float volume = 1f;

    private bool hitPlayer = false;
    private EnemyStats casterStats;
    private bool isInitialized = false;
    private Vector2 direction;

    public void Initialize(EnemyStats stats, Vector2 targetDirection)
    {
        casterStats = stats;
        direction = targetDirection.normalized;
        isInitialized = true;

        // Rotate arrow to face the direction of movement
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = true;
        }

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private void FixedUpdate()
    {
        if (rb != null && isInitialized)
        {
            rb.velocity = direction * moveSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (collision.CompareTag("Player") && !hitPlayer)
        {
            hitPlayer = true;

            var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;

            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, collision.transform.position, Quaternion.identity);
                effect.transform.localScale = new Vector3(2f, 2f, 1f);
            }

            if (hitAudio != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(hitAudio, transform, volume);
            }

            float damage = casterStats.Strength * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);

            //var playerHealth = collision.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.ChangeHealth(-damage);
            //}

            Destroy(gameObject);
        }
        else if (!collision.CompareTag("Enemy"))
        {
            // Destroy arrow when it hits anything else (walls, obstacles, etc.)
            Destroy(gameObject);
        }
    }
}
