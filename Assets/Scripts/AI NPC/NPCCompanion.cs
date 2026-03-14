using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCCompanion : MonoBehaviour
{
    [Header("Core Settings")]
    public string npcName = "Alicia";
    public int relationshipScore = 0;

    [Header("Health & Respawn")]
    public float maxHealth = 500f;
    [SerializeField] private float currentHealth;
    public float respawnTime = 60f;
    private bool isDead = false;

    [Header("Movement & Target")]
    public Transform playerTransform;
    public float followDistance = 2f;
    public float moveSpeed = 4f;

    [Header("Combat")]
    public GameObject magicBulletPrefab;
    public Transform firePoint;
    public float attackRange = 7f;
    public float baseAttackCooldown = 2f;
    public float bulletDamage = 15f;
    private float attackTimer;

    [Header("Healing Player")]
    public float healCooldown = 10f;
    private float healTimer;

    [Header("UI References")]
    public Slider healthSlider;
    public TMP_Text nameText;
    public TMP_Text relationshipText;
    public GameObject talkIcon;

    private void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (nameText != null) nameText.text = npcName;
        if (talkIcon != null) talkIcon.SetActive(false);
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Gọi lần đầu để setup UI cảm xúc
        UpdateRelationshipUI();
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;

        HandleMovement();
        HandleRelationshipBehaviors();
        HandleCombat();
    }

    // --- HỆ THỐNG CẢM XÚC (UI) ---
    private void UpdateRelationshipUI()
    {
        if (relationshipText == null) return;

        string icon = "😐"; // 0: Neutral
        if (relationshipScore >= 500) icon = "❤️";
        else if (relationshipScore >= 200) icon = "😄";
        else if (relationshipScore <= -500) icon = "🔥";
        else if (relationshipScore <= -200) icon = "😢";

        relationshipText.text = $"{icon} {relationshipScore}";
    }

    // --- HỆ THỐNG NHẬN SÁT THƯƠNG ---
    public void TakeDamage(float amount, bool isFromPlayer)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (isFromPlayer)
        {
            relationshipScore -= 1;
            UpdateRelationshipUI(); // Phải gọi dòng này để icon đổi ngay
            Debug.Log($"<color=orange>[Hệ thống]</color> Bạn vừa chém trúng Alicia! Relationship: {relationshipScore}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        relationshipScore -= 10;
        UpdateRelationshipUI(); // Phải gọi dòng này để icon đổi ngay
        Debug.Log($"<color=red>[Hệ thống]</color> Alicia đã tử trận! Bị trừ 10 điểm. Relationship: {relationshipScore}");

        if (healthSlider != null) healthSlider.value = 0;

        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) child.gameObject.SetActive(false);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        transform.position = playerTransform.position;
        currentHealth = maxHealth;
        isDead = false;

        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;

        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) child.gameObject.SetActive(true);
        }

        if (healthSlider != null) healthSlider.value = currentHealth;
        Debug.Log("<color=green>[Hệ thống]</color> Alicia đã hồi sinh!");
    }

    // --- CÁC HÀM XỬ LÝ HÀNH VI ---
    private void HandleMovement()
    {
        if (relationshipScore <= -500) return; // Dỗi, không đi theo

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (talkIcon != null) talkIcon.SetActive(distanceToPlayer <= 2.5f);

        if (distanceToPlayer > followDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
            Vector3 scale = transform.localScale;
            scale.x = (playerTransform.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void HandleRelationshipBehaviors()
    {
        // Khi đạt 500 hảo cảm, hồi 1% máu mỗi 10 giây (theo code cũ của bạn)
        if (relationshipScore >= 500)
        {
            healTimer -= Time.deltaTime;
            if (healTimer <= 0f)
            {
                if (StatsManager.instance != null && !StatsManager.instance.IsDead)
                {
                    float healAmount = StatsManager.instance.maxHealth * 0.01f;
                    StatsManager.instance.Heal(healAmount);
                    healTimer = healCooldown;
                    Debug.Log($"<color=green>[Alicia]</color> Đã buff {healAmount} HP cho bạn! ❤️");
                }
            }
        }
    }

    private void HandleCombat()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        // Nếu hảo cảm chạm đáy, bắn thẳng vào Player
        if (relationshipScore <= -500)
        {
            ShootAtTarget(playerTransform);
            attackTimer = 9999f; // Bắn 1 phát rồi nghỉ chơi luôn
            return;
        }

        // Tốc độ bắn x4 khi đạt 500 hảo cảm
        float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));

        if (hits.Length > 0)
        {
            Transform closestEnemy = hits[0].transform;
            ShootAtTarget(closestEnemy);
            attackTimer = currentCooldown;
        }
    }

    private void ShootAtTarget(Transform target)
    {
        if (magicBulletPrefab == null || firePoint == null) return;
        GameObject bullet = Instantiate(magicBulletPrefab, firePoint.position, Quaternion.identity);
        CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
        if (projScript != null)
        {
            Vector2 direction = (target.position - firePoint.position).normalized;
            projScript.Setup(direction, bulletDamage);
        }
    }
}