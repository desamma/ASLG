using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_PeaceKeeper_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_PeaceKeeper_Health health;
    [SerializeField] private bool isKnockbackable = true;

    private StateManager<Enemy_PeaceKeeper_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private List<AttackCategory<Enemy_PeaceKeeper_State>> attackCategories;

    [Header("Audio")]
    [SerializeField] private float volume;

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

    [Header("Aggression Burst")]
    [SerializeField] private float aggressionTriggerRange = 4f;
    [SerializeField] private float aggressionLow = 0.7f;
    [SerializeField] private float aggressionHigh = 3f;
    [SerializeField] private float aggressionRampDuration = 2f;
    [SerializeField] private float aggressionCooldown = 5f;

    //private
    private float attackCooldownTimer = 0f;
    private bool isAttacked = false;
    private float baseAggression = 1f;
    private float aggressionCooldownTimer = 0f;
    private Coroutine aggressionBurstCoroutine;
    private Color originalColor;
    private bool hasOriginalColor = false;


    private EnemyMoveCooldownTracker<Enemy_PeaceKeeper_State> moveCooldowns = new();
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
    private void RegisterMoveUsed(Enemy_PeaceKeeper_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_PeaceKeeper_State.Chase))
            stateManager.ChangeState(Enemy_PeaceKeeper_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_PeaceKeeper_State.Idle))
            stateManager.ChangeState(Enemy_PeaceKeeper_State.Idle);
    }

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_PeaceKeeper_State.Attack);
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

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        if (health == null)
            health = GetComponent<Enemy_PeaceKeeper_Health>();

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

        stateManager = new StateManager<Enemy_PeaceKeeper_State>(animator, Enemy_PeaceKeeper_State.Idle);

        stateManager.OnStateEnter += OnStateEnter;

        baseAggression = Behavior.Aggression;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            hasOriginalColor = true;
        }
        health.IsAttacked += OnIsAttacked;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (aggressionCooldownTimer > 0)
            aggressionCooldownTimer -= Time.deltaTime;

        if (!isAttacked) return;

        if (IsInAnyAttackState()) return;

        if (!stateManager.IsInState(Enemy_PeaceKeeper_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
            TryStartAggressionBurst();
        }

        if (stateManager.IsInState(Enemy_PeaceKeeper_State.Chase))
            Chase();

        else if (stateManager.IsInState(Enemy_PeaceKeeper_State.Patrol))
            Patrol();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
        }

        if (health != null)
            health.IsAttacked -= OnIsAttacked;
    }

    private void OnIsAttacked()
    {
        if (health.isDead) return;

        isAttacked = true;
    }

    private void TryStartAggressionBurst()
    {
        if (aggressionCooldownTimer > 0f || aggressionBurstCoroutine != null || PlayerTransform == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
        if (distanceToPlayer > aggressionTriggerRange)
            return;

        aggressionCooldownTimer = aggressionCooldown;
        aggressionBurstCoroutine = StartCoroutine(AggressionBurst());
    }

    private IEnumerator AggressionBurst()
    {
        Behavior.Aggression *= aggressionLow;
        float elapsed = 0f;

        if (spriteRenderer != null)
            spriteRenderer.color = ColorUtility.TryParseHtmlString("#FFF99E", out Color color) ? color : Color.yellow;

        while (elapsed < aggressionRampDuration)
        {
            float t = Mathf.Clamp01(elapsed / aggressionRampDuration);

            var low = baseAggression * aggressionLow;
            var high = baseAggression * aggressionHigh;

            Behavior.Aggression = Mathf.Lerp(low, high, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Behavior.Aggression = baseAggression;
        if (spriteRenderer != null && hasOriginalColor)
            spriteRenderer.color = originalColor;
        aggressionBurstCoroutine = null;
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
            () => stateManager.ChangeState(Enemy_PeaceKeeper_State.Idle),
            () => stateManager.ChangeState(Enemy_PeaceKeeper_State.Patrol));
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
            if (!stateManager.IsInState(Enemy_PeaceKeeper_State.Patrol) &&
                !IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_PeaceKeeper_State.Patrol);
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
            }
        );
    }
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_PeaceKeeper_State.Idle);
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeAttacks()
    {
        attackCategories = new List<AttackCategory<Enemy_PeaceKeeper_State>>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_PeaceKeeper_State> { State = Enemy_PeaceKeeper_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }

    #region State Callbacks
    private void OnStateEnter(Enemy_PeaceKeeper_State state)
    {
        switch (state)
        {
            case Enemy_PeaceKeeper_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_PeaceKeeper_State.Knockback),
            () => stateManager.ChangeState(Enemy_PeaceKeeper_State.Idle));
    }

    #region Getters
    public StateManager<Enemy_PeaceKeeper_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;

    #endregion
}