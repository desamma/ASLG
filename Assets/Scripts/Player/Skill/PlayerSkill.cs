using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkill : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform skillOrigin; 

    [Header("Audio")]
    [SerializeField] private float volume = 1f;

    [Header("Debug (read only)")]
    [SerializeField] private int currentUpgradeTier = 0;
    [SerializeField] private float cooldownTimer = 0f;

    private bool isUsingSkill = false;

    private PlayerClassData ClassData => ClassManager.Instance.CurrentClassData;
    private SkillData SkillData => ClassData.skillData;

    private void Start()
    {
        if (movement == null) movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
        else
            cooldownTimer = 0f;

        if (Input.GetButtonDown("Skill") && CanUseSkill())
            TriggerSkill();
    }

    private bool CanUseSkill()
    {
        if (SkillData == null) return false;
        var sm = movement.GetStateManager();
        return sm != null &&
               cooldownTimer <= 0f &&
               !isUsingSkill &&
               StatsManager.instance.currentMana >= GetCurrentManaCost() &&
               !sm.IsInState(PlayerState.Death) &&
               !sm.IsInState(PlayerState.Knockback);
    }

    private void TriggerSkill()
    {
        StatsManager.instance.currentMana -= GetCurrentManaCost();
        cooldownTimer = GetCurrentCooldown();

        // Lấy Class trực tiếp từ Data để luôn gọi đúng Skill kể cả khi mượn Data
        switch (ClassData.playerClass)
        {
            case PlayerClass.Knight: StartCoroutine(KnightSkillRoutine()); break;
            case PlayerClass.Archer: StartCoroutine(ArcherSkillRoutine()); break;
            case PlayerClass.Rogue: StartCoroutine(RogueSkillRoutine()); break;
            case PlayerClass.Summoner: StartCoroutine(SummonerSkillRoutine()); break;
            default: StartCoroutine(KnightSkillRoutine()); break; // Fallback an toàn
        }
    }

    public bool TryUpgrade()
    {
        if (SkillData == null) return false;
        if (currentUpgradeTier >= SkillData.upgrades.Length) return false;

        var tier = SkillData.upgrades[currentUpgradeTier];
        if (!StatsManager.instance.SpendUpgradePoints(tier.pointCost)) return false;

        currentUpgradeTier++;
        Debug.Log($"[PlayerSkill] {SkillData.skillName} upgraded to tier {currentUpgradeTier}.");
        return true;
    }

    public void LoadSkillTier(int tier)
    {
        if (SkillData == null) return;
        currentUpgradeTier = Mathf.Clamp(tier, 0, SkillData.upgrades?.Length ?? 0);
        Debug.Log($"[PlayerSkill] Loaded skill tier: {currentUpgradeTier}");
    }

    public Sprite SkillIcon => SkillData.icon;
    public int CurrentTier => currentUpgradeTier;
    public int MaxTier => SkillData.upgrades?.Length ?? 0;
    public float CooldownFraction => SkillData != null ? cooldownTimer / GetCurrentCooldown() : 0f;
    public float CooldownTimer => cooldownTimer;
    public float ManaCost => GetCurrentManaCost();
    public int NextUpgradeCost => (SkillData != null && currentUpgradeTier < MaxTier) ? SkillData.upgrades[currentUpgradeTier].pointCost : 0;

    private float GetCurrentManaCost()
    {
        float v = SkillData.manaCost;
        for (int i = 0; i < currentUpgradeTier; i++) v *= SkillData.upgrades[i].manaCostMultiplier;
        return v;
    }

    private float GetCurrentCooldown()
    {
        float v = SkillData.cooldown;
        for (int i = 0; i < currentUpgradeTier; i++) v *= SkillData.upgrades[i].cooldownMultiplier;
        return Mathf.Max(0.5f, v);
    }

    private int GetCurrentDamage()
    {
        float pct = SkillData.damagePercent;
        for (int i = 0; i < currentUpgradeTier; i++) pct += SkillData.upgrades[i].damagePercentBonus;
        return Mathf.RoundToInt(StatsManager.instance.damage * pct);
    }

    // ====================================================================
    // CƠ CHẾ ĐỒNG BỘ ANIMATION THÔNG MINH
    // ====================================================================
    private IEnumerator WaitAnimationFinish()
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            // Tự động chờ đến khi cái Animation hiện tại chạy được 98%
            while (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.98f)
            {
                yield return null;
            }
        }
        
        var sm = movement.GetStateManager();
        if (sm != null) sm.ChangeState(PlayerState.Idle);
        isUsingSkill = false;
    }

    // ── Knight Skill ──
    private IEnumerator KnightSkillRoutine()
    {
        isUsingSkill = true;
        var sm = movement.GetStateManager();
        sm.ChangeState(PlayerState.KnightSkill); // Bật Animation

        yield return new WaitForSeconds(SkillData.chargeTime);

        float stepDelay = SkillData.slashVfxStepDelay;
        for (int i = 0; i < currentUpgradeTier; i++) stepDelay *= SkillData.upgrades[i].cooldownMultiplier;
        stepDelay = Mathf.Max(0.05f, stepDelay);

        int facingDir = transform.localScale.x > 0 ? 1 : -1;
        Vector2 dir = new Vector2(facingDir, 0f);
        int damage = GetCurrentDamage();
        float kbForce = StatsManager.instance.knockbackForce;
        float kbTime = StatsManager.instance.knockbackTime;
        float stun = StatsManager.instance.stunTime;
        LayerMask enemies = LayerMask.GetMask("Enemy");
        var alreadyHit = new HashSet<GameObject>();

        for (int i = 0; i < SkillData.slashVfxCount; i++)
        {
            float stepDist = SkillData.slashVfxStepDistance * (i + 1);
            Vector3 spawnPos = skillOrigin.position + (Vector3)(dir * stepDist);

            if (SkillData.slashVfxPrefab != null)
            {
                var vfx = Instantiate(SkillData.slashVfxPrefab, spawnPos, Quaternion.identity);
                vfx.transform.localScale = new Vector3(facingDir * 2.5f, 2.5f, 1f);
                Destroy(vfx, SkillData.slashVfxDuration);
            }
            SlashHitAtPoint(spawnPos, damage, kbForce, kbTime, stun, enemies, alreadyHit);

            if (i < SkillData.slashVfxCount - 1) yield return new WaitForSeconds(stepDelay);
        }

        yield return StartCoroutine(WaitAnimationFinish());
    }

    private void SlashHitAtPoint(Vector3 point, int damage, float kbForce, float kbTime, float stun, LayerMask enemies, HashSet<GameObject> alreadyHit)
    {
        var hits = Physics2D.OverlapCircleAll(point, SkillData.slashHitRadius, enemies);
        bool playedSfx = false;
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy") || alreadyHit.Contains(hit.gameObject)) continue;
            alreadyHit.Add(hit.gameObject);
            hit.GetComponent<IEnemy_Health>()?.ChangeHealth(-damage);
            hit.GetComponent<IEnemy_Movement>()?.KnockBack(transform, kbForce, kbTime, stun);

            if (ClassData.hitEffectPrefab != null) Instantiate(ClassData.hitEffectPrefab, hit.transform.position, Quaternion.identity, hit.transform);
            if (!playedSfx && ClassData.hitClip != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);
                playedSfx = true;
            }
        }
    }

    // ── Archer Skill ──
    private IEnumerator ArcherSkillRoutine()
    {
        isUsingSkill = true;
        var sm = movement.GetStateManager();
        sm.ChangeState(PlayerState.ArcherSkill); 

        yield return new WaitForSeconds(0.2f); // Chờ giương cung

        if (SkillData.arrowPrefab != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            Vector2 baseDir = ((Vector2)(mouseWorld - skillOrigin.position)).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            int count = SkillData.arrowCount;
            float fanAngle = SkillData.fanAngle;
            for (int i = 0; i < currentUpgradeTier; i++)
            {
                count += SkillData.upgrades[i].arrowCountBonus;
                fanAngle += SkillData.upgrades[i].fanAngleBonus;
            }

            float step = count > 1 ? fanAngle / (count - 1) : 0f;
            float startAngle = count > 1 ? baseAngle - (fanAngle / 2f) : baseAngle;
            var enemies = LayerMask.GetMask("Enemy");

            movement.FaceToward(mouseWorld.x);

            int damage = GetCurrentDamage();
            float kbForce = StatsManager.instance.knockbackForce;
            float kbTime = StatsManager.instance.knockbackTime;
            float stun = StatsManager.instance.stunTime;
            float spd = SkillData.arrowSpeed;
            float life = SkillData.arrowLifetime;
            AudioClip hitSfx = ClassData.hitClip;
            GameObject hitFx = ClassData.hitEffectPrefab;

            if (count > 200)
            {
                StartCoroutine(SpawnArrowBatch(count, step, startAngle, damage, kbForce, kbTime, stun, spd, life, hitSfx, hitFx, enemies, skillOrigin.position));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = startAngle + (step * i);
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                    var arrow = ArrowPool.Instance.Get();
                    arrow.transform.position = skillOrigin.position;
                    arrow.transform.rotation = Quaternion.identity;

                    arrow.Initialise(dir, spd, life, damage, kbForce, kbTime, stun, hitFx, hitSfx, volume, enemies);
                }
            }
        }

        yield return StartCoroutine(WaitAnimationFinish());
    }

    private const int ARROWS_PER_FRAME = 32;
    private IEnumerator SpawnArrowBatch(int count, float step, float startAngle, int damage, float kbForce, float kbTime, float stun, float spd, float life, AudioClip hitSfx, GameObject hitFx, LayerMask enemies, Vector3 origin)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var arrow = ArrowPool.Instance.Get();
            arrow.transform.position = origin;
            arrow.transform.rotation = Quaternion.identity;
            arrow.Initialise(dir, spd, life, damage, kbForce, kbTime, stun, hitFx, hitSfx, volume, enemies);

            if (i > 0 && i % ARROWS_PER_FRAME == 0) yield return null;
        }
    }

    // ── Rogue Skill ──
    private IEnumerator RogueSkillRoutine()
    {
        isUsingSkill = true;
        var sm = movement.GetStateManager();
        sm.ChangeState(PlayerState.RogueSkill); 

        float range = SkillData.teleportRange;
        for (int i = 0; i < currentUpgradeTier; i++) range += SkillData.upgrades[i].teleportRangeBonus;

        var hits = Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Enemy"));
        Collider2D closest = null;
        float minDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Enemy")) continue;
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < minDist) { minDist = d; closest = h; }
        }

        if (closest == null)
        {
            StatsManager.instance.currentMana += GetCurrentManaCost();
            cooldownTimer = 0f;
            sm.ChangeState(PlayerState.Idle);
            isUsingSkill = false;
            yield break;
        }

        if (SkillData.teleportFxPrefab != null) Instantiate(SkillData.teleportFxPrefab, transform.position, Quaternion.identity);

        Vector2 toEnemy = (((Vector2)closest.transform.position - (Vector2)transform.position).normalized) * -1;
        Vector2 behindEnemy = (Vector2)closest.transform.position - toEnemy * 1.3f;
        transform.position = behindEnemy;

        movement.FaceToward(closest.transform.position.x);

        yield return new WaitForSeconds(0.4f); // Chờ đâm lén (Timing chuẩn xác của đòn đánh)

        closest.GetComponent<IEnemy_Health>()?.ChangeHealth(-GetCurrentDamage());
        closest.GetComponent<IEnemy_Movement>()?.KnockBack(transform, StatsManager.instance.knockbackForce, StatsManager.instance.knockbackTime, StatsManager.instance.stunTime);

        if (ClassData.hitEffectPrefab != null) Instantiate(ClassData.hitEffectPrefab, closest.transform.position, Quaternion.identity, closest.transform);
        if (ClassData.hitClip != null) SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);
        if (SkillData.teleportFxPrefab != null) Instantiate(SkillData.teleportFxPrefab, transform.position, Quaternion.identity);

        yield return StartCoroutine(WaitAnimationFinish());
    }

    // ── Summoner Skill ──
    private IEnumerator SummonerSkillRoutine()
    {
        isUsingSkill = true;
        var sm = movement.GetStateManager();
        
        // Bật Animation niệm chú của Summoner
        sm.ChangeState(PlayerState.SummonerSkill); 

        // Tìm Alicia đang đứng ở đâu trên màn hình
        NPCCompanion alicia = FindObjectOfType<NPCCompanion>();

        if (alicia != null && SkillData.summonerBulletPrefab != null)
        {
            if (ClassData.swingClip != null) SoundFXManager.Instance.PlaySoundFXClip(ClassData.swingClip, transform, volume);

            Vector3 origin = alicia.transform.position; // Lấy tâm phát nổ là Alicia
            int count = SkillData.summonerBulletCount;
            int damage = GetCurrentDamage();
            
            // Nếu có nâng cấp Skill, tăng thêm số lượng đạn (Mỗi Tier + 4 viên)
            for (int i = 0; i < currentUpgradeTier; i++) count += 4; 

            float angleStep = 360f / count; // Chia đều góc 360 độ

            // Bắn đạn tỏa ra xung quanh Alicia
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject bullet = Instantiate(SkillData.summonerBulletPrefab, origin, Quaternion.identity);
                CompanionProjectile projScript = bullet.GetComponent<CompanionProjectile>();
                
                if (projScript != null) 
                {
                    // Truyền sát thương của Player vào đạn. "false" = Không làm hại Player
                    projScript.Setup(dir, damage, false); 
                }
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Alicia hoặc chưa kéo Prefab đạn vào SkillData của Summoner!");
            // Trả lại mana nếu bấm xịt (giống Rogue)
            StatsManager.instance.currentMana += GetCurrentManaCost();
            cooldownTimer = 0f;
        }

        // Đợi Animation niệm chú của Summoner xong mới cho di chuyển tiếp
        yield return StartCoroutine(WaitAnimationFinish());
    }
}