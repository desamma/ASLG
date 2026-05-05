using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Coalfist_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Coalfist_Health health;
    [SerializeField] private bool isKnockbackable = true;

    private StateManager<Enemy_Coalfist_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_Coalfist_State>> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    private Vector2 originalPosition;

    [Header("Patrol Settings")]
    [SerializeField] private float idleToPatrolWaitTime = 5f;
    private Vector2[] patrolPoints;
    public float patrolDistance = 3f;
    int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private float stuckCheckTimer = 0f;
    private Vector2 lastCheckedPosition;
    private float waitTimer = 0f;

    //private
    private Collider2D charCollider;
    private float attackCooldownTimer = 0f;
    private EnemyMoveCooldownTracker<Enemy_Coalfist_State> moveCooldowns = new();
    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

    // enemy movement helper
    public Transform PlayerTransform { get; set; }
    public bool IsRecovering { get; set; }
    public int FacingDirection { get; set; }

    //readonly properties for helper
    public Rigidbody2D Rb => rb;
    public BehaviorProfile Behavior => health.behavior;
    public EnemyStats Stats => health.stats;
    public Transform DetectionPoint => detectionPoint;
    public LayerMask PlayerLayer => playerLayer;
    public Transform SelfTransform => transform;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_Coalfist_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_Coalfist_State.Chase))
            stateManager.ChangeState(Enemy_Coalfist_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_Coalfist_State.Idle))
            stateManager.ChangeState(Enemy_Coalfist_State.Idle);
    }

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_Coalfist_State.Attack) ||
               stateManager.IsInState(Enemy_Coalfist_State.BackStepAttack);
    }

    private void Start()
    {
        originalPosition = transform.position;

        patrolPoints = new Vector2[4];
        patrolPoints[0] = originalPosition + Vector2.up * patrolDistance;
        patrolPoints[1] = originalPosition + Vector2.down * patrolDistance;
        patrolPoints[2] = originalPosition + Vector2.left * patrolDistance;
        patrolPoints[3] = originalPosition + Vector2.right * patrolDistance;
        lastCheckedPosition = originalPosition;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_Coalfist_Health>();

        IsRecovering = false;
        FacingDirection = 1;

        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        InitializeAttacks();

        stateManager = new StateManager<Enemy_Coalfist_State>(animator, Enemy_Coalfist_State.Idle);

        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (IsInAnyAttackState()) return;

        if (!stateManager.IsInState(Enemy_Coalfist_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_Coalfist_State.Chase))
            Chase();

        else if (stateManager.IsInState(Enemy_Coalfist_State.Patrol))
            Patrol();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
        }
    }

    public void Chase()
    {
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    private void Patrol()
    {
        EnemyMovementHelper.Patrol(this, patrolPoints, ref currentPatrolIndex, ref isWaiting, ref waitTimer,
            ref stuckCheckTimer, ref lastCheckedPosition,
            idleToPatrolWaitTime,
            unstuckCheckInterval: 1.0f,   // check every 1 second
            stuckThreshold: 0.3f,          // must move at least 0.3 units per check
            () => stateManager.ChangeState(Enemy_Coalfist_State.Idle),
            () => stateManager.ChangeState(Enemy_Coalfist_State.Patrol));
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this, distanceToPlayer =>
        {
            if (distanceToPlayer <= Stats.AttackRange && !IsInAnyAttackState() && !IsRecovering)
            {
                if (attackCooldownTimer <= 0 && !IsInAnyAttackState())
                {
                    DecideAttackType();
                    attackCooldownTimer = Stats.AttackCooldown;
                }
            }
            else if (!IsInAnyAttackState())
            {
                ChangeToChaseState();
            }
        }, true,
        OnPatrolInsteadOfIdle: () =>
        {
            if (!stateManager.IsInState(Enemy_Coalfist_State.Patrol) &&
                !IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_Coalfist_State.Patrol);
            }
        });
    }

    private void DecideAttackType()
    {
        EnemyMovementHelper.DecideAttackType(this, attackCategories, moveCooldowns,
            OnAttackSelected: state =>
            {
                stateManager.ChangeState(state);
                RegisterMoveUsed(state);
            },
            extraFilter: BackStepAttackCondition
        );
    }

    private bool BackStepAttackCondition(Enemy_Coalfist_State state)
    {
        if (PlayerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
            float closeThreshold = Stats.AttackRange * 0.5f;

            // If player is NOT close and we're evaluating Attack, filter it out (prefer BackStepAttack when close)
            if (distanceToPlayer <= closeThreshold && state == Enemy_Coalfist_State.Attack)
                return false;

            // If player is far and we're evaluating BackStepAttack, filter it out (prefer Attack when far)
            if (distanceToPlayer > closeThreshold && state == Enemy_Coalfist_State.BackStepAttack)
                return false;
        }

        return true;
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_Coalfist_State.Idle);
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeAttacks()
    {
        attackCategories = new List<AttackCategory<Enemy_Coalfist_State>>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_Coalfist_State> { State = Enemy_Coalfist_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 },

                    new AttackConfig<Enemy_Coalfist_State> { State = Enemy_Coalfist_State.BackStepAttack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }

    #region State Callbacks
    private void OnStateEnter(Enemy_Coalfist_State state)
    {
        switch (state)
        {
            case Enemy_Coalfist_State.Attack:
                transform.position += new Vector3(FacingDirection, 0); // small lunge forward
                rb.velocity = Vector2.zero;
                isKnockbackable = false;
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_Coalfist_State.BackStepAttack:
                transform.position += new Vector3(-FacingDirection, 0);
                rb.velocity = Vector2.zero;
                isKnockbackable = false;
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_Coalfist_State.Death:
                rb.velocity = Vector2.zero;
                charCollider = GetComponents<Collider2D>().FirstOrDefault(c => c.enabled);
                if (charCollider != null)
                    charCollider.enabled = false;
                StopAllCoroutines();
                break;
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(Enemy_Coalfist_State state)
    {
        switch (state)
        {
            case Enemy_Coalfist_State.Attack:
                isKnockbackable = true;
                break;
            case Enemy_Coalfist_State.BackStepAttack:
                isKnockbackable = true;
                break;
        }
    }

    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_Coalfist_State.Knockback),
            () =>
            {
                stateManager.ChangeState(Enemy_Coalfist_State.Idle);
            });
    }

    #region Getters
    public StateManager<Enemy_Coalfist_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}
