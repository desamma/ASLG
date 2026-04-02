using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayne_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_ArgeonHighmayne_Health health;
    [SerializeField] private bool isKnockbackable = false;
    [SerializeField] private float auraFarmingDuration = 5f;

    private StateManager<Enemy_ArgeonHighmayne_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_ArgeonHighmayne_State>> attackCategories;

    [Header("Death Effects")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSoundEffect;
    [SerializeField] private GameObject phase2EntranceEffect;
    [SerializeField] private float delayToPhase2 = 4f;

    [Header("WarSurgeTeleportAttack")]
    [SerializeField] private GameObject warSurgeMarkEffect;
    [SerializeField] private GameObject warSurgeChargeUpEffect;
    [SerializeField] private GameObject warSurgeAfterTPEffect;
    [SerializeField] private float warSurgeSearchRadius = 50f;
    [SerializeField] private float warSurgeWaitTime = 3f;
    private Transform warSurgeTarget;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private float castRange;
    private float attackCooldownTimer = 0f;
    private float warSurgeWaitTimer = 0f;
    private bool isAuraFarming = false;

    private EnemyMoveCooldownTracker<Enemy_ArgeonHighmayne_State> moveCooldowns = new();
    private KnockbackHandler knockbackHandler;
    private EnemyAttackRecovery attackRecovery;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_ArgeonHighmayne_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    // enemy movement helper
    public Transform PlayerTransform { get; set; }
    public bool IsRecovering { get; set; }
    public int FacingDirection { get; set; }

    // readonly properties for helper
    public Rigidbody2D Rb => rb;
    public BehaviorProfile Behavior => health.behavior;
    public EnemyStats Stats => health.stats;
    public Transform DetectionPoint => detectionPoint;
    public LayerMask PlayerLayer => playerLayer;
    public Transform SelfTransform => transform;

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_ArgeonHighmayne_State.Attack) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.WarSurge) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.SunBloom) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.EntropicDecay) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.Decimated) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.AurynNexus);
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayne_Health>();

        castRange = health.stats.AttackRange * 2f;

        IsRecovering = false;
        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeAttacks();

        stateManager = new StateManager<Enemy_ArgeonHighmayne_State>(animator, Enemy_ArgeonHighmayne_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;

        knockbackHandler = new KnockbackHandler(this, rb);
        attackRecovery = new EnemyAttackRecovery(this, rb);

        StartCoroutine(AuraFarming());
    }

    private IEnumerator AuraFarming()
    {
        isAuraFarming = true;
        charCollider.enabled = false;
        rb.velocity = Vector2.zero;
        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);

        yield return new WaitForSeconds(auraFarmingDuration);
        isAuraFarming = false;
        charCollider.enabled = true;
    }

    private void Update()
    {
        if (health.isDead || isAuraFarming || IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (warSurgeWaitTimer > 0)
            warSurgeWaitTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_ArgeonHighmayne_State.Chase))
            Chase();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
        }
    }

    public void Chase()
    {
        EnemyMovementHelper.Chase(this,
            OnEnterAttackRange: () =>
            {
                if (!IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Attack);
                    RegisterMoveUsed(Enemy_ArgeonHighmayne_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (!IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
                }
            });
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this, distanceToPlayer =>
        {
            if (distanceToPlayer <= castRange)
            {
                ChangeToChaseState();

                if (distanceToPlayer <= Stats.AttackRange && !IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Attack);
                    RegisterMoveUsed(Enemy_ArgeonHighmayne_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (attackCooldownTimer <= 0 && !IsInAnyAttackState())
                {
                    DecideAttackType();
                    attackCooldownTimer = Stats.AttackCooldown;
                }
            }
            else if (!IsInAnyAttackState())
            {
                ChangeToChaseState();
            }
        });
    }

    private void DecideAttackType()
    {
        EnemyMovementHelper.DecideAttackType(this, attackCategories, moveCooldowns,
            OnPriorityCodeBeforeRoll: () =>
            {
                if (warSurgeTarget != null && warSurgeWaitTimer <= 0 && !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.WarSurge);
                    PlayerTransform = warSurgeTarget;
                    warSurgeTarget = null;
                    RegisterMoveUsed(Enemy_ArgeonHighmayne_State.WarSurge);
                    return;
                }
            },
            OnAttackSelected: state =>
            {
                if (state == Enemy_ArgeonHighmayne_State.WarSurge && warSurgeTarget == null)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
                    if (distanceToPlayer <= warSurgeSearchRadius)
                    {
                        warSurgeTarget = PlayerTransform;
                        Vector3 position = PlayerTransform.position + new Vector3(0, 1.8f, 0);
                        Instantiate(warSurgeMarkEffect, position, Quaternion.identity, PlayerTransform.transform);
                        warSurgeWaitTimer = warSurgeWaitTime;
                    }
                    return;
                }

                stateManager.ChangeState(state);
                RegisterMoveUsed(state);
            });
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
                IsRecovering = true;
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeAttacks()
    {
        attackCategories = new List<AttackCategory<Enemy_ArgeonHighmayne_State>>
        {
            new() {
                Frequency = Behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig<Enemy_ArgeonHighmayne_State> { State = Enemy_ArgeonHighmayne_State.Decimated,   Range = castRange, MoveCountCooldown = 5 },

                    new AttackConfig<Enemy_ArgeonHighmayne_State> { State = Enemy_ArgeonHighmayne_State.AurynNexus,  Range = castRange, MoveCountCooldown = 5 }
                }
            },
            new() {
                Frequency = Behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig<Enemy_ArgeonHighmayne_State> { State = Enemy_ArgeonHighmayne_State.SunBloom, Range = castRange, MoveCountCooldown = 4 },
                }
            },
            new() {
                Frequency = Behavior.SpecialAttackFrequency * Behavior.MobilityUsageFrequency,
                Attacks = new[]
                {
                    new AttackConfig<Enemy_ArgeonHighmayne_State> { State = Enemy_ArgeonHighmayne_State.WarSurge, Range = Stats.AttackRange, MoveCountCooldown = 3 },
                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_ArgeonHighmayne_State> { State = Enemy_ArgeonHighmayne_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => a.State));
    }

    public void PlayWarSurgeChargeUpEffect()
    {
        Vector3 effectPosition = transform.position + new Vector3(0, -1f, 0);
        Instantiate(warSurgeChargeUpEffect, effectPosition, Quaternion.identity);
    }

    private void WarSurgeTP()
    {
        if (PlayerTransform == null) return;
        float playerFacingDirection = PlayerTransform.localScale.x > 0 ? 1f : -1f;
        float offsetDistance = 1.5f;
        Vector3 targetPosition = PlayerTransform.position + new Vector3(-playerFacingDirection * offsetDistance, 0f, 0f);
        StartCoroutine(WarSurgeTPWaitTime(targetPosition));
    }

    private IEnumerator WarSurgeTPWaitTime(Vector3 targetPosition)
    {
        yield return new WaitForSeconds(warSurgeWaitTime);

        transform.position = targetPosition;
        Vector3 effectPosition = transform.position + new Vector3(0, -1f, 0);
        Instantiate(warSurgeAfterTPEffect, effectPosition, Quaternion.identity);

        if (PlayerTransform != null)
            FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
    }

    #region State Callbacks

    private void OnStateEnter(Enemy_ArgeonHighmayne_State state)
    {
        switch (state)
        {
            case Enemy_ArgeonHighmayne_State.Attack:
                rb.velocity = Vector2.zero;
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_ArgeonHighmayne_State.WarSurge:
                rb.velocity = Vector2.zero;
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                WarSurgeTP();
                break;
            case Enemy_ArgeonHighmayne_State.SunBloom:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Decimated:
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.AurynNexus:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    #endregion

    public void PlayLastJudgement()
    {
        var location = transform.position + new Vector3(0, 1.28f, 0);
        Instantiate(deathEffect, location, Quaternion.identity);
        SoundFXManager.Instance.PlaySoundFXClip(deathSoundEffect, transform, 1f);
        StartCoroutine(DeathToPhase2());
    }
    private IEnumerator DeathToPhase2()
    {
        yield return new WaitForSeconds(delayToPhase2);
        Destroy(gameObject);
        var location = transform.position + new Vector3(0, 1.8f, 0);
        Instantiate(phase2EntranceEffect, location, Quaternion.identity);
    }

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Knockback),
            () => stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Chase))
            stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Idle))
            stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_ArgeonHighmayne_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion

}