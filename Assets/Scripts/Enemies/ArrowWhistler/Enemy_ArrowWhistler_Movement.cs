using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_ArrowWhistler_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_ArrowWhistler_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float moveAwayDistance = 5f;
    [SerializeField] private float moveAwayDuration = 1f;

    private StateManager<Enemy_ArrowWhistler_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private Enemy_ArrowWhistler_Attack attackComponent;
    private List<AttackCategory> attackCategories;


    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private int facingDirection;
    private EnemyStats stats;
    private bool isRecovering = false;
    private bool canMoveAway = false;   // resets true after each attack
    private bool isMovingAway = false;  // prevents re-triggering mid-move
    private EnemyMoveCooldownTracker<Enemy_ArrowWhistler_State> moveCooldowns = new();

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_ArrowWhistler_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }

    private bool IsMoveOnCooldown(Enemy_ArrowWhistler_State state) => moveCooldowns.IsOnCooldown(state);
    #endregion

    #region Attack Configuration Classes
    private class AttackCategory
    {
        public float Frequency;
        public AttackConfig[] Attacks;
    }

    private class AttackConfig
    {
        public Enemy_ArrowWhistler_State State;
        public float Range;
        public int MoveCountCooldown;
    }
    #endregion

    private bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_ArrowWhistler_State.Attack);
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_ArrowWhistler_Attack>();

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        if (health == null)
            health = GetComponent<Enemy_ArrowWhistler_Health>();

        stats = health.stats;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_ArrowWhistler_State>(animator, Enemy_ArrowWhistler_State.Idle);
        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;

        health.OnEnraged += OnEnraged;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (!stateManager.IsInState(Enemy_ArrowWhistler_State.Knockback) && !isRecovering && !isMovingAway)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_ArrowWhistler_State.Chase) && !isMovingAway)
            Chase();
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

        if (health != null)
        {
            health.OnEnraged -= OnEnraged;
        }
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        behavior.ApplyDifficulty(newModifier);
    }

    public void Chase()
    {
        if (player == null) return;
        Flip();

        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = behavior.Aggression * stats.Speed * direction;
    }

    public void CheckForPlayer()
    {
        if (player != null)
        {
            float distanceToLockedPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToLockedPlayer > behavior.DetectionRange)
                player = null;
        }
        else
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionPoint.position, behavior.DetectionRange, playerLayer);

            if (hitColliders.Length > 0)
            {
                float closestSqrDistance = float.MaxValue;
                Transform closestTransform = null;

                foreach (var collider in hitColliders)
                {
                    float sqrDistance = (collider.transform.position - detectionPoint.position).sqrMagnitude;
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestSqrDistance = sqrDistance;
                        closestTransform = collider.transform;
                    }
                }

                player = closestTransform;
            }
        }

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer <= stats.AttackRange)
            {
                // Move away once per attack cycle if player is too close
                if (distanceToPlayer < moveAwayDistance && canMoveAway && !isMovingAway && !isRecovering && !IsInAnyAttackState())
                {
                    canMoveAway = false;
                    StartCoroutine(MoveAwayCoroutine());
                    return;
                }
                else
                {
                    DecideAttackType();
                }
            }
            else if (!IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_ArrowWhistler_State.Chase);
            }
        }
        else
        {
            if (!stateManager.IsInState(Enemy_ArrowWhistler_State.Idle))
            {
                stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle);
                rb.velocity = Vector2.zero;
            }
        }
    }

    private IEnumerator MoveAwayCoroutine()
    {
        isMovingAway = true;
        float elapsed = 0f;

        stateManager.ChangeState(Enemy_ArrowWhistler_State.Chase);

        while (elapsed < moveAwayDuration)
        {
            if (player == null) break;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist >= moveAwayDistance) break;

            FlipAwayFromPlayer();

            Vector2 direction = (transform.position - player.position).normalized;
            rb.velocity = behavior.Aggression * stats.Speed * direction;

            elapsed += Time.deltaTime;
            yield return null;
        }
        isMovingAway = false;
    }

    private void FlipAwayFromPlayer()
    {
        if (player.position.x > transform.position.x && facingDirection == 1 ||
            player.position.x < transform.position.x && facingDirection == -1)
        {
            facingDirection *= -1;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    private void DecideAttackType()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float random = Random.value;
        float cumulativeProbability = 0f;

        foreach (var category in attackCategories)
        {
            cumulativeProbability += category.Frequency;

            if (random < cumulativeProbability)
            {
                var availableAttacks = category.Attacks
                    .Where(a => distanceToPlayer <= a.Range && !IsMoveOnCooldown(a.State))
                    .ToArray();

                if (availableAttacks.Length > 0)
                {
                    var selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];
                    stateManager.ChangeState(selectedAttack.State);
                    RegisterMoveUsed(selectedAttack.State);
                    return;                                                                                                                                                                      
                }
            }
        }
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        StartCoroutine(AttackRecovery());
    }
    private void OnEnraged()
    {
        behavior.Aggression *= 1.1f;
        stats.Speed *= 1.2f;
        stats.AttackRange *= 1.2f;
        stats.AttackCooldown *= 0.9f;
        stats.Strength *= 1.2f;
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(stats.AttackCooldown);
        isRecovering = false;
        canMoveAway = true; // ← allow move-away until next attack
    }

    public void Flip()
    {
        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            facingDirection *= -1;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }
    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 10f,
            EnrageThreshold = 0.5f,
            Aggression = 1f
        };

        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig { State = Enemy_ArrowWhistler_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));
    }


    #region State Callbacks
    private void OnStateChanged(Enemy_ArrowWhistler_State previousState, Enemy_ArrowWhistler_State newState)
    {
    }

    private void OnStateEnter(Enemy_ArrowWhistler_State state)
    {
        switch (state)
        {
            case Enemy_ArrowWhistler_State.Attack:
                rb.velocity = Vector2.zero;
                Flip();
                break;
            case Enemy_ArrowWhistler_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    private void OnStateExit(Enemy_ArrowWhistler_State state)
    {
    }

    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
    {
        if (!isKnockbackable) return;
        stateManager.ChangeState(Enemy_ArrowWhistler_State.Knockback);
        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));
        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);
        stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_ArrowWhistler_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion
}