using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCCompanion : MonoBehaviour
{
    public enum CompanionClass { MageRanged, MeleeBrawler }
    
    [Header("Core Settings")]
    public string npcID = "npc_alicia";
    public string npcName = "Alicia";
    [TextArea(2,4)]
    public string systemPrompt = "You are Alicia. Reply strictly in English (1-3 sentences). Append [REL: X] at the end.";
    public CompanionClass companionClass = CompanionClass.MageRanged;
    public int relationshipScore = 0;

    [Header("Health & Respawn")]
    public float maxHealth = 500f;
    [SerializeField] private float currentHealth;
    public float respawnTime = 60f;
    private bool isDead = false;

    [Header("Movement & Target")]
    public Transform playerTransform;
    public float followDistance = 2f;
    public float moveSpeed = 100f;
    public float maxTeleportDistance = 50f;

    [Header("Combat")]
    [Tooltip("Alicia: Prefab Đạn ma thuật.\nJohnson: Prefab Vụ nổ AOE cho Skill 1.")]
    public GameObject magicBulletPrefab;
    
    [Tooltip("Hiệu ứng hình ảnh khi tung Ultimate (Dành cho Johnson).")]
    public GameObject ultimateEffectPrefab; 
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
    private float skill1Timer = 5f; 
    public float skill2Cooldown = 30f;
    private float skill2Timer = 15f;
    private bool isCastingSkill2 = false;
    private float angryTimer = 0f;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private Transform currentTarget;

    [Header("UI References")]
    public Slider healthSlider;
    public TMP_Text nameText;
    public GameObject talkIcon;

    private string originalTag;
    private int originalLayer;
    private Rigidbody2D rb;

    private void Start()
    {
        originalTag = gameObject.tag;
        originalLayer = gameObject.layer;
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (nameText != null) nameText.text = npcName;
        if (talkIcon != null) talkIcon.SetActive(false);
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Bỏ qua va chạm giữa NPC và Player để không chắn đường (Collision)
        if (playerTransform != null)
        {
            Collider2D[] npcColliders = GetComponentsInChildren<Collider2D>();
            Collider2D[] playerColliders = playerTransform.GetComponentsInChildren<Collider2D>();
            foreach (var nCol in npcColliders)
            {
                foreach (var pCol in playerColliders)
                {
                    Physics2D.IgnoreCollision(nCol, pCol, true);
                }
            }
        }

        // Tự động đăng ký NPC này vào LLMChatManager khi khởi chạy
        if (LLMChatManager.Instance != null)
        {
            LLMChatManager.Instance.RegisterCompanion(this);
        }

        UpdateRelationshipUI();
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;

        CheckDistanceAndTeleport();

        if (angryTimer > 0f) angryTimer -= Time.deltaTime;
        skill1Timer -= Time.deltaTime;
        skill2Timer -= Time.deltaTime;

        FindTarget();
        HandleMovement();
        HandleRelationshipBehaviors();
        HandleCombat();
    }

    private void LateUpdate()
    {
        // Giữ cho các Canvas UI (chứa tên, thanh máu) không bị lật ngược khi NPC quay mặt
        if (transform.localScale.x != 0)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Canvas>() != null)
                {
                    Vector3 childScale = child.localScale;
                    childScale.x = Mathf.Sign(transform.localScale.x) * Mathf.Abs(childScale.x);
                    child.localScale = childScale;
                }
            }
        }
    }

    private void CheckDistanceAndTeleport()
    {
        if (isCastingSkill2) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > maxTeleportDistance)
        {
            Debug.Log($"<color=cyan>[Hệ thống]</color> {npcName} đã đi quá xa, dịch chuyển về bên cạnh Player.");
            transform.position = playerTransform.position + new Vector3(-1.5f, 0, 0); 
        }
    }

    private void FindTarget()
    {
        if (angryTimer > 0f)
        {
            currentTarget = playerTransform;
            return;
        }

        currentTarget = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, followDistance * 2f);
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = hit.transform;
                }
            }
        }
    }

    public void UpdateRelationshipUI()
    {
        if (LLMChatManager.Instance != null && LLMChatManager.Instance.activeNPC == this)
        {
            LLMChatManager.Instance.UpdateRelationshipUI();
        }
    }

    public void TakeDamage(float amount, bool isFromPlayer)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (isFromPlayer)
        {
            relationshipScore -= 1;
            UpdateRelationshipUI();
            Debug.Log($"<color=orange>[Hệ thống]</color> Bạn vừa chém trúng {npcName}! Relationship: {relationshipScore}");
            
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
        angryTimer = 5f; 
        Debug.Log($"<color=red>[{npcName}]</color> Grrr! Đừng có chọc tức tôi!");
    }

    private void Die()
    {
        isDead = true;
        relationshipScore -= 10;
        UpdateRelationshipUI(); 
        Debug.Log($"<color=red>[Hệ thống]</color> {npcName} đã tử trận! Bị trừ 10 điểm. Relationship: {relationshipScore}");

        if (healthSlider != null) healthSlider.value = 0;

        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        StopNPC();

        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) child.gameObject.SetActive(false);
        }

        gameObject.tag = "Untagged";
        gameObject.layer = 2;
        transform.position = new Vector3(9999f, 9999f, 0f);

        isCastingSkill2 = false;
        StopAllCoroutines();

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        gameObject.tag = originalTag;
        gameObject.layer = originalLayer;
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
        Debug.Log($"<color=green>[Hệ thống]</color> {npcName} đã hồi sinh!");
    }

    private void HandleMovement()
    {
        if (isCastingSkill2) 
        {
            StopNPC();
            return; 
        }

        if (relationshipScore <= -500 && angryTimer <= 0f) 
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            {
                wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * 5f;
                wanderTimer = 3f; 
            }
            MoveNPC(wanderTarget, moveSpeed);
            return; 
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (talkIcon != null) talkIcon.SetActive(distanceToPlayer <= 2.5f);

        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
            
            float stopDistance = (companionClass == CompanionClass.MageRanged) ? attackRange * 0.8f : 1.2f;

            if (distanceToTarget > stopDistance)
            {
                MoveNPC(currentTarget.position, moveSpeed);
            }
            else
            {
                StopNPC();
            }
        }
        else 
        {
            // Lượn lờ xung quanh Player thay vì đi theo thụ động
            wanderTimer -= Time.deltaTime;
            
            float currentMoveSpeed = moveSpeed * 0.75f; // Tốc độ lượn lờ là 75%

            if (distanceToPlayer > followDistance * 1.5f)
            {
                wanderTarget = playerTransform.position;
                wanderTimer = 0.5f; // Liên tục cập nhật khi ở xa
                currentMoveSpeed = moveSpeed; // Tăng tốc tối đa (100%) để đuổi kịp Player
            }
            else if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.5f)
            {
                Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(followDistance * 0.5f, followDistance);
                wanderTarget = (Vector2)playerTransform.position + randomOffset;
                wanderTimer = Random.Range(1.5f, 3.5f);
            }

            MoveNPC(wanderTarget, currentMoveSpeed);
        }
    }

    private void MoveNPC(Vector2 targetPosition, float speed)
    {
        float distance = Vector2.Distance(transform.position, targetPosition);
        if (distance <= 0.1f)
        {
            StopNPC();
            return;
        }
        
        if (rb != null)
        {
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * speed;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }

        // Tránh lật mặt liên tục khi mục tiêu ở vị trí xấp xỉ X
        if (Mathf.Abs(targetPosition.x - transform.position.x) > 0.05f)
        {
            Vector3 scale = transform.localScale;
            scale.x = (targetPosition.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void StopNPC()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private void HandleRelationshipBehaviors()
    {
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
                    Debug.Log($"<color=green>[{npcName}]</color> Đã buff {healAmount} HP cho bạn! ❤️");
                }
            }
        }
    }

    private void HandleCombat()
    {
        if (isCastingSkill2 || currentTarget == null) return;

        if (companionClass == CompanionClass.MageRanged)
        {
            if (skill2Timer <= 0f) { StartCoroutine(CastSkill2GiantBall(currentTarget)); skill2Timer = skill2Cooldown; return; }
            if (skill1Timer <= 0f) { StartCoroutine(CastSkill1Cone(currentTarget)); skill1Timer = skill1Cooldown; return; }
            
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;
                ShootAtTarget(currentTarget);
                attackTimer = currentCooldown;
            }
        }
        else if (companionClass == CompanionClass.MeleeBrawler)
        {
            if (skill2Timer <= 0f) { StartCoroutine(JohnsonUltimateBuff()); skill2Timer = skill2Cooldown; return; }
            if (skill1Timer <= 0f) { StartCoroutine(JohnsonSkill1Explode(currentTarget)); skill1Timer = skill1Cooldown; return; }
            
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;
                JohnsonMeleeAttack(currentTarget);
                attackTimer = currentCooldown;
            }
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
            projScript.Setup(direction, bulletDamage, angryTimer > 0f);
        }
    }

    private IEnumerator CastSkill1Cone(Transform target)
    {
        for (int wave = 0; wave < 3; wave++) 
        {
            if (target == null) break;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 baseDir = (target.position - spawnPos).normalized;
            float angleStep = 15f; 
            float startAngle = -((8 - 1) * angleStep) / 2f;

            for (int i = 0; i < 8; i++) 
            {
                float currentAngle = startAngle + (i * angleStep);
                Vector2 dir = Quaternion.Euler(0, 0, currentAngle) * baseDir;
                
                GameObject bullet = Instantiate(magicBulletPrefab, spawnPos, Quaternion.identity);
                CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
                if (projScript != null) projScript.Setup(dir, bulletDamage * 0.8f, angryTimer > 0f); 
            }
            yield return new WaitForSeconds(0.3f); 
        }
    }

    private IEnumerator CastSkill2GiantBall(Transform target)
    {
        isCastingSkill2 = true;
        Vector2 targetPos = target.position; 
        
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject giantBall = Instantiate(magicBulletPrefab, spawnPos, Quaternion.identity);
        giantBall.transform.localScale = Vector3.one * 3f; 
        
        CompanionProjectile proj = giantBall.GetComponent<CompanionProjectile>();
        if (proj != null)
        {
             bool canHurtPlayer = (angryTimer > 0f); 
             proj.SetupAoE(targetPos, bulletDamage * 3f, 4f, canHurtPlayer);
        }

        yield return new WaitForSeconds(1.5f); 
        isCastingSkill2 = false;
    }

    private void JohnsonMeleeAttack(Transform target)
    {
        if (Vector2.Distance(transform.position, target.position) <= 2.5f)
        {
            var enemyHealth = target.GetComponent<IEnemy_Health>();
            if (enemyHealth != null) enemyHealth.ChangeHealth(-bulletDamage);
            else if (angryTimer > 0f && target == playerTransform) StatsManager.instance.TakeDamage(bulletDamage);
        }
    }

    private IEnumerator JohnsonSkill1Explode(Transform target)
    {
        transform.position = target.position + new Vector3(Random.Range(-0.5f, 0.5f), 0, 0);
        if (magicBulletPrefab != null)
        {
            GameObject explosion = Instantiate(magicBulletPrefab, transform.position, Quaternion.identity);
            CompanionProjectile proj = explosion.GetComponent<CompanionProjectile>();
            if (proj != null) proj.SetupInstantAoE(bulletDamage * 2f, 3f, angryTimer > 0f);
        }
        yield return null;
    }

    private IEnumerator JohnsonUltimateBuff()
    {
        isCastingSkill2 = true;
        transform.position = playerTransform.position + new Vector3(1f, 0, 0);
        
        if (ultimateEffectPrefab != null)
        {
            GameObject effect = Instantiate(ultimateEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); 
        }

        if (StatsManager.instance != null && !StatsManager.instance.IsDead) { StatsManager.instance.Heal(1000f); StatsManager.instance.isInvincible = true; }
        yield return new WaitForSeconds(5f); 
        if (StatsManager.instance != null) StatsManager.instance.isInvincible = false;
        
        isCastingSkill2 = false;
    }
}