using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Pax_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Pax_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float attackRecoveryDuration = 1f;

    private StateManager<Enemy_Pax_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Enemy_Pax_Attack attackComponent;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    private Vector2 originalPosition;

    [Header("Patrol Settings")]
    [SerializeField] private float idleToPatrolWaitTime = 5f;
    private Vector2[] patrolPoints;
    public float patrolDistance = 3f;
    int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private readonly float unstuckPatrolWaitTime = 1f;
    private float unstuckPatrolWaitTimer;
    private float waitTimer = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip footstepAudioClip;
    [SerializeField] private AudioClip catEngine;
    [SerializeField] private AudioClip catMeow;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float minAudioDistance = 1f;
    [SerializeField] private float maxAudioDistance = 15f;
    private AudioSource loopingAudioSource;

    private EnemyStats stats;
    private float attackCooldownTimer = 0f;
    private float idleTimer = 0f;
    private readonly float idleToLickPawTime = 5f;
    private bool isAttacked = false;

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
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_Pax_Attack>();

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        if (health == null)
            health = GetComponent<Enemy_Pax_Health>();
        stats = health.stats;

        originalPosition = transform.position;

        patrolPoints = new Vector2[4];
        patrolPoints[0] = originalPosition + Vector2.up * patrolDistance;
        patrolPoints[1] = originalPosition + Vector2.down * patrolDistance;
        patrolPoints[2] = originalPosition + Vector2.left * patrolDistance;
        patrolPoints[3] = originalPosition + Vector2.right * patrolDistance;

        IsRecovering = false;
        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_Pax_State>(animator, Enemy_Pax_State.Idle);


        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;

        health.IsAttacked += OnIsAttacked;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!isAttacked)
        {
            // Track idle time for lick paw in both Idle and when waiting during Patrol
            if (stateManager.IsInState(Enemy_Pax_State.Idle))
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleToLickPawTime)
                {
                    stateManager.ChangeState(Enemy_Pax_State.LickPaw);
                    idleTimer = 0f;
                }
            }
            else if (stateManager.IsInState(Enemy_Pax_State.LickPaw))
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleToLickPawTime)
                {
                    // Return to patrol after lick paw if we were patrolling
                    if (isWaiting)
                    {
                        stateManager.ChangeState(Enemy_Pax_State.Patrol);
                    }
                    else
                    {
                        stateManager.ChangeState(Enemy_Pax_State.Idle);
                    }
                    idleTimer = 0f;
                }
            }
        }

        if (!stateManager.IsInState(Enemy_Pax_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_Pax_State.Chase))
        {
            Chase();
        }

        else if (stateManager.IsInState(Enemy_Pax_State.Patrol))
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
        health.IsAttacked -= OnIsAttacked;
        DifficultyManager.Instance.OnDifficultyChanged -= OnDifficultyChanged;
    }

    public void OnIsAttacked()
    {
        if (health.isDead) return;

        isAttacked = true;
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
            () => stateManager.ChangeState(Enemy_Pax_State.Idle),
            () => stateManager.ChangeState(Enemy_Pax_State.Patrol));
    }

    public void CheckForPlayer()
    {
        if (IsInAnyAttackState()) return;

        EnemyMovementHelper.CheckForPlayer(this,
            OnPlayerFound: distanceToPlayer =>
            {
                if (distanceToPlayer <= stats.AttackRange)
                {

                    if (attackCooldownTimer <= 0)
                    {
                        stateManager.ChangeState(Enemy_Pax_State.Attack);
                        attackCooldownTimer = stats.AttackCooldown;
                    }
                }
                else
                {
                    stateManager.ChangeState(Enemy_Pax_State.Chase);
                }
            }, true,
            OnPatrolInsteadOfIdle: () =>
            {
                if (!stateManager.IsInState(Enemy_Pax_State.Patrol) &&
                    !IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Pax_State.Patrol);
                }
            });
    }
    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (!stateManager.IsInState(Enemy_Pax_State.Attack))
            return;

        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        IsRecovering = true;
        stateManager.ChangeState(Enemy_Pax_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(attackRecoveryDuration);

        IsRecovering = false;
    }

    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 8f,
            ChaseRange = 5f,
            SpecialAttackFrequency = 0f,
            UltimateAttackFrequency = 0f,
            Aggression = 1.5f,
            EnrageThreshold = 0f,
            MobilityUsageFrequency = 0f,
        };
    }

    #region State Callbacks
    private void OnStateEnter(Enemy_Pax_State state)
    {
        switch (state)
        {
            case Enemy_Pax_State.Attack:
                rb.velocity = Vector2.zero;
                if (PlayerTransform != null)
                    FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_Pax_State.Death:
                rb.velocity = Vector2.zero;
                Destroy(gameObject, 1.3f);
                break;
            case Enemy_Pax_State.LickPaw:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_Pax_State.Chase:
                {
                    if (footstepAudioClip != null)
                    {
                        loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                            footstepAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                        if (loopingAudioSource != null)
                        {
                            loopingAudioSource.transform.SetParent(transform);
                        }
                    }
                }
                break;
            case Enemy_Pax_State.Patrol:
                {
                    unstuckPatrolWaitTimer = unstuckPatrolWaitTime;
                    if (footstepAudioClip != null)
                    {
                        loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                            footstepAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                        if (loopingAudioSource != null)
                        {
                            loopingAudioSource.transform.SetParent(transform);
                        }
                    }
                    break;
                }
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(Enemy_Pax_State state)
    {
        switch (state)
        {
            case Enemy_Pax_State.Chase:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
            case Enemy_Pax_State.Patrol:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
            default: 
                rb .velocity = Vector2.zero;
                break;
        }
    }

    #endregion
    public void PlayCatEngine()
    {
        float rand = Random.Range(0f, 1f);
        if (rand > 0.5f)
            SoundFXManager.Instance.PlaySoundFXClip(catEngine, transform, volume);
    }

    public void PlayCatMeow()
    {
        float rand = Random.Range(0f, 1f);
        if (rand > 0.5f)
            SoundFXManager.Instance.PlaySoundFXClip(catMeow, transform, volume);
    }
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
    {
        if (!isKnockbackable) return;
        stateManager.ChangeState(Enemy_Pax_State.Knockback);

        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));

        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);

        stateManager.ChangeState(Enemy_Pax_State.Idle);
    }
    #region Getters
    public StateManager<Enemy_Pax_State> GetStateManager()
    {
        return stateManager;
    }
    public BehaviorProfile GetBehavior()
    {
        return behavior;
    }

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_Pax_State.Attack);
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_Pax_State.Chase))
            stateManager.ChangeState(Enemy_Pax_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_Pax_State.Idle))
            stateManager.ChangeState(Enemy_Pax_State.Idle);
    }
    #endregion
}
