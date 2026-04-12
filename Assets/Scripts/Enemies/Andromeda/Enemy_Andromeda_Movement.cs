using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Andromeda_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Andromeda_Health health;
    [SerializeField] private float attackRecoveryDuration = 1f;
    [SerializeField] private bool isKnockbackable = true;

    private StateManager<Enemy_Andromeda_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    private Vector2 originalPosition;

    [Header("Patrol Settings")]
    [SerializeField] private float idleToPatrolWaitTime = 2f;
    private Vector2[] patrolPoints;
    public float patrolDistance = 3f;
    int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private float stuckCheckTimer = 0f;
    private Vector2 lastCheckedPosition;
    private float waitTimer = 0f;

    private float castRange;
    private float attackCooldownTimer = 0f;

    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

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

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        if (health == null)
            health = GetComponent<Enemy_Andromeda_Health>();

        FacingDirection = 1;

        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        stateManager = new StateManager<Enemy_Andromeda_State>(animator, Enemy_Andromeda_State.Idle);

        castRange = health.stats.AttackRange * 2.5f;

        stateManager.OnStateEnter += OnStateEnter;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_Andromeda_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_Andromeda_State.Chase))
        {
            Chase();
        }
        else if (stateManager.IsInState(Enemy_Andromeda_State.Patrol))
        {
            Patrol();
        }
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
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    private void Patrol()
    {
        EnemyMovementHelper.Patrol(this, patrolPoints, ref currentPatrolIndex, ref isWaiting, ref waitTimer,
            ref stuckCheckTimer, ref lastCheckedPosition,
            idleToPatrolWaitTime,
            unstuckCheckInterval: 1.0f,   // check every 1 second
            stuckThreshold: 0.3f,          // must move at least 0.3 units per check
            () => stateManager.ChangeState(Enemy_Andromeda_State.Idle),
            () => stateManager.ChangeState(Enemy_Andromeda_State.Patrol));
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this,
            OnPlayerFound: distanceToPlayer =>
            {
                if (distanceToPlayer <= castRange)
                {
                    rb.velocity = Vector2.zero;

                    if (attackCooldownTimer <= 0)
                    {
                        DecideAttackType();
                        attackCooldownTimer = health.stats.AttackCooldown;
                    }
                }
                else if (distanceToPlayer > castRange &&
                         !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Andromeda_State.Chase);
                }
            }, true,
            OnPatrolInsteadOfIdle: () =>
            {
                if (!stateManager.IsInState(Enemy_Andromeda_State.Patrol) &&
                    !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Andromeda_State.Patrol);
                }
            });
    }

    private void DecideAttackType()
    {
        if (PlayerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
        float random = UnityEngine.Random.value;

        // Cast is the special attack
        if (random < health.behavior.SpecialAttackFrequency)
        {
            if (distanceToPlayer <= castRange)
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Cast);
            }
            else
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Chase);
            }
        }
        else
        {
            if (distanceToPlayer <= health.stats.AttackRange)
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Attack);
            }
            else
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Chase);
            }
        }
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (!stateManager.IsInState(Enemy_Andromeda_State.Attack) &&
            !stateManager.IsInState(Enemy_Andromeda_State.Cast))
            return;

        attackRecovery.StartRecovery(attackRecoveryDuration,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_Andromeda_State.Idle);
                rb.velocity = Vector2.zero;
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    #region State Callbacks
    private void OnStateEnter(Enemy_Andromeda_State state)
    {
        switch (state)
        {
            case Enemy_Andromeda_State.Death:
                rb.velocity = Vector2.zero;
                Destroy(gameObject, 1.3f);
                break;
            case Enemy_Andromeda_State.Patrol:
                stuckCheckTimer = 0f;
                lastCheckedPosition = transform.position;
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
            () => stateManager.ChangeState(Enemy_Andromeda_State.Knockback),
            () => stateManager.ChangeState(Enemy_Andromeda_State.Idle));
    }

    public bool IsInAnyAttackState() =>
        stateManager.IsInState(Enemy_Andromeda_State.Attack) ||
        stateManager.IsInState(Enemy_Andromeda_State.Cast);

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_Andromeda_State.Chase))
            stateManager.ChangeState(Enemy_Andromeda_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_Andromeda_State.Idle))
            stateManager.ChangeState(Enemy_Andromeda_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_Andromeda_State> GetStateManager()
    {
        return stateManager;
    }
    #endregion
}
