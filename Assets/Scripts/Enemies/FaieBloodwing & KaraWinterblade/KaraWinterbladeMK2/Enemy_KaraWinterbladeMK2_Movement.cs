using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaraWinterbladeMK2_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaraWinterbladeMK2_Health health;
    [SerializeField] private bool isKnockbackable = true;

    private StateManager<Enemy_KaraWinterbladeMK2_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_KaraWinterbladeMK2_State>> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip cryoCharge;
    [SerializeField] private float volume = 1f;

    private float castRange;
    private float attackCooldownTimer = 0f;
    private EnemyMoveCooldownTracker<Enemy_KaraWinterbladeMK2_State> moveCooldowns = new();
    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaraWinterbladeMK2_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

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

    private Transform faieBloodwingTransform;
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
            health = GetComponent<Enemy_KaraWinterbladeMK2_Health>();

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        castRange = health.stats.AttackRange * 3.5f;

        IsRecovering = false;
        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

        stateManager = new StateManager<Enemy_KaraWinterbladeMK2_State>(animator, Enemy_KaraWinterbladeMK2_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (faieBloodwingTransform == null)
        {
            var faie = FindObjectOfType<Enemy_FaieBloodwing_Movement>();
            if (faie != null)
                faieBloodwingTransform = faie.transform;
        }

        if (stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Chase))
            Chase();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
        }
    }

    public void Chase()
    {
        if (PlayerTransform == null) return;

        EnemyMovementHelper.Chase(this);
    }

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Attack) ||
               stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Cast);
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this, distanceToPlayer =>
        {
            if (distanceToPlayer <= castRange)
            {
                ChangeToChaseState();
                if (attackCooldownTimer <= 0 && !IsInAnyAttackState())
                {
                    DecideAttackType();
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (attackCooldownTimer > 0 && distanceToPlayer <= Stats.AttackRange)
                {
                    stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Attack);
                    RegisterMoveUsed(Enemy_KaraWinterbladeMK2_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else
                {
                    ChangeToChaseState();
                }
            }
            else if (!IsInAnyAttackState())
            {
                ChangeToChaseState();
            }
        });
    }

    private void DecideAttackType()
    {
        EnemyMovementHelper.DecideAttackType(this, attackCategories, moveCooldowns,
            OnAttackSelected: state =>
            {
                stateManager.ChangeState(state);
                RegisterMoveUsed(state);
            });
    }

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Idle);
                rb.velocity = Vector2.zero;
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeBehavior()
    {
        attackCategories = new List<AttackCategory<Enemy_KaraWinterbladeMK2_State>>
         {
             new() {
                 Frequency = Behavior.SpecialAttackFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaraWinterbladeMK2_State> { State = Enemy_KaraWinterbladeMK2_State.Cast, Range = castRange, MoveCountCooldown = 5 },
                 }
             },
             new() {
                 Frequency = 1f,
                 Attacks = new[]
                 {
                     // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_KaraWinterbladeMK2_State> { State = Enemy_KaraWinterbladeMK2_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                 }
             }
         };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }
    #region State Callbacks
    private void OnStateEnter(Enemy_KaraWinterbladeMK2_State state)
    {
        switch (state)
        {
            case Enemy_KaraWinterbladeMK2_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
            case Enemy_KaraWinterbladeMK2_State.Cast:
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                rb.velocity = Vector2.zero;
                SoundFXManager.Instance.PlaySoundFXClip(cryoCharge, transform, volume);
                break;
            default:
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                rb.velocity = Vector2.zero;
                break;
        }
    }

    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Knockback),
            () => stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Chase))
            stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_KaraWinterbladeMK2_State.Idle))
            stateManager.ChangeState(Enemy_KaraWinterbladeMK2_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_KaraWinterbladeMK2_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}