using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Dogehai_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Dogehai_Health health;
    [SerializeField] private bool isKnockbackable = true;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float auraFarmingDuration = 2f;

    private StateManager<Enemy_Dogehai_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_Dogehai_State>> attackCategories;

    [Header("Dig Settings")]
    [SerializeField] private float digChance = 0.5f;
    [SerializeField] private float digDuration = 2f;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip digAudio;
    [SerializeField] private AudioClip barkAudio;
    [SerializeField] private AudioClip cryAudio;
    [SerializeField] private float volume = 0.8f;

    private EnemyStats stats;
    private float castRange;
    private float attackCooldownTimer = 0f;
    private float warSurgeWaitTimer = 0f;
    private bool isAuraFarming = false;

    private EnemyMoveCooldownTracker<Enemy_Dogehai_State> moveCooldowns = new();
    private KnockbackHandler knockbackHandler;
    private EnemyAttackRecovery attackRecovery;

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

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_Dogehai_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_Dogehai_State.Attack);
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
            health = GetComponent<Enemy_Dogehai_Health>();

        stats = health.stats;
        castRange = stats.AttackRange * 3f;

        IsRecovering = false;
        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        knockbackHandler = new KnockbackHandler(this, rb);
        attackRecovery = new EnemyAttackRecovery(this, rb);

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_Dogehai_State>(animator, Enemy_Dogehai_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;
        StartCoroutine(AuraFarming());
    }
    private IEnumerator AuraFarming()
    {
        isAuraFarming = true;
        charCollider.enabled = false;
        rb.velocity = Vector2.zero;
        stateManager.ChangeState(Enemy_Dogehai_State.Idle);
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

        if (!stateManager.IsInState(Enemy_Dogehai_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_Dogehai_State.Chase))
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
        EnemyMovementHelper.Chase(this,
            OnEnterAttackRange: () =>
            {
                if (!IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_Dogehai_State.Attack);
                    RegisterMoveUsed(Enemy_Dogehai_State.Attack);
                    attackCooldownTimer = stats.AttackCooldown;
                }
                else if (!IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_Dogehai_State.Idle);
                }
            });
    }

    public void CheckForPlayer()
    {
        EnemyMovementHelper.CheckForPlayer(this, distanceToPlayer =>
        {
            if (distanceToPlayer <= castRange)
            {
                ChangeToChaseState();

                if (distanceToPlayer <= Stats.AttackRange && !IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_Dogehai_State.Attack);
                    RegisterMoveUsed(Enemy_Dogehai_State.Attack);
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
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(stats.AttackCooldown,
             () =>
             {
                 IsRecovering = true;
                 stateManager.ChangeState(Enemy_Dogehai_State.Idle);
             },
             () =>
             {
                 StartCoroutine(DigCoroutine());
             });
    }

    private IEnumerator DigCoroutine()
    {
        // Random chance to dig after attack recovery
        if (Random.value < digChance)
        {
            stateManager.ChangeState(Enemy_Dogehai_State.Dig);
            yield return new WaitForSeconds(digDuration);
        }

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
            Aggression = 1f,
            MobilityUsageFrequency = 0f,
        };

        attackCategories = new List<AttackCategory<Enemy_Dogehai_State>>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_Dogehai_State> { State = Enemy_Dogehai_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }

    #region State Callbacks
    private void OnStateEnter(Enemy_Dogehai_State state)
    {
        switch (state)
        {
            case Enemy_Dogehai_State.Idle:
                rb.velocity = Vector2.zero;
                break;

            case Enemy_Dogehai_State.Dig:
                rb.velocity = Vector2.zero;
                SoundFXManager.Instance.PlaySoundFXClip(digAudio, transform, volume);
                break;
            case Enemy_Dogehai_State.Attack:
                rb.velocity = Vector2.zero;
                if (PlayerTransform != null)
                    FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                SoundFXManager.Instance.PlaySoundFXClip(barkAudio, transform, volume);
                break;
            case Enemy_Dogehai_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                SoundFXManager.Instance.PlaySoundFXClip(cryAudio, transform, volume);
                break;
        }
    }

    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_Dogehai_State.Knockback),
            () => stateManager.ChangeState(Enemy_Dogehai_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_Dogehai_State.Chase))
            stateManager.ChangeState(Enemy_Dogehai_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_Dogehai_State.Idle))
            stateManager.ChangeState(Enemy_Dogehai_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_Dogehai_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion
}