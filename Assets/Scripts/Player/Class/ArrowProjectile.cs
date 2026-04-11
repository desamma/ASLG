using UnityEngine;

[DisallowMultipleComponent]
public class ArrowProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private int damage;
    private float knockbackForce;
    private float knockbackTime;
    private float stunTime;
    private GameObject hitEffectPrefab;
    private AudioClip hitClip;
    private float volume;
    private LayerMask enemyLayer;

    private bool hasHit = false;

    /// <summary>
    /// Called immediately after Instantiate by PlayerAttack.
    /// </summary>
    public void Initialise(
        Vector2 dir, float spd, float life,
        int dmg, float kbForce, float kbTime, float stun,
        GameObject hitFx, AudioClip hitSfx, float vol,
        LayerMask enemies)
    {
        direction = dir.normalized;
        speed = spd;
        lifetime = life;
        damage = dmg;
        knockbackForce = kbForce;
        knockbackTime = kbTime;
        stunTime = stun;
        hitEffectPrefab = hitFx;
        hitClip = hitSfx;
        volume = vol;
        enemyLayer = enemies;

        // Rotate arrow sprite to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        var playerCol = Physics2D.OverlapPoint(transform.position,
                        LayerMask.GetMask("Player"));
        if (playerCol != null)
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerCol);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasHit) return;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (!IsInLayerMask(other.gameObject.layer, enemyLayer)) return;
        if (!other.CompareTag("Enemy")) return;

        hasHit = true;

        if (hitClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(hitClip, transform, volume);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity, other.transform);

        var enemyHealth = other.GetComponent<IEnemy_Health>();
        var enemyMovement = other.GetComponent<IEnemy_Movement>();

        enemyHealth?.ChangeHealth(-damage);
        enemyMovement?.KnockBack(transform, knockbackForce, knockbackTime, stunTime);

        Destroy(gameObject);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask) =>
        (mask.value & (1 << layer)) != 0;
}