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
    [SerializeField] private float currentHealth; // Đã phơi bày ra Inspector!
    public float respawnTime = 60f;
    [SerializeField] private bool isDead = false;

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

    [Header("UI References")]
    public Slider healthSlider;
    public TMP_Text nameText;
    public TMP_Text relationshipText; // Kéo object Text chứa Icon vào đây
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

        UpdateRelationshipUI();
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;

        HandleMovement();
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

        // Nếu bị Player chém trúng (Friendly Fire)
        if (isFromPlayer)
        {
            relationshipScore -= 1;
            UpdateRelationshipUI(); // Cập nhật ngay lập tức
            Debug.Log($"<color=orange>[Hệ thống]</color> Bạn chém trúng Alicia! Relationship: {relationshipScore}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // --- THUẬT TOÁN GIẢ CHẾT ---
    private void Die()
    {
        isDead = true;
        relationshipScore -= 10;
        UpdateRelationshipUI();
        Debug.Log($"<color=red>[Hệ thống]</color> Alicia tử trận! Bị trừ 10 điểm. Relationship: {relationshipScore}");
        if (healthSlider != null) healthSlider.value = 0;

        // Tắt Sprite và Collider (Để đạn xuyên qua)
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // Ẩn thanh máu và Canvas trên đầu
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) child.gameObject.SetActive(false);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Chờ đúng 60 giây
        yield return new WaitForSeconds(respawnTime);

        // Hồi sinh tại vị trí Player
        transform.position = playerTransform.position;
        currentHealth = maxHealth;
        isDead = false;

        // Bật lại thân xác
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) child.gameObject.SetActive(true);
        }

        if (healthSlider != null) healthSlider.value = currentHealth;
        Debug.Log("<color=green>[Hệ thống]</color> Alicia đã hồi sinh bên cạnh bạn!");
    }

    private void HandleMovement()
    {
        if (relationshipScore <= -500) return; // Giận quá nghỉ đi theo
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

    private void HandleCombat()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        // Nếu cực kỳ giận, quay sang bắn luôn Player!
        if (relationshipScore <= -500)
        {
            ShootAtTarget(playerTransform);
            attackTimer = 9999f;
            return;
        }

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