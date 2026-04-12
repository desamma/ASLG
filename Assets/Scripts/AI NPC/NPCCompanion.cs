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

    [Header("Special Skills & Behaviors")]
    public float specialSkillCooldown = 10f;
    private float specialSkillTimer;
    private float hostilityTimer;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float contactDamageCooldown;

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

        specialSkillTimer = specialSkillCooldown;
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;

        contactDamageCooldown -= Time.deltaTime;

        HandleMovement();
        HandleRelationshipBehaviors();
        HandleCombat();
        HandleSpecialSkill();
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

            if (relationshipScore <= -500)
            {
                TriggerHostility(15f); // Bị chém lúc đang dỗi -> Tấn công lại 15 giây
            }
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

    // --- NHẬN SÁT THƯƠNG TỪ QUÁI ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && contactDamageCooldown <= 0f)
        {
            TakeDamage(10f, false);
            contactDamageCooldown = 1f;
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if ((collider.gameObject.CompareTag("Enemy") || collider.gameObject.CompareTag("EnemyAttack")) && contactDamageCooldown <= 0f)
        {
            TakeDamage(15f, false);
            contactDamageCooldown = 1f;
        }
    }

    public void TriggerHostility(float duration)
    {
        hostilityTimer = duration;
        Debug.Log($"<color=red>[Alicia]</color> Quá đáng lắm rồi! Alicia quay sang tấn công bạn!");
    }

    // --- CÁC HÀM XỬ LÝ HÀNH VI ---
    private void HandleMovement()
    {
        if (relationshipScore <= -500)
        {
            // Lảng vảng, tự đi dạo xung quanh, không bám theo Player nữa
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            {
                wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * 5f;
                wanderTimer = Random.Range(2f, 5f);
            }
            
            transform.position = Vector2.MoveTowards(transform.position, wanderTarget, (moveSpeed * 0.5f) * Time.deltaTime);
            
            Vector3 wScale = transform.localScale;
            wScale.x = (wanderTarget.x > transform.position.x) ? Mathf.Abs(wScale.x) : -Mathf.Abs(wScale.x);
            transform.localScale = wScale;

            if (talkIcon != null) talkIcon.SetActive(false);
            return;
        }

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
        hostilityTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        // Phản đòn Player nếu bị chọc tức
        if (hostilityTimer > 0f)
        {
            if (attackTimer <= 0f)
            {
                ShootAtTarget(playerTransform, true);
                attackTimer = baseAttackCooldown;
            }
            return; // Đang đánh Player thì không đánh quái
        }

        if (attackTimer > 0f) return;

        // Tìm quái gần nhất để bắn (Tự vệ hoặc Hỗ trợ)
        float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));

        if (hits.Length > 0)
        {
            Transform closestEnemy = hits[0].transform;
            ShootAtTarget(closestEnemy, false);
            attackTimer = currentCooldown;
        }
    }

    private void HandleSpecialSkill()
    {
        if (relationshipScore <= -500) return; // Dỗi thì không xài skill đặc biệt

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));
        bool isPlayerLow = (StatsManager.instance != null && StatsManager.instance.currentHealth < StatsManager.instance.maxHealth * 0.5f);

        // Chỉ đếm thời gian khi đang trong combat hoặc Player đang thoi thóp
        if (hits.Length > 0 || isPlayerLow)
        {
            specialSkillTimer -= Time.deltaTime;
            if (specialSkillTimer <= 0f)
            {
                specialSkillTimer = specialSkillCooldown; // Reset 10s

                if (isPlayerLow)
                {
                    // Hồi 20% máu khẩn cấp
                    float healAmount = StatsManager.instance.maxHealth * 0.2f;
                    StatsManager.instance.Heal(healAmount);
                    Debug.Log($"<color=green>[Alicia]</color> Hồi phục khẩn cấp 20% HP!");
                }
                else if (hits.Length > 0)
                {
                    // Bắn 3 loạt đạn hình nón
                    StartCoroutine(ConeAttackRoutine(hits[0].transform));
                    Debug.Log($"<color=cyan>[Alicia]</color> Dùng kỹ năng: Mưa Sao Băng!");
                }
            }
        }
        else
        {
            // Nạp lại skill nhanh hơn khi ngoài giao tranh
            if (specialSkillTimer > 0) specialSkillTimer -= Time.deltaTime * 2f;
        }
    }

    private void ShootAtTarget(Transform target, bool isHostile)
    {
        if (magicBulletPrefab == null || firePoint == null) return;
        GameObject bullet = Instantiate(magicBulletPrefab, firePoint.position, Quaternion.identity);
        CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
        if (projScript != null)
        {
            Vector2 direction = (target.position - firePoint.position).normalized;
            projScript.Setup(direction, bulletDamage, isHostile);
        }
    }

    private IEnumerator ConeAttackRoutine(Transform target)
    {
        for (int wave = 0; wave < 3; wave++)
        {
            if (target == null || isDead) break;

            Vector2 baseDir = (target.position - firePoint.position).normalized;
            float startAngle = -30f; // Góc mở rộng 60 độ
            float angleStep = 60f / 9f; // Chia làm 10 viên đạn

            for (int i = 0; i < 10; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
                
                GameObject bullet = Instantiate(magicBulletPrefab, firePoint.position, Quaternion.identity);
                bullet.GetComponent<CompanionProjectile>()?.Setup(dir, bulletDamage * 0.75f, false);
            }
            yield return new WaitForSeconds(0.3f); // Thời gian chờ giữa 3 loạt đạn
        }
    }
}