using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Slime_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Slime_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float attackRecoveryDuration = 0.5f;

    private StateManager<Enemy_Slime_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Enemy_Slime_Attack attackComponent;

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

    [Header("Audio")]
    [SerializeField] private AudioClip movementAudioClip;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float minAudioDistance = 1f;
    [SerializeField] private float maxAudioDistance = 10f;
    private AudioSource loopingAudioSource;

    private EnemyStats stats;

    private float attackCooldownTimer = 0f;
    private bool hasWokenUp = false;

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
    private KnockbackHandler knockbackHandler;
    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_Slime_Attack>();

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }
        if (health == null)
            health = GetComponent<Enemy_Slime_Health>();

        stats = health.stats;

        originalPosition = transform.position;

        patrolPoints = new Vector2[4];
        patrolPoints[0] = originalPosition + Vector2.up * patrolDistance;
        patrolPoints[1] = originalPosition + Vector2.down * patrolDistance;
        patrolPoints[2] = originalPosition + Vector2.left * patrolDistance;
        patrolPoints[3] = originalPosition + Vector2.right * patrolDistance;

        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        knockbackHandler = new KnockbackHandler(this, rb);
        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        Enemy_Slime_State initialState = Random.value < 0.5f ? Enemy_Slime_State.Sleep : Enemy_Slime_State.Idle;
        stateManager = new StateManager<Enemy_Slime_State>(animator, initialState);

        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_Slime_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_Slime_State.Chase))
        {
            Chase();
        }
        else if (stateManager.IsInState(Enemy_Slime_State.Patrol))
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
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    private void Patrol()
    {
        EnemyMovementHelper.Patrol(this, patrolPoints, ref currentPatrolIndex, ref isWaiting, ref waitTimer, ref unstuckPatrolWaitTimer,
            idleToPatrolWaitTime, unstuckPatrolWaitTime,
            () => stateManager.ChangeState(Enemy_Slime_State.Idle),
            () => stateManager.ChangeState(Enemy_Slime_State.Patrol));
    }
    public void CheckForPlayer()
    {
        if (IsInAnyAttackState() || IsRecovering)
        {
            return;
        }
        EnemyMovementHelper.CheckForPlayer(this,
            OnPlayerFound: distanceToPlayer =>
            {
                if (!hasWokenUp)
                {
                    hasWokenUp = true;
                    if (stateManager.IsInState(Enemy_Slime_State.Sleep))
                    {
                        stateManager.ChangeState(Enemy_Slime_State.Idle);
                    }
                }
                if (distanceToPlayer <= stats.AttackRange)
                {
                    rb.velocity = Vector2.zero;

                    if (attackCooldownTimer <= 0)
                    {
                        DecideAttackType();
                        attackCooldownTimer = stats.AttackCooldown;
                    }
                }
                else if (distanceToPlayer > stats.AttackRange &&
                         !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Slime_State.Chase);
                }
            }, true,
            OnPatrolInsteadOfIdle: () =>
            {
                if (hasWokenUp &&
                !stateManager.IsInState(Enemy_Slime_State.Patrol) &&
                !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Slime_State.Patrol);
                }
            });
    }

    /// <summary>
    /// Decide between jump attack and spin attack based on behavior profile
    /// </summary>
    private void DecideAttackType()
    {
        if (PlayerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
        float mobilityRandom = Random.value;

        if (mobilityRandom < behavior.MobilityUsageFrequency)
        {
            stateManager.ChangeState(Enemy_Slime_State.Spin);
        }
        else
        {
            if (distanceToPlayer <= stats.AttackRange)
            {
                stateManager.ChangeState(Enemy_Slime_State.Jump);
            }
            else
            {
                stateManager.ChangeState(Enemy_Slime_State.Chase);
            }
        }
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (!stateManager.IsInState(Enemy_Slime_State.Jump) &&
            !stateManager.IsInState(Enemy_Slime_State.Spin))
            return;

        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        IsRecovering = true;
        stateManager.ChangeState(Enemy_Slime_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(attackRecoveryDuration);

        IsRecovering = false;
    }

    public void Flip()
    {
        FacingDirection *= -1;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 10f,
            ChaseRange = 8f,
            SpecialAttackFrequency = 0.3f,
            UltimateAttackFrequency = 0f,
            Aggression = 1.1f,
            EnrageThreshold = 0f,
            MobilityUsageFrequency = 0.5f
        };
    }

    #region State Callbacks
    private void OnStateEnter(Enemy_Slime_State state)
    {
        switch (state)
        {
            case Enemy_Slime_State.Chase:
                if (movementAudioClip != null)
                {
                    loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                        movementAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                    if (loopingAudioSource != null)
                    {
                        loopingAudioSource.transform.SetParent(transform);
                    }
                }
                break;
            case Enemy_Slime_State.Patrol:
                {
                    unstuckPatrolWaitTimer = unstuckPatrolWaitTime;
                    if (movementAudioClip != null)
                    {
                        loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                            movementAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                        if (loopingAudioSource != null)
                        {
                            loopingAudioSource.transform.SetParent(transform);
                        }
                    }
                }
                break;
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(Enemy_Slime_State state)
    {
        switch (state)
        {
            case Enemy_Slime_State.Chase:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
            case Enemy_Slime_State.Patrol:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
            case Enemy_Slime_State.Spin:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_Slime_State.Knockback),
            () => stateManager.ChangeState(Enemy_Slime_State.Idle));
    }

    #region Getters
    public StateManager<Enemy_Slime_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion

    public bool IsInAnyAttackState() =>
        stateManager.IsInState(Enemy_Slime_State.Jump) ||
        stateManager.IsInState(Enemy_Slime_State.Spin);

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_Slime_State.Chase))
            stateManager.ChangeState(Enemy_Slime_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_Slime_State.Idle))
            stateManager.ChangeState(Enemy_Slime_State.Idle);
    }
}
