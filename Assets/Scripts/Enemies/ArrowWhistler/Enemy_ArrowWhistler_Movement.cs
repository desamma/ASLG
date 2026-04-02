using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_ArrowWhistler_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_ArrowWhistler_Health health;
    [SerializeField] private bool isKnockbackable = true;
    [SerializeField] private float moveAwayDistance = 5f;
    [SerializeField] private float moveAwayDuration = 1f;

    private StateManager<Enemy_ArrowWhistler_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Enemy_ArrowWhistler_Attack attackComponent;
    private List<AttackCategory<Enemy_ArrowWhistler_State>> attackCategories;


    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private bool canMoveAway = false;   // resets true after each attack
    private bool isMovingAway = false;  // prevents re-triggering mid-move

    private EnemyMoveCooldownTracker<Enemy_ArrowWhistler_State> moveCooldowns = new();
    private KnockbackHandler knockbackHandler;
    private EnemyAttackRecovery attackRecovery;

    // enemy movement helper
    public Transform PlayerTransform { get; set; }
    public bool IsRecovering { get; set; }
    public int FacingDirection { get; set; }

    // readonly properties for helper
    public Rigidbody2D Rb => rb;
    public BehaviorProfile Behavior => health.behavior;
    public EnemyStats Stats => health.stats;
    public Transform DetectionPoint => detectionPoint;
    public LayerMask PlayerLayer => playerLayer;
    public Transform SelfTransform => transform;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_ArrowWhistler_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    public bool IsInAnyAttackState()
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

        knockbackHandler = new KnockbackHandler(this, rb);
        attackRecovery = new EnemyAttackRecovery(this, rb);

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        if (health == null)
            health = GetComponent<Enemy_ArrowWhistler_Health>();

        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeAttacks();

        stateManager = new StateManager<Enemy_ArrowWhistler_State>(animator, Enemy_ArrowWhistler_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;

        health.OnEnraged += OnEnraged;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (!stateManager.IsInState(Enemy_ArrowWhistler_State.Knockback) && !IsRecovering && !isMovingAway)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_ArrowWhistler_State.Chase) && !isMovingAway)
            Chase();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
        }

        if (health != null)
        {
            health.OnEnraged -= OnEnraged;
        }
    }

    public void Chase()
    {
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this, distanceToPlayer =>
        {
            if (distanceToPlayer <= Stats.AttackRange)
            {
                if (distanceToPlayer < moveAwayDistance && canMoveAway && !isMovingAway && !IsRecovering && !IsInAnyAttackState())
                {
                    canMoveAway = false;
                    StartCoroutine(MoveAwayCoroutine());
                    return;
                }
                DecideAttackType();
            }
            else if (!IsInAnyAttackState())
            {
                ChangeToChaseState();
            }
        });
    }

    private IEnumerator MoveAwayCoroutine()
    {
        isMovingAway = true;
        float elapsed = 0f;

        stateManager.ChangeState(Enemy_ArrowWhistler_State.Chase);

        while (elapsed < moveAwayDuration)
        {
            if (PlayerTransform == null) break;

            float dist = Vector2.Distance(transform.position, PlayerTransform.position);
            if (dist >= moveAwayDistance) break;

            FacingDirection = TransformHelper.FlipAway(transform, PlayerTransform, FacingDirection);

            Vector2 direction = (transform.position - PlayerTransform.position).normalized;
            rb.velocity = Behavior.Aggression * Stats.Speed * direction;

            elapsed += Time.deltaTime;
            yield return null;
        }
        isMovingAway = false;
    }

    private void DecideAttackType()
    {
        EnemyMovementHelper.DecideAttackType(this, attackCategories, moveCooldowns,
            OnAttackSelected: selectedState =>
            {
                stateManager.ChangeState(selectedState);
                RegisterMoveUsed(selectedState);
            });
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;

        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle);
            },
            () =>
            {
                IsRecovering = false;
                canMoveAway = true;
            });
    }

    private void OnEnraged()
    {
        Behavior.Aggression *= 1.1f;
        Stats.Speed *= 1.2f;
        Stats.AttackRange *= 1.2f;
        Stats.AttackCooldown *= 0.9f;
        Stats.Strength *= 1.2f;
    }

    public void InitializeAttacks()
    {
        attackCategories = new List<AttackCategory<Enemy_ArrowWhistler_State>>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_ArrowWhistler_State> { State = Enemy_ArrowWhistler_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));
    }


    #region State Callbacks

    private void OnStateEnter(Enemy_ArrowWhistler_State state)
    {
        switch (state)
        {
            case Enemy_ArrowWhistler_State.Attack:
                rb.velocity = Vector2.zero;
                if (PlayerTransform != null)
                    FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_ArrowWhistler_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_ArrowWhistler_State.Knockback),
            () => stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_ArrowWhistler_State.Chase))
            stateManager.ChangeState(Enemy_ArrowWhistler_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_ArrowWhistler_State.Idle))
            stateManager.ChangeState(Enemy_ArrowWhistler_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_ArrowWhistler_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}