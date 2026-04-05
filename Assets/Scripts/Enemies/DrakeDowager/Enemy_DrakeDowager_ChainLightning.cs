using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_DrakeDowager_ChainLightning : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float destroyTime = 2f;
    [SerializeField] private float warningDelay = 0.5f;

    [Header("Components")]
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private GameObject hitEffect;

    private bool hitPlayer = false;
    private EnemyStats casterStats;
    private Vector2 direction;

    public void Initialize(EnemyStats stats, Vector2 targetDirection)
    {
        casterStats = stats;
        direction = targetDirection.normalized;

        // Rotate arrow to face the direction of movement
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    private void Start()
    {
        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        // Disable collider initially
        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = false;
        }

        StartCoroutine(ActivationDelay());

        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private IEnumerator ActivationDelay()
    {
        yield return new WaitForSeconds(warningDelay);
        
        if (spellCollider != null)
            spellCollider.enabled = true;
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

            float damage = casterStats.Strength * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);

            StatsManager.instance.TakeDamage(damage);
        }
    }
}

