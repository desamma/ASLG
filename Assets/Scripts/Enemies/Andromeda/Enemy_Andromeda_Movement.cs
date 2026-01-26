using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Andromeda_Movement : MonoBehaviour, IEnemy_Movement
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
    [SerializeField] private Transform player;
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

    private int facingDirection;
    private EnemyStats stats;
    private float castRange;
    private float attackCooldownTimer = 0f;
    private bool isRecovering = false;

    private void Awake()
    {
        originalPosition = transform.position;

        patrolPoints = new Vector2[4];
        patrolPoints[0] = originalPosition + Vector2.up * patrolDistance;
        patrolPoints[1] = originalPosition + Vector2.down * patrolDistance;
        patrolPoints[2] = originalPosition + Vector2.left * patrolDistance;
        patrolPoints[3] = originalPosition + Vector2.right * patrolDistance;
    }

    private void Start()
    {
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

        if (health == null)
            health = GetComponent<Enemy_Andromeda_Health>();

        stats = health.stats;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_Andromeda_State>(animator, Enemy_Andromeda_State.Idle);
        
        castRange = stats.AttackRange * 2.5f;
    
        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_Andromeda_State.Knockback) && !isRecovering)
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
            stateManager.OnStateChanged -= OnStateChanged;
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
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
        if (player == null) return;

        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            Flip();
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * stats.Speed;
    }

    void Patrol()
    {
        if (isWaiting)
        {
            stateManager.ChangeState(Enemy_Andromeda_State.Idle);
            waitTimer += Time.deltaTime;

            if (waitTimer >= idleToPatrolWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;

                int newIndex;
                do
                {
                    newIndex = Random.Range(0, patrolPoints.Length);
                } while (newIndex == currentPatrolIndex);

                currentPatrolIndex = newIndex;
                stateManager.ChangeState(Enemy_Andromeda_State.Patrol);
            }
            return;
        }

        if (unstuckPatrolWaitTimer > 0)
        {
            unstuckPatrolWaitTimer -= Time.deltaTime;

            Vector3 targetPos = patrolPoints[currentPatrolIndex];
            Vector3 direction = (targetPos - transform.position).normalized;
            rb.velocity = direction * stats.Speed;

            if ((targetPos.x > transform.position.x && facingDirection == -1) ||
                (targetPos.x < transform.position.x && facingDirection == 1))
            {
                Flip();
            }

            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                rb.velocity = Vector2.zero;
                isWaiting = true;
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
            unstuckPatrolWaitTimer = unstuckPatrolWaitTime;
            isWaiting = true;
        }
    }

    public void CheckForPlayer()
    {
        // Don't check if currently attacking or recovering
        if (stateManager.IsInState(Enemy_Andromeda_State.Attack) ||
            stateManager.IsInState(Enemy_Andromeda_State.Cast) ||
            isRecovering)
        {
            return;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionPoint.position, behavior.DetectionRange, playerLayer);

        if (hitColliders.Length > 0)
        {
            player = hitColliders[0].transform;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Player within cast range
            if (distanceToPlayer <= castRange)
            {
                rb.velocity = Vector2.zero;

                if (attackCooldownTimer <= 0)
                {
                    DecideAttackType();
                    attackCooldownTimer = stats.AttackCooldown;
                }
            }
            // Player outside cast range ? chase
            else if (distanceToPlayer > castRange &&
                     !(stateManager.IsInState(Enemy_Andromeda_State.Attack) ||
                       stateManager.IsInState(Enemy_Andromeda_State.Cast)))
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Chase);
            }
        }
        else
        {
            if (!stateManager.IsInState(Enemy_Andromeda_State.Patrol) &&
                !stateManager.IsInState(Enemy_Andromeda_State.Attack) &&
                !stateManager.IsInState(Enemy_Andromeda_State.Cast))
            {
                stateManager.ChangeState(Enemy_Andromeda_State.Patrol);
            }
        }
    }

    private void DecideAttackType()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float random = Random.value;

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

        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_Andromeda_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(attackRecoveryDuration);

        isRecovering = false;
    }

    public void Flip()
    {
        facingDirection *= -1;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
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
    private void OnStateChanged(Enemy_Andromeda_State previousState, Enemy_Andromeda_State newState)
    {
    }

    private void OnStateEnter(Enemy_Andromeda_State state)
    {
        switch (state)
        {
            case Enemy_Andromeda_State.Attack:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_Andromeda_State.Cast:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_Andromeda_State.Death:
                rb.velocity = Vector2.zero;
                Destroy(gameObject, 1.3f);
                break;
            case Enemy_Andromeda_State.Patrol:
                unstuckPatrolWaitTimer = unstuckPatrolWaitTime;
                break;
        }
    }

    private void OnStateExit(Enemy_Andromeda_State state)
    {
    }

    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        stateManager.ChangeState(Enemy_Andromeda_State.Knockback);

        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));

        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);

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
