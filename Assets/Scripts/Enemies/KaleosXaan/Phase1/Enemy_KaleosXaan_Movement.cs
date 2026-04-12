using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_KaleosXaan_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaleosXaan_Health health;
    [SerializeField] private bool isKnockbackable = false;
    [SerializeField] private float auraFarmingDuration = 5f;

    private StateManager<Enemy_KaleosXaan_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_KaleosXaan_State>> attackCategories;

    [Header("Effects")]
    [SerializeField] private GameObject groundFire;
    [SerializeField] private GameObject phase2TransitionEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip groundFireSound;
    [SerializeField] private float volume;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    //private
    private float castRange;
    private float attackCooldownTimer = 0f;
    private float buffAbilitySharedCooldown = 0f;
    private bool isAuraFarming = false;
    private GameObject activeCompanion = null;

    private EnemyMoveCooldownTracker<Enemy_KaleosXaan_State> moveCooldowns = new();
    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

    // enemy movement helper
    public Transform PlayerTransform { get; set; }
    public bool IsRecovering { get; set; }
    public int FacingDirection { get; set; }

    //readonly properties for helper
    public Rigidbody2D Rb => rb;
    public BehaviorProfile Behavior => health.behavior;
    public EnemyStats Stats => health.stats;
    public Transform DetectionPoint => detectionPoint;
    public LayerMask PlayerLayer => playerLayer;
    public Transform SelfTransform => transform;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaleosXaan_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_KaleosXaan_State.Chase))
            stateManager.ChangeState(Enemy_KaleosXaan_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_KaleosXaan_State.Idle))
            stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
    }

    public bool IsInAnyAttackState()
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

        castRange = health.stats.AttackRange * 3f;
        IsRecovering = false;
        FacingDirection = 1;

        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        InitializeAttacks();

        stateManager = new StateManager<Enemy_KaleosXaan_State>(animator, Enemy_KaleosXaan_State.Idle);

        stateManager.OnStateEnter += OnStateEnter;
        StartCoroutine(AuraFarming());
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

        if (buffAbilitySharedCooldown > 0)
            buffAbilitySharedCooldown -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_KaleosXaan_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_KaleosXaan_State.Chase))
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
        EnemyMovementHelper.Chase(this,
            OnEnterAttackRange: () =>
            {
                // If close enough and not already attacking, perform immediate attack
                if (!IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_KaleosXaan_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaan_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (!IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
                }
                return;
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
                    stateManager.ChangeState(Enemy_KaleosXaan_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaan_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (attackCooldownTimer <= 0 && !IsInAnyAttackState())
                {
                    DecideAttackType();
                    attackCooldownTimer = Stats.AttackCooldown;
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

                if (state == Enemy_KaleosXaan_State.ArcaneHeart ||
                    state == Enemy_KaleosXaan_State.BlinkEnhance)
                    buffAbilitySharedCooldown = 20f;
            },
            extraFilter: state =>
                IsBuffAbilityOnSharedCooldown(state) || IsCompanionActive(state)
        );
    }

    private bool IsBuffAbilityOnSharedCooldown(Enemy_KaleosXaan_State state)
    {
        if (state == Enemy_KaleosXaan_State.ArcaneHeart || state == Enemy_KaleosXaan_State.BlinkEnhance)
        {
            return buffAbilitySharedCooldown > 0;
        }
        return false;
    }

    private bool IsCompanionActive(Enemy_KaleosXaan_State state)
    {
        if (state == Enemy_KaleosXaan_State.SummonCompanion)
        {
            return activeCompanion != null;
        }
        return false;
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
        if (stateManager == null || !IsInAnyAttackState()) return;
        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_KaleosXaan_State.Idle);
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeAttacks()
    {
        attackCategories = new List<AttackCategory<Enemy_KaleosXaan_State>>
        {
            new() {
                Frequency = Behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig<Enemy_KaleosXaan_State> { State = Enemy_KaleosXaan_State.SummonCompanion, Range = castRange, MoveCountCooldown = 15 }
                }
            },
            new() {
                Frequency = Behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig<Enemy_KaleosXaan_State> { State = Enemy_KaleosXaan_State.ArcaneHeart, Range = castRange, MoveCountCooldown = 5 },
                    new AttackConfig<Enemy_KaleosXaan_State> { State = Enemy_KaleosXaan_State.BlinkEnhance, Range = castRange, MoveCountCooldown = 5 },
                    new AttackConfig<Enemy_KaleosXaan_State> { State = Enemy_KaleosXaan_State.DaemonicLure, Range = castRange, MoveCountCooldown = 2 },
                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_KaleosXaan_State> { State = Enemy_KaleosXaan_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
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

    #region State Callbacks

    private void OnStateEnter(Enemy_KaleosXaan_State state)
    {
        switch (state)
        {
            case Enemy_KaleosXaan_State.Attack:
                rb.velocity = Vector2.zero;
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
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

    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_KaleosXaan_State.Knockback),
            () => stateManager.ChangeState(Enemy_KaleosXaan_State.Idle));
    }

    #region Getters
    public StateManager<Enemy_KaleosXaan_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}