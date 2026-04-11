using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;

    [Header("Attack Origin")]
    [SerializeField] private Transform attackPoint;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Audio")]
    [SerializeField] private float volume = 1f;

    private bool hitEnemy = false;
    private float attackCooldownTimer = 0f;

    // Cached class data — refreshed whenever ApplyClass() is called
    private PlayerClassData ClassData => ClassManager.Instance?.CurrentClassData;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (enemyLayer == 0) enemyLayer = LayerMask.GetMask("Enemy");

        // Apply whichever class was selected before this scene loaded
        ClassManager.Instance?.ApplyToStatsManager();
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        HandleAttackInput();
    }

    // ── Input / Trigger ───────────────────────────────────────────────────────

    private void HandleAttackInput()
    {
        if (Input.GetButtonDown("Slash") && CanAttack())
            TriggerAttack();
    }

    private bool CanAttack()
    {
        var sm = movement.GetStateManager();
        return sm != null &&
               attackCooldownTimer <= 0f &&
               !sm.IsInState(PlayerState.Attack) &&
               !sm.IsInState(PlayerState.Knockback) &&
               !sm.IsInState(PlayerState.Death) &&
               !sm.IsInState(PlayerState.Dash);
    }

    private void TriggerAttack()
    {
        attackCooldownTimer = StatsManager.instance.cooldown;

        // Flip toward mouse before locking into Attack state

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        movement.FaceToward(mouseWorld.x);
        
        //TODO: change attack state for each class
        movement.GetStateManager().ChangeState(PlayerState.Attack);

        if (ClassData?.swingClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(ClassData.swingClip, transform, volume);
    }

    // ── Animation Event: called at the hit frame ──────────────────────────────

    /// <summary>
    /// Called by animation event. Routes to melee or ranged based on current class.
    /// </summary>
    public void OnAttackHitFrame()
    {
        if (ClassData == null) return;

        switch (ClassData.attackType)
        {
            case AttackType.Melee: ExecuteMeleeAttack(); break;
            case AttackType.Ranged: ExecuteRangedAttack(); break;
        }
    }

    // ── Melee (Knight / Rogue) ────────────────────────────────────────────────

    private void ExecuteMeleeAttack()
    {
        int combinedLayer = enemyLayer | LayerMask.GetMask("Player", "Default", "NPC");
        var hits = Physics2D.OverlapCircleAll(attackPoint.position,
                                              StatsManager.instance.weaponRange,
                                              combinedLayer);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.CompareTag("Enemy"))
            {
                HandleEnemyHit(hit);
            }
            else
            {
                var npc = hit.GetComponent<NPCCompanion>();
                npc?.TakeDamage(StatsManager.instance.damage, true);
                SpawnHitEffect(hit.transform.position, hit.transform);
            }
        }

        hitEnemy = false;
    }

    private void HandleEnemyHit(Collider2D hit)
    {
        if (!hitEnemy)
        {
            if (ClassData?.hitClip != null)
                SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);
            hitEnemy = true;
        }

        SpawnHitEffect(hit.transform.position, hit.transform);

        hit.GetComponent<IEnemy_Health>()?.ChangeHealth(-StatsManager.instance.damage);
        hit.GetComponent<IEnemy_Movement>()?.KnockBack(
            transform,
            StatsManager.instance.knockbackForce,
            StatsManager.instance.knockbackTime,
            StatsManager.instance.stunTime);
    }

    // ── Ranged (Archer) ───────────────────────────────────────────────────────

    private void ExecuteRangedAttack()
    {
        if (ClassData?.projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerAttack] Archer class has no projectile prefab assigned.");
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 fireDir = ((Vector2)(mouseWorld - attackPoint.position)).normalized;

        var go = Instantiate(ClassData.projectilePrefab,
                             attackPoint.position,
                             Quaternion.identity);

        var arrow = go.GetComponent<ArrowProjectile>();
        if (arrow == null)
        {
            Debug.LogWarning("[PlayerAttack] Projectile prefab has no ArrowProjectile component.");
            Destroy(go);
            return;
        }

        arrow.Initialise(
            dir: fireDir,
            spd: ClassData.projectileSpeed,
            life: StatsManager.instance.weaponRange,
            dmg: StatsManager.instance.damage,
            kbForce: StatsManager.instance.knockbackForce,
            kbTime: StatsManager.instance.knockbackTime,
            stun: StatsManager.instance.stunTime,
            hitFx: ClassData.hitEffectPrefab,
            hitSfx: ClassData.hitClip,
            vol: volume,
            enemies: enemyLayer
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SpawnHitEffect(Vector3 position, Transform parent)
    {
        if (ClassData?.hitEffectPrefab != null)
            Instantiate(ClassData.hitEffectPrefab, position, Quaternion.identity, parent);
    }

    public void OnAttackAnimationComplete()
    {
        var sm = movement.GetStateManager();
        if (sm != null && sm.IsInState(PlayerState.Attack))
            sm.ChangeState(PlayerState.Idle);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        float range = Application.isPlaying && StatsManager.instance != null
            ? StatsManager.instance.weaponRange
            : 1f;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawSphere(attackPoint.position, range);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(attackPoint.position, range);
    }
#endif
}