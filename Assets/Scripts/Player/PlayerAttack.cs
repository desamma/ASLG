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

    private PlayerClassData ClassData => ClassManager.Instance.CurrentClassData;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (enemyLayer == 0) enemyLayer = LayerMask.GetMask("Enemy");

        ClassManager.Instance.ApplyToStatsManager();
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;
        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (Input.GetButtonDown("Slash") && CanAttack() && Time.timeScale != 0f) TriggerAttack();
    }

    private bool CanAttack()
    {
        var sm = movement.GetStateManager();
        return sm != null && attackCooldownTimer <= 0f && !sm.IsInState(PlayerState.Attack) && !sm.IsInState(PlayerState.Knockback) && !sm.IsInState(PlayerState.Death) && !sm.IsInState(PlayerState.Dash);
    }

    private void TriggerAttack()
    {
        attackCooldownTimer = StatsManager.instance.cooldown;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        movement.FaceToward(mouseWorld.x);

        movement.GetStateManager().ChangeState(PlayerState.Attack);

        if (ClassData.swingClip != null) SoundFXManager.Instance.PlaySoundFXClip(ClassData.swingClip, transform, volume);
    }

    public void OnAttackHitFrame()
    {
        if (ClassData == null) return;
        switch (ClassData.attackType)
        {
            case AttackType.Melee: ExecuteMeleeAttack(); break;
            case AttackType.Ranged: ExecuteRangedAttack(); break;
        }
    }

    private void ExecuteMeleeAttack()
    {
        int combinedLayer = enemyLayer | LayerMask.GetMask("Player", "Default", "NPC", "Ally", "Companion");
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, StatsManager.instance.weaponRange, combinedLayer);
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            var npc = hit.GetComponentInParent<NPCCompanion>();
            if (npc != null)
            {
                npc.TakeDamage(StatsManager.instance.damage, true);
                SpawnHitEffect(hit.transform.position, hit.transform);
                continue;
            }
            if (hit.CompareTag("Enemy")) HandleEnemyHit(hit);
        }
        hitEnemy = false;
    }

    private void HandleEnemyHit(Collider2D hit)
    {
        if (!hitEnemy)
        {
            if (ClassData?.hitClip != null) SoundFXManager.Instance.PlaySoundFXClip(ClassData.hitClip, transform, volume);
            hitEnemy = true;
        }
        SpawnHitEffect(hit.transform.position, hit.transform);
        hit.GetComponent<IEnemy_Health>()?.ChangeHealth(-StatsManager.instance.damage);
        hit.GetComponent<IEnemy_Movement>()?.KnockBack(transform, StatsManager.instance.knockbackForce, StatsManager.instance.knockbackTime, StatsManager.instance.stunTime);
    }

    private void ExecuteRangedAttack()
    {
        if (ClassData?.projectilePrefab == null) return;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 fireDir = ((Vector2)(mouseWorld - attackPoint.position)).normalized;
        var go = Instantiate(ClassData.projectilePrefab, attackPoint.position, Quaternion.identity);
        var arrow = go.GetComponent<ArrowProjectile>();
        if (arrow == null) { Destroy(go); return; }
        arrow.Initialise(fireDir, ClassData.projectileSpeed, StatsManager.instance.weaponRange, StatsManager.instance.damage, StatsManager.instance.knockbackForce, StatsManager.instance.knockbackTime, StatsManager.instance.stunTime, ClassData.hitEffectPrefab, ClassData.hitClip, volume, enemyLayer | LayerMask.GetMask("Player", "Default", "NPC", "Ally", "Companion"));
    }

    private void SpawnHitEffect(Vector3 position, Transform parent)
    {
        if (ClassData?.hitEffectPrefab != null) Instantiate(ClassData.hitEffectPrefab, position, Quaternion.identity, parent);
    }

    public void OnAttackAnimationComplete()
    {
        var sm = movement.GetStateManager();
        if (sm != null && sm.IsInState(PlayerState.Attack)) sm.ChangeState(PlayerState.Idle);
    }
}