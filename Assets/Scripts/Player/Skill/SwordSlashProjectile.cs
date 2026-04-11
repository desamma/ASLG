using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class SwordSlashProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private float knockbackForce;
    private float knockbackTime;
    private float stunTime;
    private LayerMask enemyLayer;

    // Prevent hitting the same enemy twice per slash
    private readonly HashSet<GameObject> alreadyHit = new();

    public void Initialise(
        Vector2 dir, float spd, float lifetime,
        int dmg, float kbForce, float kbTime, float stun,
        LayerMask enemies)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        knockbackForce = kbForce;
        knockbackTime = kbTime;
        stunTime = stun;
        enemyLayer = enemies;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (alreadyHit.Contains(other.gameObject)) return;

        alreadyHit.Add(other.gameObject);

        other.GetComponent<IEnemy_Health>()?.ChangeHealth(-damage);
        other.GetComponent<IEnemy_Movement>()?.KnockBack(
            transform, knockbackForce, knockbackTime, stunTime);
    }
}