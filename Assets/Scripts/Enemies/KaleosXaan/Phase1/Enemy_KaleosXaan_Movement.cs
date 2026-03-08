using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaleosXaan_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float auraFarmingDuration = 5f;

    private StateManager<Enemy_KaleosXaan_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    private List<AttackCategory> attackCategories;

    [Header("Effects")]
    [SerializeField] private GameObject groundFire;
    [SerializeField] private GameObject phase2TransitionEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip groundFireSound;
    [SerializeField] private float volume;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private int facingDirection;
    private EnemyStats stats;
    private float castRange;
    private float attackCooldownTimer = 0f;
    private float warSurgeWaitTimer = 0f;
    private float buffAbilitySharedCooldown = 0f;
    private bool isRecovering = false;
    private bool isAuraFarming = false;

    private EnemyMoveCooldownTracker<Enemy_KaleosXaan_State> moveCooldowns = new();

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaleosXaan_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }

    private bool IsMoveOnCooldown(Enemy_KaleosXaan_State state) => moveCooldowns.IsOnCooldown(state);
    #endregion

    #region Attack Configuration Classes
    private class AttackCategory
    {
        public float Frequency;
        public AttackConfig[] Attacks;
    }

    private class AttackConfig
    {
        public Enemy_KaleosXaan_State State;
        public float Range;
        public int MoveCountCooldown;
    }
    #endregion

    private bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_KaleosXaan_State.Attack) ||
               stateManager.IsInState(Enemy_KaleosXaan_State.ArcaneHeart) ||
               stateManager.IsInState(Enemy_KaleosXaan_State.DaemonicLure) ||
               stateManager.IsInState(Enemy_KaleosXaan_State.BlinkEnhance) ||
               stateManager.IsInState(Enemy_KaleosXaan_State.SummonCompanion);
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
            health = GetComponent<Enemy_KaleosXaan_Health>();

        stats = health.stats;
        castRange = stats.AttackRange * 3f;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_KaleosXaan_State>(animator, Enemy_KaleosXaan_State.Idle);
        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;

    }

    private IEnumerator AuraFarming()
    {
        isAuraFarming = true;
        charCollider.enabled = false;
        rb.velocity = Vector2.zero;
        stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);

        yield return new WaitForSeconds(auraFarmingDuration);
        isAuraFarming = false;
        charCollider.enabled = true;
    }

    private void Update()
    {
        if (health.isDead || isAuraFarming || IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (warSurgeWaitTimer > 0)
            warSurgeWaitTimer -= Time.deltaTime;

        if (buffAbilitySharedCooldown > 0)
            buffAbilitySharedCooldown -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_KaleosXaan_State.Knockback) && !isRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_KaleosXaan_State.Chase))
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
    }

    public void OnDifficultyChanged(DifficultyModifier newModifier)
    {
        if (newModifier == null) return;
        behavior.ApplyDifficulty(newModifier);
    }

    public void Chase()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= stats.AttackRange)
        {
            rb.velocity = Vector2.zero;

            // If close enough and not already attacking, perform immediate attack
            if (!IsInAnyAttackState() && !isRecovering)
            {
                stateManager.ChangeState(Enemy_KaleosXaan_State.Attack);
                RegisterMoveUsed(Enemy_KaleosXaan_State.Attack);
                attackCooldownTimer = stats.AttackCooldown;
            }
            else if (!IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
            }
            return;
        }

        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            Flip();
        }

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

            if (distanceToPlayer <= castRange)
            {
                if (!stateManager.IsInState(Enemy_KaleosXaan_State.Chase))
                    stateManager.ChangeState(Enemy_KaleosXaan_State.Chase);

                // Prioritize immediate attack when within attack range, ignoring cooldown
                if (distanceToPlayer <= stats.AttackRange && !IsInAnyAttackState() && !isRecovering)
                {
                    stateManager.ChangeState(Enemy_KaleosXaan_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaan_State.Attack);
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
                stateManager.ChangeState(Enemy_KaleosXaan_State.Chase);
            }
        }
        else
        {
            if (!stateManager.IsInState(Enemy_KaleosXaan_State.Idle))
            {
                stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
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
                                !IsBuffAbilityOnSharedCooldown(a.State))
                    .ToArray();

                if (availableAttacks.Length > 0)
                {
                    var selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];

                    stateManager.ChangeState(selectedAttack.State);
                    RegisterMoveUsed(selectedAttack.State);

                    if (selectedAttack.State == Enemy_KaleosXaan_State.ArcaneHeart ||
                        selectedAttack.State == Enemy_KaleosXaan_State.BlinkEnhance)
                    {
                        buffAbilitySharedCooldown = 20f;
                    }

                    return;
                }
            }
        }
    }

    private bool IsBuffAbilityOnSharedCooldown(Enemy_KaleosXaan_State state)
    {
        if (state == Enemy_KaleosXaan_State.ArcaneHeart || state == Enemy_KaleosXaan_State.BlinkEnhance)
        {
            return buffAbilitySharedCooldown > 0;
        }
        return false;
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
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
            UltimateAttackFrequency = 0.3f,
            Aggression = 1f,
            MobilityUsageFrequency = 1f,
        };

        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaan_State.SummonCompanion, Range = castRange, MoveCountCooldown = 15 }
                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_KaleosXaan_State.ArcaneHeart, Range = castRange, MoveCountCooldown = 5 },
                    new AttackConfig { State = Enemy_KaleosXaan_State.BlinkEnhance, Range = castRange, MoveCountCooldown = 5 },
                    new AttackConfig { State = Enemy_KaleosXaan_State.DaemonicLure, Range = castRange, MoveCountCooldown = 2 },
                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig { State = Enemy_KaleosXaan_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }
    public void Phase2Transition()
    {
        var position = transform.position + new Vector3(0f, 1.2f, 0f);
        Instantiate(phase2TransitionEffect, position, Quaternion.identity);
    }
    public void DestroyObject()
    {
        Destroy(gameObject);
    }

    #region State Callbacks
    private void OnStateChanged(Enemy_KaleosXaan_State previousState, Enemy_KaleosXaan_State newState)
    {
    }

    private void OnStateEnter(Enemy_KaleosXaan_State state)
    {
        switch (state)
        {
            case Enemy_KaleosXaan_State.Attack:
                rb.velocity = Vector2.zero;
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                    player.position.x < transform.position.x && facingDirection == 1)
                    Flip();
                break;
            case Enemy_KaleosXaan_State.DaemonicLure:
                rb.velocity = Vector2.zero;
                var blinkPosition = transform.position + new Vector3(0f, -1f, 0f);
                Instantiate(groundFire, blinkPosition, Quaternion.identity);
                SoundFXManager.Instance.PlaySoundFXClip(groundFireSound, transform, volume);
                break;
            case Enemy_KaleosXaan_State.ArcaneHeart:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_KaleosXaan_State.BlinkEnhance:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_KaleosXaan_State.SummonCompanion:
                rb.velocity = Vector2.zero;
                var summonPosition = transform.position + new Vector3(0f, -1f, 0f);
                Instantiate(groundFire, summonPosition, Quaternion.identity);
                SoundFXManager.Instance.PlaySoundFXClip(groundFireSound, transform, volume);
                break;
            case Enemy_KaleosXaan_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    private void OnStateExit(Enemy_KaleosXaan_State state)
    {
    }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        stateManager.ChangeState(Enemy_KaleosXaan_State.Knockback);
        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));
        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);
        stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_KaleosXaan_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion
}