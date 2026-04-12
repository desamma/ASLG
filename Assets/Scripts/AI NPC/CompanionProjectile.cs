using UnityEngine;

public class CompanionProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float destroyTime = 2f;

    private Vector2 moveDirection;
    private float damage;
    private Rigidbody2D rb;
    private bool isHostileToPlayer = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, destroyTime); // Tự hủy sau 2s để tránh rác RAM
    }

    public void Setup(Vector2 direction, float dmg, bool hostileToPlayer = false)
    {
        moveDirection = direction;
        damage = dmg;
        isHostileToPlayer = hostileToPlayer;
    }

    private void FixedUpdate()
    {
        rb.velocity = moveDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Bắn trúng quái
        if (collision.CompareTag("Enemy"))
        {
            var enemyHealth = collision.GetComponent<IEnemy_Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-damage); // Trừ máu giống hệt PlayerAttack.cs
            }
            Destroy(gameObject);
        }
        // Bắn trúng Player (Khi đang bị dỗi / Relationship âm)
        else if (collision.CompareTag("Player") && isHostileToPlayer)
        {
            StatsManager.instance.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}