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

    [Header("New Skills & Behaviors")]
    public float skill1Cooldown = 10f;
    private float skill1Timer = 5f; // Khởi tạo 5s để không xả skill ngay khi vừa vào game
    public float skill2Cooldown = 30f;
    private float skill2Timer = 15f;
    private bool isCastingSkill2 = false;
    private float angryTimer = 0f;
    private Vector2 wanderTarget;
    private float wanderTimer;

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

        if (angryTimer > 0f) angryTimer -= Time.deltaTime;
        skill1Timer -= Time.deltaTime;
        skill2Timer -= Time.deltaTime;

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
            
            // Nếu đang ở trạng thái dỗi mà bạn còn đánh cô ấy -> Phản công!
            if (relationshipScore <= -500)
            {
                TriggerAngryState();
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TriggerAngryState()
    {
        angryTimer = 5f; // Tức giận và tấn công Player trong 5 giây
        Debug.Log("<color=red>[Alicia]</color> Grrr! Đừng có chọc tức tôi!");
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

        // Ngắt toàn bộ chiêu thức đang niệm dang dở nếu lỡ chết
        isCastingSkill2 = false;
        StopAllCoroutines();

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
        if (isCastingSkill2) return; // Đứng yên tuyệt đối khi niệm chú Skill 2

        if (relationshipScore <= -500 && angryTimer <= 0f) 
        {
            // Trạng thái dỗi: Lảng vảng ngẫu nhiên (Wandering), không bám theo Player
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            {
                wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * 5f;
                wanderTimer = 3f; // Đổi hướng sau mỗi 3 giây
            }
            transform.position = Vector2.MoveTowards(transform.position, wanderTarget, moveSpeed * 0.5f * Time.deltaTime);
            Vector3 wScale = transform.localScale;
            wScale.x = (wanderTarget.x > transform.position.x) ? Mathf.Abs(wScale.x) : -Mathf.Abs(wScale.x);
            transform.localScale = wScale;
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
        if (isCastingSkill2) return;

        Transform target = null;
        
        // Xác định mục tiêu: Ưu tiên đập Player nếu đang tức giận
        if (angryTimer > 0f)
        {
            target = playerTransform;
        }
        else
        {
            // Fix: Quét toàn bộ và check Tag để tránh lỗi quên set Layer "Enemy" trong Unity
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    target = hit.transform;
                    break;
                }
            }
        }

        if (target == null) return;

        // SKILL 2: Quả cầu năng lượng khổng lồ AOE (Mỗi 30s)
        if (skill2Timer <= 0f)
        {
            StartCoroutine(CastSkill2GiantBall(target));
            skill2Timer = skill2Cooldown;
            return;
        }

        // SKILL 1: Nón năng lượng 3 đợt (Mỗi 10s)
        if (skill1Timer <= 0f)
        {
            StartCoroutine(CastSkill1Cone(target));
            skill1Timer = skill1Cooldown;
            return;
        }

        // Đánh thường
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;
            ShootAtTarget(target);
            attackTimer = currentCooldown;
        }
    }

    private void ShootAtTarget(Transform target)
    {
        if (magicBulletPrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(magicBulletPrefab, spawnPos, Quaternion.identity);
        CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
        if (projScript != null)
        {
            Vector2 direction = (target.position - spawnPos).normalized;
            // Chỉ gây sát thương cho Player nếu đang ở trạng thái Angry (phản công)
            projScript.Setup(direction, bulletDamage, angryTimer > 0f);
        }
    }

    private IEnumerator CastSkill1Cone(Transform target)
    {
        for (int wave = 0; wave < 3; wave++) // 3 đợt
        {
            if (target == null) break;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 baseDir = (target.position - spawnPos).normalized;
            float angleStep = 15f; 
            float startAngle = -((8 - 1) * angleStep) / 2f;

            for (int i = 0; i < 8; i++) // 8 quả mỗi đợt
            {
                float currentAngle = startAngle + (i * angleStep);
                Vector2 dir = Quaternion.Euler(0, 0, currentAngle) * baseDir;
                
                GameObject bullet = Instantiate(magicBulletPrefab, spawnPos, Quaternion.identity);
                CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
                if (projScript != null) projScript.Setup(dir, bulletDamage * 0.8f, angryTimer > 0f); // Sát thương 80% mỗi viên
            }
            yield return new WaitForSeconds(0.3f); // Delay giữa các đợt
        }
    }

    private IEnumerator CastSkill2GiantBall(Transform target)
    {
        isCastingSkill2 = true;
        Vector2 targetPos = target.position; // Khóa chết vị trí lúc bắt đầu cast
        
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        // Triệu hồi quả cầu và phóng to
        GameObject giantBall = Instantiate(magicBulletPrefab, spawnPos, Quaternion.identity);
        giantBall.transform.localScale = Vector3.one * 3f; // Phóng to 3 lần
        
        CompanionProjectile proj = giantBall.GetComponent<CompanionProjectile>();
        if (proj != null)
        {
             bool canHurtPlayer = (angryTimer > 0f); // Có làm tổn thương Player không?
             proj.SetupAoE(targetPos, bulletDamage * 3f, 4f, canHurtPlayer);
        }

        yield return new WaitForSeconds(1.5f); // Thời gian đứng im chờ giáng đòn
        isCastingSkill2 = false;
    }
}