using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkill : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Transform skillOrigin; // tip of weapon / spawn point

    [Header("Audio")]
    [SerializeField] private float volume = 1f;

    [Header("Debug (read only)")]
    [SerializeField] private int currentUpgradeTier = 0;
    [SerializeField] private float cooldownTimer = 0f;

    private bool isUsingSkill = false;

    private PlayerClassData ClassData => ClassManager.Instance?.CurrentClassData;
    private SkillData SkillData => ClassData?.skillData;

    // ── Unity ─────────────────────────────────────────────────────────────────

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

    // ── Conditions ────────────────────────────────────────────────────────────

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

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void TriggerSkill()
    {
        StatsManager.instance.currentMana -= GetCurrentManaCost();
        cooldownTimer = GetCurrentCooldown();

        switch (ClassManager.Instance.SelectedClass)
        {
            case PlayerClass.Knight: StartCoroutine(KnightSkillRoutine()); break;
            case PlayerClass.Archer: ArcherSkill(); break;
            case PlayerClass.Rogue: StartCoroutine(RogueSkillRoutine()); break;
        }
    }

    // ── Upgrade ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Call from a UI button. Returns true if upgrade succeeded.
    /// </summary>
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

    // Exposed for UI
    public Sprite SkillIcon => SkillData.icon;
    public int CurrentTier => currentUpgradeTier;
    public int MaxTier => SkillData.upgrades?.Length ?? 0;
    public float CooldownFraction => SkillData != null ? cooldownTimer / GetCurrentCooldown() : 0f;
    public float CooldownTimer => cooldownTimer;
    public float ManaCost => GetCurrentManaCost();
    public int NextUpgradeCost => (SkillData != null && currentUpgradeTier < MaxTier)
                                     ? SkillData.upgrades[currentUpgradeTier].pointCost : 0;

    // ── Computed stats (stack upgrade bonuses) ────────────────────────────────

    private float GetCurrentManaCost()
    {
        float v = SkillData.manaCost;
        for (int i = 0; i < currentUpgradeTier; i++)
            v *= SkillData.upgrades[i].manaCostMultiplier;
        return v;
    }

    private float GetCurrentCooldown()
    {
        float v = SkillData.cooldown;
        for (int i = 0; i < currentUpgradeTier; i++)
            v *= SkillData.upgrades[i].cooldownMultiplier;
        return Mathf.Max(0.5f, v);
    }

    private int GetCurrentDamage()
    {
        float pct = SkillData.damagePercent;
        for (int i = 0; i < currentUpgradeTier; i++)
            pct += SkillData.upgrades[i].damagePercentBonus;
        return Mathf.RoundToInt(StatsManager.instance.damage * pct);
    }

    // ── Knight: charged sword slash ───────────────────────────────────────────

    private IEnumerator KnightSkillRoutine()
    {
        isUsingSkill = true;
        var sm = movement.GetStateManager();
        sm.ChangeState(PlayerState.KnightSkill);

        yield return new WaitForSeconds(SkillData.chargeTime);

        float stepDelay = SkillData.slashVfxStepDelay;
        for (int i = 0; i < currentUpgradeTier; i++)
            stepDelay *= SkillData.upgrades[i].cooldownMultiplier;
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

            if (i < SkillData.slashVfxCount - 1)
                yield return new WaitForSeconds(stepDelay);
        }

        sm.ChangeState(PlayerState.Idle);
        isUsingSkill = false;
    }

    private void SlashHitAtPoint(
        Vector3 point, int damage,
        float kbForce, float kbTime, float stun,
        LayerMask enemies, HashSet<GameObject> alreadyHit)
    {
        var hits = Physics2D.OverlapCircleAll(point, SkillData.slashHitRadius, enemies);

        bool playedSfx = false;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (alreadyHit.Contains(hit.gameObject)) continue;

            alreadyHit.Add(hit.gameObject);

            hit.GetComponent<IEnemy_Health>()?.ChangeHealth(-damage);
            hit.GetComponent<IEnemy_Movement>()?.KnockBack(
                transform, kbForce, kbTime, stun);

            if (ClassData.hitEffectPrefab != null)
                Instantiate(ClassData.hitEffectPrefab,
                            hit.transform.position, Quaternion.identity, hit.transform);

            if (!playedSfx && ClassData.hitClip != null)
            {
                SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);
                playedSfx = true;
            }
        }
    }

    // ── Archer: fan shot ──────────────────────────────────────────────────────

    private void ArcherSkill()
    {
        if (SkillData.arrowPrefab == null)
        {
            Debug.LogWarning("[PlayerSkill] Archer skill has no arrow prefab.");
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 baseDir = ((Vector2)(mouseWorld - skillOrigin.position)).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // Apply upgrade bonuses
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

        // Flip player toward mouse
        movement.FaceToward(mouseWorld.x);

        const int MAX_ARROWS = 200;
        if (count > MAX_ARROWS)
        {
            Debug.LogWarning($"[PlayerSkill] Arrow count excese {MAX_ARROWS}.");
            // Cache every value that would otherwise be recomputed per-arrow
            int damage = GetCurrentDamage();
            float kbForce = StatsManager.instance.knockbackForce;
            float kbTime = StatsManager.instance.knockbackTime;
            float stun = StatsManager.instance.stunTime;
            float spd = SkillData.arrowSpeed;
            float life = SkillData.arrowLifetime;
            AudioClip hitSfx = ClassData.hitClip;
            GameObject hitFx = ClassData.hitEffectPrefab;
            Vector3 origin = skillOrigin.position;

            StartCoroutine(SpawnArrowBatch(
                count, step, startAngle,
                damage, kbForce, kbTime, stun,
                spd, life, hitSfx, hitFx, enemies, origin));
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

                arrow.Initialise(
                    dir: dir,
                    spd: SkillData.arrowSpeed,
                    life: SkillData.arrowLifetime,
                    dmg: GetCurrentDamage(),
                    kbForce: StatsManager.instance.knockbackForce,
                    kbTime: StatsManager.instance.knockbackTime,
                    stun: StatsManager.instance.stunTime,
                    hitFx: ClassData.hitEffectPrefab,
                    hitSfx: ClassData.hitClip,
                    vol: volume,
                    enemies: enemies);
            }
        }
    }

    private const int ARROWS_PER_FRAME = 32;

    private IEnumerator SpawnArrowBatch(
        int count, float step, float startAngle,
        int damage, float kbForce, float kbTime, float stun,
        float spd, float life, AudioClip hitSfx, GameObject hitFx,
        LayerMask enemies, Vector3 origin)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var arrow = ArrowPool.Instance.Get();
            arrow.transform.position = origin;
            arrow.transform.rotation = Quaternion.identity;
            arrow.Initialise(dir, spd, life, damage, kbForce, kbTime, stun,
                             hitFx, hitSfx, volume, enemies);

            if (i > 0 && i % ARROWS_PER_FRAME == 0)
                yield return null;
        }
    }

    // ── Rogue: shadow step ────────────────────────────────────────────────────

    private IEnumerator RogueSkillRoutine()
    {
        isUsingSkill = true;

        float range = SkillData.teleportRange;
        for (int i = 0; i < currentUpgradeTier; i++)
            range += SkillData.upgrades[i].teleportRangeBonus;

        // Find closest enemy in range
        var hits = Physics2D.OverlapCircleAll(transform.position, range,
                          LayerMask.GetMask("Enemy"));
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
            // No target — refund mana
            StatsManager.instance.currentMana += GetCurrentManaCost();
            cooldownTimer = 0f;
            isUsingSkill = false;
            Debug.Log("[PlayerSkill] Rogue: no enemy in range, skill refunded.");
            yield break;
        }

        // Spawn teleport FX at origin
        if (SkillData.teleportFxPrefab != null)
            Instantiate(SkillData.teleportFxPrefab, transform.position, Quaternion.identity);

        // Teleport behind the enemy relative to player's approach direction
        Vector2 toEnemy = (((Vector2)closest.transform.position - (Vector2)transform.position).normalized) * -1;
        Vector2 behindEnemy = (Vector2)closest.transform.position - toEnemy * 1.3f;
        transform.position = behindEnemy;

        // Face toward the enemy from behind
        movement.FaceToward(closest.transform.position.x);

        yield return null; // let physics catch up

        // Deal damage
        closest.GetComponent<IEnemy_Health>()?.ChangeHealth(-GetCurrentDamage());
        closest.GetComponent<IEnemy_Movement>()?.KnockBack(
            transform,
            StatsManager.instance.knockbackForce,
            StatsManager.instance.knockbackTime,
            StatsManager.instance.stunTime);

        if (ClassData.hitEffectPrefab != null)
            Instantiate(ClassData.hitEffectPrefab,
                        closest.transform.position, Quaternion.identity, closest.transform);

        if (ClassData.hitClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);

        // Spawn teleport FX at destination
        if (SkillData.teleportFxPrefab != null)
            Instantiate(SkillData.teleportFxPrefab, transform.position, Quaternion.identity);

        isUsingSkill = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (SkillData != null && ClassManager.Instance?.SelectedClass == PlayerClass.Knight)
        {
            int facingDir = transform.localScale.x > 0 ? 1 : -1;
            Vector2 dir = new Vector2(facingDir, 0f);

            for (int i = 0; i < SkillData.slashVfxCount; i++)
            {
                float t = (i + 1) * SkillData.slashVfxStepDistance;
                Vector3 pos = skillOrigin != null
                    ? skillOrigin.position + (Vector3)(dir * t)
                    : transform.position + (Vector3)(dir * t);

                float alpha = 1f - (i / (float)SkillData.slashVfxCount) * 0.4f;
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f * alpha);
                Gizmos.DrawSphere(pos, SkillData.slashHitRadius);
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f * alpha);
                Gizmos.DrawWireSphere(pos, SkillData.slashHitRadius);
            }
        } else if (SkillData != null && ClassManager.Instance?.SelectedClass == PlayerClass.Rogue)
        {
            float range = SkillData.teleportRange;
            for (int i = 0; i < currentUpgradeTier && i < SkillData.upgrades.Length; i++)
                range += SkillData.upgrades[i].teleportRangeBonus;

            Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.2f);
            Gizmos.DrawSphere(transform.position, range);
            Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
#endif
}