using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Andromeda_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Andromeda_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float attackRecoveryDuration = 1f;

    private StateManager<Enemy_Andromeda_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Enemy_Andromeda_Attack attackComponent;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    private Vector2 originalPosition;

    [Header("Patrol Settings")]
    [SerializeField] private float idleToPatrolWaitTime = 2f;
    private Vector2[] patrolPoints;
    public float patrolDistance = 3f;
    int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private readonly float unstuckPatrolWaitTime = 1f;
    private float unstuckPatrolWaitTimer;
    private float waitTimer = 0f;

    private EnemyStats stats;
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
    public BehaviorProfile Behavior => behavior;
    public EnemyStats Stats => stats;
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

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_Andromeda_Attack>();

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        if (health == null)
            health = GetComponent<Enemy_Andromeda_Health>();

        stats = health.stats;

        FacingDirection = 1;

        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_Andromeda_State>(animator, Enemy_Andromeda_State.Idle);

        castRange = stats.AttackRange * 2.5f;

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

    private void OnEnable()
    {
        DifficultyManager.Instance.OnDifficultyChanged += OnDifficultyChanged;
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
        }
        DifficultyManager.Instance.OnDifficultyChanged -= OnDifficultyChanged;
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        behavior.ApplyDifficulty(newModifier);
    }

    public void Chase()
    {
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    private void Patrol()
    {
        EnemyMovementHelper.Patrol(this, patrolPoints, ref currentPatrolIndex, ref isWaiting, ref waitTimer, ref unstuckPatrolWaitTimer,
            idleToPatrolWaitTime, unstuckPatrolWaitTime,
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
                        attackCooldownTimer = stats.AttackCooldown;
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
        if (random < behavior.SpecialAttackFrequency)
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
            if (distanceToPlayer <= stats.AttackRange)
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

    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 15f,
            ChaseRange = 8f,
            SpecialAttackFrequency = 0.3f,
            UltimateAttackFrequency = 0f,
            Aggression = 1f,
            EnrageThreshold = 0f,
            MobilityUsageFrequency = 0f,
        };
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
                unstuckPatrolWaitTimer = unstuckPatrolWaitTime;
                break;
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
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
    public BehaviorProfile GetBehavior()
    {
        return behavior;
    }
    #endregion
}
