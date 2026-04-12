using UnityEngine;
using System.Collections;

public class CompanionProjectile : MonoBehaviour
{
    public float speed = 10f;
    public float destroyTime = 2f;

    private Vector2 moveDirection;
    private float damage;
    private Rigidbody2D rb;

    // Biến hỗ trợ Skill AoE Giant Ball
    private bool isAoE = false;
    private float aoeRadius = 4f;
    private bool canDamagePlayer = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, destroyTime); // Tự hủy sau 2s để tránh rác RAM
    }

    public void Setup(Vector2 direction, float dmg, bool dmgPlayer = false)
    {
        moveDirection = direction;
        damage = dmg;
        canDamagePlayer = dmgPlayer;
    }

    public void SetupAoE(Vector2 targetPos, float dmg, float radius, bool dmgPlayer)
    {
        isAoE = true;
        damage = dmg;
        aoeRadius = radius;
        canDamagePlayer = dmgPlayer;
        StartCoroutine(AoERoutine(targetPos));
    }

    private void FixedUpdate()
    {
        if (isAoE) return; // Vô hiệu hóa vận tốc vật lý mặc định nếu đang là đạn bay theo chuỗi AoE
        rb.velocity = moveDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isAoE) return; // Bỏ qua va chạm thường nếu là đạn AoE

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
        else if (collision.CompareTag("Player") && canDamagePlayer)
        {
            StatsManager.instance.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    private IEnumerator AoERoutine(Vector2 targetPos)
    {
        // Bước 1: Bay lên ngay phía trên mục tiêu
        Vector2 startPos = transform.position;
        Vector2 hoverPos = targetPos + new Vector2(0f, 6f);
        
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(startPos, hoverPos, t / 0.5f);
            yield return null;
        }

        // Bước 2: Treo lơ lửng chờ 1 giây
        yield return new WaitForSeconds(1f);

        // Bước 3: Giáng xuống cực nhanh (0.15s)
        t = 0;
        Vector2 fallStart = transform.position;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.position = Vector2.Lerp(fallStart, targetPos, t / 0.15f);
            yield return null;
        }

        // Tới đích -> Nổ + Vẽ sóng Sonic
        CreateSonicWave();
        
        // Trừ máu vòng tròn
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                var enemyHealth = hit.GetComponent<IEnemy_Health>();
                if (enemyHealth != null) enemyHealth.ChangeHealth(-damage);
            }
            else if (hit.CompareTag("Player") && canDamagePlayer)
            {
                StatsManager.instance.TakeDamage(damage);
            }
        }

        // Hủy quả cầu gốc
        Destroy(gameObject);
    }

    private void CreateSonicWave()
    {
        // Render đường tròn Sonic bằng LineRenderer
        GameObject wave = new GameObject("SonicWave");
        wave.transform.position = transform.position;
        LineRenderer lr = wave.AddComponent<LineRenderer>();
        
        lr.startWidth = 0.5f;
        lr.endWidth = 0.5f;
        lr.startColor = new Color(0, 1f, 1f, 1f);   // Màu Cyan
        lr.endColor = new Color(0, 1f, 1f, 0f);     // Phai dần đi
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.positionCount = 51; // 50 phân đoạn mượt mà
        
        for (int i = 0; i <= 50; i++)
        {
            float angle = i * Mathf.PI * 2f / 50;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * aoeRadius, Mathf.Sin(angle) * aoeRadius, 0) + wave.transform.position);
        }
        
        Destroy(wave, 0.4f); // Xóa hiệu ứng sau 0.4s
    }
}