using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_ArgeonHighmayne_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_ArgeonHighmayne_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float attackRecoveryDuration = 2f;
    [SerializeField] private float auraFarmingDuration = 5f;

    private StateManager<Enemy_ArgeonHighmayne_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private Enemy_ArgeonHighmayne_Attack attackComponent;
    private List<AttackCategory> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private int facingDirection;
    private EnemyStats stats;
    private float castRange;
    private float attackCooldownTimer = 0f;
    private bool isRecovering = false;
    private bool isAuraFarming = false;

    #region Attack Configuration Classes
    private class AttackCategory
    {
        public float Frequency;
        public AttackConfig[] Attacks;
    }

    private class AttackConfig
    {
        public Enemy_ArgeonHighmayne_State State;
        public float Range;
    }
    #endregion

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_ArgeonHighmayne_Attack>();

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (playerLayer != LayerMask.GetMask("Player"))
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayne_Health>();

        stats = health.stats;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_ArgeonHighmayne_State>(animator, Enemy_ArgeonHighmayne_State.Idle);

        castRange = stats.AttackRange * 3f;

        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;

        StartCoroutine(AuraFarming());
    }

    private IEnumerator AuraFarming()
    {
        isAuraFarming = true;
        charCollider.enabled = false;
        rb.velocity = Vector2.zero;
        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);

        yield return new WaitForSeconds(auraFarmingDuration);
        isAuraFarming = false;
        charCollider.enabled = true;
    }

    private void Update()
    {
        if (health.isDead || isAuraFarming) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Knockback) && !isRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_ArgeonHighmayne_State.Chase))
        {
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
        // If we have a locked player, check if they're still in detection range
        if (player != null)
        {
            float distanceToLockedPlayer = Vector2.Distance(transform.position, player.position);

            // Release lock if player is out of detection range
            if (distanceToLockedPlayer > behavior.DetectionRange)
            {
                player = null;
            }
        }

        // If no player is locked, try to find one
        else if (player == null)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionPoint.position, behavior.DetectionRange, playerLayer);

            if (hitColliders.Length > 0)
            {
                // Find the closest player
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

                // Lock onto the closest player
                player = closestTransform;
            }
        }

        // If we have a locked player, decide actions
        if (player != null)
        {
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
                     !(stateManager.IsInState(Enemy_ArgeonHighmayne_State.Attack) ||
                       stateManager.IsInState(Enemy_ArgeonHighmayne_State.WarSurge) ||
                       stateManager.IsInState(Enemy_ArgeonHighmayne_State.SunBloom) ||
                       stateManager.IsInState(Enemy_ArgeonHighmayne_State.EntropicDecay) ||
                       stateManager.IsInState(Enemy_ArgeonHighmayne_State.Decimated) ||
                       stateManager.IsInState(Enemy_ArgeonHighmayne_State.AurynNexus)))
            {
                stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Chase);
            }
        }
        else
        {
            // No player locked, return to idle if not already
            if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Idle))
            {
                stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
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

        // Check each category in priority order
        foreach (var category in attackCategories)
        {
            cumulativeProbability += category.Frequency;

            if (random < cumulativeProbability)
            {
                // Filter attacks that are in range
                var availableAttacks = category.Attacks.Where(a => distanceToPlayer <= a.Range).ToArray();

                if (availableAttacks.Length > 0)
                {
                    // Randomly choose from available attacks
                    var selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];
                    stateManager.ChangeState(selectedAttack.State);
                }
                else
                {
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Chase);
                }
                return;
            }
        }
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Attack) &&
            !stateManager.IsInState(Enemy_ArgeonHighmayne_State.WarSurge) &&
            !stateManager.IsInState(Enemy_ArgeonHighmayne_State.SunBloom) &&
            !stateManager.IsInState(Enemy_ArgeonHighmayne_State.EntropicDecay) &&
            !stateManager.IsInState(Enemy_ArgeonHighmayne_State.Decimated) &&
            !stateManager.IsInState(Enemy_ArgeonHighmayne_State.AurynNexus))
            return;

        StartCoroutine(AttackRecovery());
    }
    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
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
            UltimateAttackFrequency = 0.2f,
            Aggression = 1f,
            EnrageThreshold = 0f,
            MobilityUsageFrequency = 0f,
        };

        // Initialize attack categories
        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.Decimated, Range = castRange },
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.AurynNexus, Range = castRange }
                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.WarSurge, Range = stats.AttackRange },
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.SunBloom, Range = castRange },
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.EntropicDecay, Range = stats.AttackRange }
                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.Attack, Range = stats.AttackRange }
                }
            }
        };
    }
    #region State Callbacks
    private void OnStateChanged(Enemy_ArgeonHighmayne_State previousState, Enemy_ArgeonHighmayne_State newState)
    {
    }

    private void OnStateEnter(Enemy_ArgeonHighmayne_State state)
    {
        switch (state)
        {
            case Enemy_ArgeonHighmayne_State.Attack:
                rb.velocity = Vector2.zero;
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                player.position.x < transform.position.x && facingDirection == 1)
                {
                    Flip();
                }
                break;
            case Enemy_ArgeonHighmayne_State.WarSurge:
                rb.velocity = Vector2.zero;
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                player.position.x < transform.position.x && facingDirection == 1)
                {
                    Flip();
                }
                break;
            case Enemy_ArgeonHighmayne_State.SunBloom:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Decimated:
                if (player.position.x > transform.position.x && facingDirection == -1 ||
                 player.position.x < transform.position.x && facingDirection == 1)
                {
                    Flip();
                }
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.AurynNexus:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Death:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(Enemy_ArgeonHighmayne_State state)
    {
    }
    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Knockback);

        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));

        Vector2 knockbackDirection = (transform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;
    }

    IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);

        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
    }
    #region Getters
    public StateManager<Enemy_ArgeonHighmayne_State> GetStateManager()
    {
        return stateManager;
    }
    public BehaviorProfile GetBehavior()
    {
        return behavior;
    }
    #endregion
}