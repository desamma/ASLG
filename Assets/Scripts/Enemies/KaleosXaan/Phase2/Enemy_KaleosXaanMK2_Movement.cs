using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kaleos Xaan Phase 2 Movement.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaanMK2_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaleosXaanMK2_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float auraFarmingDuration = 5f;
    [SerializeField] private float sawSpeedDecrease = 0.5f;

    private StateManager<Enemy_KaleosXaanMK2_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    private List<AttackCategory> attackCategories;

    [Header("Effects")]

    [Header("Audio")]
    [SerializeField] private float volume = 1f;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private float pivotDownward = 1.5f;

    private Vector3 AdjustedPosition => transform.position + Vector3.down * pivotDownward;

    public int facingDirection;
    private EnemyStats stats;
    private float castRange;
    private float midRange;

    private float attackCooldownTimer = 0f;
    private float buffAbilitySharedCooldown = 0f;
    private bool isRecovering = false;
    private bool isAuraFarming = false;
    private bool priorityAttack = true;
    private GameObject activeCompanion = null;

    private EnemyMoveCooldownTracker<Enemy_KaleosXaanMK2_State> moveCooldowns = new();

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaleosXaanMK2_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }

    private bool IsMoveOnCooldown(Enemy_KaleosXaanMK2_State state) => moveCooldowns.IsOnCooldown(state);
    #endregion                                                      b

    #region Attack Configuration Classes
    private class AttackCategory
    {
        public float Frequency;
        public AttackConfig[] Attacks;
    }

    private class AttackConfig
    {
        public Enemy_KaleosXaanMK2_State State;
        public float Range;
        public int MoveCountCooldown;
    }
    #endregion

    private bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_KaleosXaanMK2_State.Attack) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.ArcaneHeart) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.BlinkEnhance) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.Disappear) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.ThreeHitCombo)
               //|| stateManager.IsInState(Enemy_KaleosXaanMK2_State.Saw)
               ;
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
            health = GetComponent<Enemy_KaleosXaanMK2_Health>();

        stats = health.stats;

        castRange = stats.AttackRange * 3.5f;
        midRange = stats.AttackRange * 2.5f;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_KaleosXaanMK2_State>(animator, Enemy_KaleosXaanMK2_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
        StartCoroutine(AuraFarming());
    }

    private IEnumerator AuraFarming()
    {
        isAuraFarming = true;
        charCollider.enabled = false;
        rb.velocity = Vector2.zero;
        stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);

        yield return new WaitForSeconds(auraFarmingDuration);
        isAuraFarming = false;
        charCollider.enabled = true;
    }

    private void Update()
    {
        if (health.isDead || isAuraFarming) return;

        if (stateManager.GetCurrentState() == Enemy_KaleosXaanMK2_State.Saw)
        {
            priorityAttack = false;
            if (attackCooldownTimer > 0) attackCooldownTimer -= Time.deltaTime;
            Chase();
            return;
        }

        if (IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (buffAbilitySharedCooldown > 0) 
            buffAbilitySharedCooldown -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Knockback) && !isRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_KaleosXaanMK2_State.Chase))
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

        float distanceToPlayer = Vector2.Distance(AdjustedPosition, player.position);

        if (priorityAttack)
        {
            if (distanceToPlayer <= stats.AttackRange)
            {
                rb.velocity = Vector2.zero;

                // If close enough and not already attacking, perform immediate attack
                if (!IsInAnyAttackState() && !isRecovering)
                {
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaanMK2_State.Attack);
                    attackCooldownTimer = stats.AttackCooldown;
                }
                else if (!IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
                }
                return;
            }
        }

        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            Flip();
        }
        
        Vector2 direction = (player.position - AdjustedPosition).normalized;
        if (stateManager.IsInState(Enemy_KaleosXaanMK2_State.Saw))
        {
            rb.velocity = behavior.Aggression * stats.Speed * sawSpeedDecrease * direction;
        }
        else
        {
            rb.velocity = behavior.Aggression * stats.Speed * direction;
        }
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

            if (distanceToPlayer <= castRange)
            {
                if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Chase))
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Chase);

                // Prioritize immediate attack when within attack range, ignoring cooldown
                if (distanceToPlayer <= stats.AttackRange && !IsInAnyAttackState() && !isRecovering)
                {
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaanMK2_State.Attack);
                    attackCooldownTimer = stats.AttackCooldown;
                }
                else if (attackCooldownTimer <= 0 && !IsInAnyAttackState())
                {
                    DecideAttackType();
                    attackCooldownTimer = stats.AttackCooldown;
                }
            }
            else if (!IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Chase);
            }
        }
        else
        {
            if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Idle))
            {
                stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
                rb.velocity = Vector2.zero;
            }
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
                    .Where(a => distanceToPlayer <= a.Range &&
                                !IsMoveOnCooldown(a.State) &&
                                !IsBuffAbilityOnSharedCooldown(a.State) &&
                                !IsCompanionActive()
                                )
                    .ToArray();

                if (availableAttacks.Length > 0)
                {
                    var selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];

                    stateManager.ChangeState(selectedAttack.State);
                    RegisterMoveUsed(selectedAttack.State);

                    if (selectedAttack.State == Enemy_KaleosXaanMK2_State.ArcaneHeart ||
                        selectedAttack.State == Enemy_KaleosXaanMK2_State.BlinkEnhance)
                    {
                        buffAbilitySharedCooldown = 20f;
                    }

                    return;
                }
            }
        }
    }

    private bool IsBuffAbilityOnSharedCooldown(Enemy_KaleosXaanMK2_State state)
    {
        if (state == Enemy_KaleosXaanMK2_State.ArcaneHeart || state == Enemy_KaleosXaanMK2_State.BlinkEnhance)
        {
            return buffAbilitySharedCooldown > 0;
        }
        return false;
    }

    public bool IsCompanionActive()
    {
        return activeCompanion != null;
    }

    public void RegisterCompanion(GameObject companion)
    {
        activeCompanion = companion;
    }

    public void OnCompanionDestroyed()
    {
        activeCompanion = null;
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        bool isSaw = stateManager.IsInState(Enemy_KaleosXaanMK2_State.Saw);
        if (!IsInAnyAttackState() && !isSaw) return;

        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(stats.AttackCooldown);
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
            SpecialAttackFrequency = 0.4f,
            UltimateAttackFrequency = 0.2f,
            Aggression = 1f,
            MobilityUsageFrequency = 1f,
        };

        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.ThreeHitCombo, Range = midRange, MoveCountCooldown = 3 }
                }
            },
            new() {
                Frequency = behavior.UltimateAttackFrequency * behavior.MobilityUsageFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.Disappear, Range = castRange, MoveCountCooldown = 10 },
                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency * behavior.MobilityUsageFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.Saw, Range = midRange, MoveCountCooldown = 3 },

                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.ArcaneHeart, Range = castRange, MoveCountCooldown = 6 },
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.BlinkEnhance, Range = castRange, MoveCountCooldown = 6 },

                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig { State = Enemy_KaleosXaanMK2_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }
    #region State Callbacks
    private void OnStateEnter(Enemy_KaleosXaanMK2_State state)
    {
        switch (state)
        {
            case Enemy_KaleosXaanMK2_State.Attack:
                rb.velocity = Vector2.zero;
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                    player.position.x < transform.position.x && facingDirection == 1)
                    Flip();
                break;
            case Enemy_KaleosXaanMK2_State.ThreeHitCombo:
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                player.position.x < transform.position.x && facingDirection == 1)
                    Flip();
                rb.velocity = Vector2.zero;
                break;
            case Enemy_KaleosXaanMK2_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
            default:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(Enemy_KaleosXaanMK2_State state)
    {
        switch (state)
        {
            case Enemy_KaleosXaanMK2_State.Saw:
                priorityAttack = true;
                rb.velocity = Vector2.zero;
                break;
        }
    }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime, bool isKnockbackable)
    {
        if (!isKnockbackable) return;
        stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Knockback);
        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));
        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);
        stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_KaleosXaanMK2_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion
}