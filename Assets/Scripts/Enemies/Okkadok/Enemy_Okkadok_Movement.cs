using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Okkadok_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Okkadok_Health health;
    [SerializeField] private bool isKnockbackable = true;

    [Header("Be a chicken parameters")]
    [SerializeField] private float retreatSafeDistance = 6f;
    [SerializeField] private float attackAnywayDistance = 4f;
    [SerializeField] private float retreatSpeedNegate = 0.7f;

    private StateManager<Enemy_Okkadok_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private Enemy_Okkadok_Attack attackComponent;
    private List<AttackCategory> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private int facingDirection;
    private bool isRecovering = false;
    private bool runAwayAfterAttack = false;
    private bool isScreamable = true;
    private float screamCooldown = 5f;
    private float screamCooldownTimer = 0;
    private EnemyMoveCooldownTracker<Enemy_Okkadok_State> moveCooldowns = new();
    private KnockbackHandler knockbackHandler;
    private EnemyAttackRecovery attackRecovery;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_Okkadok_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }

    private bool IsMoveOnCooldown(Enemy_Okkadok_State state) => moveCooldowns.IsOnCooldown(state);
    #endregion

    #region Attack Configuration Classes
    private class AttackCategory
    {
        public float Frequency;
        public AttackConfig[] Attacks;
    }

    private class AttackConfig
    {
        public Enemy_Okkadok_State State;
        public float Range;
        public int MoveCountCooldown;
    }
    #endregion

    private bool IsInAnyAttackState() => stateManager.IsInState(Enemy_Okkadok_State.Attack);

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (attackComponent == null) attackComponent = GetComponent<Enemy_Okkadok_Attack>();
        if (charCollider == null) charCollider = GetComponent<Collider2D>();
        if (playerLayer != LayerMask.GetMask("Player")) playerLayer = LayerMask.GetMask("Player");
        if (health == null) health = GetComponent<Enemy_Okkadok_Health>();
        attackRecovery ??= new EnemyAttackRecovery(this, rb);
        knockbackHandler ??= new KnockbackHandler(this, rb);

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        stateManager = new StateManager<Enemy_Okkadok_State>(animator, Enemy_Okkadok_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (screamCooldownTimer > 0)
        {
            screamCooldownTimer -= Time.deltaTime;
        }

        if (!stateManager.IsInState(Enemy_Okkadok_State.Knockback) && !isRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_Okkadok_State.Chase))
            Chase();
        else if (stateManager.IsInState(Enemy_Okkadok_State.Retreat))
            Retreat();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
        }
    }

    public void CheckForPlayer()
    {
        if (player != null)
        {
            if (Vector2.Distance(transform.position, player.position) > health.behavior.DetectionRange)
                player = null;
        }
        else
        {
            var closest = TransformHelper.FindClosestInRange(detectionPoint.position, health.behavior.DetectionRange, playerLayer);
            player = closest;
        }

        if (player == null)
        {
            if (!stateManager.IsInState(Enemy_Okkadok_State.Idle))
            {
                stateManager.ChangeState(Enemy_Okkadok_State.Idle);
                rb.velocity = Vector2.zero;
            }
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        bool playerFacingUs = TransformHelper.IsFacingTarget2D(player, transform);

        if (playerFacingUs)
        {
            // attacking regardless
            if (dist <= attackAnywayDistance && !IsInAnyAttackState() && !runAwayAfterAttack)
            {
                DecideAttackType();
            }
            else if (!IsInAnyAttackState())
            {
                if (dist < retreatSafeDistance)
                {
                    if (!stateManager.IsInState(Enemy_Okkadok_State.Retreat))
                        stateManager.ChangeState(Enemy_Okkadok_State.Retreat);
                    runAwayAfterAttack = false;
                }
                else
                {
                    // Safe distance reached
                    if (!stateManager.IsInState(Enemy_Okkadok_State.Scream) && isScreamable && screamCooldownTimer <= 0)
                    {
                        stateManager.ChangeState(Enemy_Okkadok_State.Scream);
                        isScreamable = false;
                    }
                    else
                    {
                        if (!stateManager.IsInState(Enemy_Okkadok_State.Idle) && !stateManager.IsInState(Enemy_Okkadok_State.Scream))
                            stateManager.ChangeState(Enemy_Okkadok_State.Idle);
                    }
                    rb.velocity = Vector2.zero;
                    Flip();
                }
            }
        }
        else
        {
            // Player is facing away
            if (!IsInAnyAttackState())
            {
                if (dist <= health.stats.AttackRange)
                    DecideAttackType();
                else
                    stateManager.ChangeState(Enemy_Okkadok_State.Chase);
            }
        }
    }

    public void Chase()
    {
        if (player == null) return;
        Flip();
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = health.behavior.Aggression * health.stats.Speed * direction;
    }

    private void Retreat()
    {
        if (player == null) return;
        facingDirection = TransformHelper.FlipAway(transform, player, facingDirection);
        Vector2 direction = (transform.position - player.position).normalized;
        rb.velocity = retreatSpeedNegate * health.behavior.Aggression * health.stats.Speed * direction;
    }

    private void DecideAttackType()
    {
        if (player == null) return;
        runAwayAfterAttack = true;

        float dist = Vector2.Distance(transform.position, player.position);
        float random = Random.value;
        float cumulative = 0f;

        foreach (var category in attackCategories)
        {
            cumulative += category.Frequency;
            if (random < cumulative)
            {
                var available = category.Attacks
                    .Where(a => dist <= a.Range && !IsMoveOnCooldown(a.State))
                    .ToArray();

                if (available.Length > 0)
                {
                    var chosen = available[Random.Range(0, available.Length)];
                    stateManager.ChangeState(chosen.State);
                    RegisterMoveUsed(chosen.State);
                    return;
                }
            }
        }
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(health.stats.AttackCooldown, 
            () =>
            {
                isRecovering = true;
                stateManager.ChangeState(Enemy_Okkadok_State.Idle);
            },
            () =>
            {
                isRecovering = false;
            });
    }

    public void OnScreamAnimationComplete()
    {
        if (stateManager == null || !stateManager.IsInState(Enemy_Okkadok_State.Scream)) return;
        screamCooldownTimer = screamCooldown;
        stateManager.ChangeState(Enemy_Okkadok_State.Idle);
    }

    public void Flip()
    {
        if (player == null) return;
        if ((player.position.x > transform.position.x && facingDirection == -1) ||
            (player.position.x < transform.position.x && facingDirection == 1))
        {
            facingDirection *= -1;
            Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s;
        }
    }

    public void InitializeBehavior()
    {
        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_Okkadok_State.Attack, Range = health.stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        moveCooldowns.Initialize(attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => a.State));
    }

    #region State Manager Callbacks
    private void OnStateEnter(Enemy_Okkadok_State state)
    {
        switch (state)
        {
            case Enemy_Okkadok_State.Attack:
                rb.velocity = Vector2.zero;
                Flip();
                break;
            case Enemy_Okkadok_State.Scream:
                rb.velocity = Vector2.zero;
                Flip();
                break;
            case Enemy_Okkadok_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    private void OnStateExit(Enemy_Okkadok_State state)
    {
        if (state == Enemy_Okkadok_State.Scream)
        {
            isScreamable = true;
        }
    }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable) return;
        stateManager.ChangeState(Enemy_Okkadok_State.Knockback);
        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () =>
            {
                stateManager.ChangeState(Enemy_Okkadok_State.Knockback);
            },
            () =>
            {
                stateManager.ChangeState(Enemy_Okkadok_State.Idle);
            }); ;
    }

    #region Getters
    public StateManager<Enemy_Okkadok_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}