using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_AlterRexx_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_AlterRexx_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float attackRecoveryDuration = 1f;

    private StateManager<Enemy_AlterRexx_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private Enemy_AlterRexx_Attack attackComponent;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip movementAudioClip;
    [SerializeField] private float volume = 0.2f;
    [SerializeField] private float minAudioDistance = 5f;
    [SerializeField] private float maxAudioDistance = 50f;
    private AudioSource loopingAudioSource;

    private int facingDirection;
    private EnemyStats stats;
    private float attackCooldownTimer = 0f;
    private bool isRecovering = false;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_AlterRexx_Attack>();

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        if (health == null)
            health = GetComponent<Enemy_AlterRexx_Health>();

        stats = health.stats;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_AlterRexx_State>(animator, Enemy_AlterRexx_State.Idle);

        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_AlterRexx_State.Knockback) && !isRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_AlterRexx_State.Chase))
        {
            if (player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);
                if (distanceToPlayer > behavior.ChaseRange)
                {
                    rb.velocity = Vector2.zero;
                    stateManager.ChangeState(Enemy_AlterRexx_State.Idle);
                    player = null;
                    return;
                }
            }
            Chase();
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

    public void CheckForPlayer()
    {
        // Don't check if currently attacking or recovering
        if (stateManager.IsInState(Enemy_AlterRexx_State.Attack) ||
            isRecovering)
        {
            return;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionPoint.position, behavior.DetectionRange, playerLayer);

        if (hitColliders.Length > 0)
        {
            player = hitColliders[0].transform;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Player within attack range
            if (distanceToPlayer <= stats.AttackRange)
            {
                rb.velocity = Vector2.zero;

                if (attackCooldownTimer <= 0)
                {
                    stateManager.ChangeState(Enemy_AlterRexx_State.Attack);
                    attackCooldownTimer = stats.AttackCooldown;
                }
            }
            // Player outside attack range but within chase range - chase
            else if (distanceToPlayer > stats.AttackRange &&
                     distanceToPlayer <= behavior.ChaseRange &&
                     !stateManager.IsInState(Enemy_AlterRexx_State.Attack))
            {
                stateManager.ChangeState(Enemy_AlterRexx_State.Chase);
            }
        }
        else
        {
            // Player not detected - return to idle if currently chasing
            if (stateManager.IsInState(Enemy_AlterRexx_State.Chase))
            {
                rb.velocity = Vector2.zero;
                stateManager.ChangeState(Enemy_AlterRexx_State.Idle);
                player = null;
            }
        }
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (!stateManager.IsInState(Enemy_AlterRexx_State.Attack))
            return;

        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_AlterRexx_State.Idle);
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
    private void OnStateChanged(Enemy_AlterRexx_State previousState, Enemy_AlterRexx_State newState)
    {
    }

    private void OnStateEnter(Enemy_AlterRexx_State state)
    {
        switch (state)
        {
            case Enemy_AlterRexx_State.Attack:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_AlterRexx_State.Death:
                rb.velocity = Vector2.zero;
                Destroy(gameObject, 1.3f);
                break;
            case Enemy_AlterRexx_State.Chase:
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
        }
    }

    private void OnStateExit(Enemy_AlterRexx_State state)
    {
        switch (state)
        {
            case Enemy_AlterRexx_State.Chase:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
        }
    }

    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
    {
        if (!isKnockbackable) return;
        stateManager.ChangeState(Enemy_AlterRexx_State.Knockback);

        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));

        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);

        stateManager.ChangeState(Enemy_AlterRexx_State.Idle);
    }
    #region Getters
    public StateManager<Enemy_AlterRexx_State> GetStateManager()
    {
        return stateManager;
    }
    public BehaviorProfile GetBehavior()
    {
        return behavior;
    }
    #endregion
}
