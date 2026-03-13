using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCCompanion : MonoBehaviour
{
    [Header("Core Settings")]
    public string npcName = "Alicia";
    public int relationshipScore = 0; // -1000 đến 1000

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

    [Header("UI")]
    public Slider healthSlider; // Tạm thời để mốc nếu bạn cần làm máu cho NPC
    public TMP_Text nameText;
    public GameObject talkIcon; // Icon bấm E

    private void Start()
    {
        if (nameText != null) nameText.text = npcName;
        if (talkIcon != null) talkIcon.SetActive(false);
        if (playerTransform == null) playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        HandleMovement();
        HandleRelationshipBehaviors();
        HandleCombat();
    }

    private void HandleMovement()
    {
        // Nếu bị ghét cay đắng (-500), đứng im dỗi, không đi theo
        if (relationshipScore <= -500) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Hiện Icon nói chuyện nếu ở gần
        if (talkIcon != null)
            talkIcon.SetActive(distanceToPlayer <= 2.5f);

        // Đi theo Player nếu ở xa
        if (distanceToPlayer > followDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

            // Xoay mặt
            Vector3 scale = transform.localScale;
            scale.x = (playerTransform.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void HandleRelationshipBehaviors()
    {
        // 1. Tự động hồi máu cho Player nếu Relationship > 500
        if (relationshipScore >= 500)
        {
            healTimer -= Time.deltaTime;
            if (healTimer <= 0f)
            {
                // Hồi 1% máu tối đa của Player dựa theo cấu trúc StatsManager
                float healAmount = StatsManager.instance.maxHealth * 0.01f;
                StatsManager.instance.Heal(healAmount);
                Debug.Log($"{npcName} healed player for {healAmount} HP.");
                healTimer = healCooldown;
            }
        }
    }

    private void HandleCombat()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        // Nếu bị ghét quá (-500), AI bắn Player 1 cái rồi dỗi
        if (relationshipScore <= -500)
        {
            ShootAtTarget(playerTransform);
            attackTimer = 9999f; // Bắn 1 phát rồi nghỉ luôn tới khi được dỗ
            return;
        }

        // Tốc độ bắn tăng x4 nếu Relationship > 500
        float currentCooldown = (relationshipScore >= 500) ? baseAttackCooldown / 4f : baseAttackCooldown;

        // Tìm quái vật gần nhất
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));
        if (hits.Length > 0)
        {
            Transform closestEnemy = hits[0].transform; // Tạm lấy quái đầu tiên thấy
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
            // Truyền hướng bắn và sát thương
            Vector2 direction = (target.position - firePoint.position).normalized;
            projScript.Setup(direction, bulletDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}