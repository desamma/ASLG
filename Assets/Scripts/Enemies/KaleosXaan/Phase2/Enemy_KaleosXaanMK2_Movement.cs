﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kaleos Xaan Phase 2 Movement.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaleosXaanMK2_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaleosXaanMK2_Health health;
    [SerializeField] private bool isKnockbackable = false;
    [SerializeField] private float auraFarmingDuration = 5f;
    [SerializeField] private float sawSpeedDecrease = 0.5f;

    private StateManager<Enemy_KaleosXaanMK2_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    private List<AttackCategory<Enemy_KaleosXaanMK2_State>> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private float pivotDownward = 1.5f;

    private Vector3 AdjustedPosition => transform.position + Vector3.down * pivotDownward;

    private float castRange;
    private float midRange;

    private float attackCooldownTimer = 0f;
    private float buffAbilitySharedCooldown = 0f;
    private bool isAuraFarming = false;
    private bool priorityAttack = true;
    private GameObject activeCompanion = null;

    private EnemyMoveCooldownTracker<Enemy_KaleosXaanMK2_State> moveCooldowns = new();
    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaleosXaanMK2_State usedState)
    {
        var allAttacks = attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => (a.State, a.MoveCountCooldown));
        moveCooldowns.Register(usedState, allAttacks);
    }
    #endregion

    // enemy movement helper
    public Transform PlayerTransform { get; set; }
    public bool IsRecovering { get ; set; }
    public int FacingDirection { get; set; }

    // readonly properties for helper
    public Rigidbody2D Rb => rb;
    public BehaviorProfile Behavior => health.behavior;
    public EnemyStats Stats => health.stats;
    public Transform DetectionPoint => detectionPoint;
    public LayerMask PlayerLayer => playerLayer;
    public Transform SelfTransform => transform;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (health == null)
            health = GetComponent<Enemy_KaleosXaanMK2_Health>();

        attackRecovery = new EnemyAttackRecovery(this, rb);
        knockbackHandler = new KnockbackHandler(this, rb);

        castRange = health.stats.AttackRange * 3.5f;
        midRange = health.stats.AttackRange * 2.5f;

        IsRecovering = false;
        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();

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

        if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_KaleosXaanMK2_State.Chase))
            Chase();
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
        }
    }

    public void Chase()
    {
        if (PlayerTransform == null) return;

        float speedMultiplier = stateManager.IsInState(Enemy_KaleosXaanMK2_State.Saw) ? sawSpeedDecrease : 1f;
        EnemyMovementHelper.Chase(this, speedMultiplier,
            OnEnterAttackRange: () =>
            {
                if (!priorityAttack) return;

                if (!IsInAnyAttackState() && !IsRecovering)
                {
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaanMK2_State.Attack);
                    attackCooldownTimer = Stats.AttackCooldown;
                }
                else if (!IsInAnyAttackState())
                {
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
                }
            },
            positionOverride: () => AdjustedPosition,
            isStopOnAttackRange: priorityAttack);
    }

    public bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_KaleosXaanMK2_State.Attack) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.ArcaneHeart) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.BlinkEnhance) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.Disappear) ||
               stateManager.IsInState(Enemy_KaleosXaanMK2_State.ThreeHitCombo)
               //|| stateManager.IsInState(Enemy_KaleosXaanMK2_State.Saw)
               ;
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
                    stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Attack);
                    RegisterMoveUsed(Enemy_KaleosXaanMK2_State.Attack);
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

                if (state == Enemy_KaleosXaanMK2_State.ArcaneHeart ||
                    state == Enemy_KaleosXaanMK2_State.BlinkEnhance)
                {
                    buffAbilitySharedCooldown = 20f;
                }
            },
            extraFilter: state =>
                !IsBuffAbilityOnSharedCooldown(state) && !IsCompanionActive()
        );
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

        attackRecovery.StartRecovery(Stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
                rb.velocity = Vector2.zero;
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeBehavior()
    {
        attackCategories = new List<AttackCategory<Enemy_KaleosXaanMK2_State>>
         {
             new() {
                 Frequency = Behavior.UltimateAttackFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.ThreeHitCombo, Range = midRange, MoveCountCooldown = 3 }
                 }
             },
             new() {
                 Frequency = Behavior.UltimateAttackFrequency * Behavior.MobilityUsageFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.Disappear, Range = castRange, MoveCountCooldown = 10 },
                 }
             },
             new() {
                 Frequency = Behavior.SpecialAttackFrequency * Behavior.MobilityUsageFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.Saw, Range = midRange, MoveCountCooldown = 3 },

                 }
             },
             new() {
                 Frequency = Behavior.SpecialAttackFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.ArcaneHeart, Range = castRange, MoveCountCooldown = 6 },
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.BlinkEnhance, Range = castRange, MoveCountCooldown = 6 },

                 }
             },
             new() {
                 Frequency = 1f,
                 Attacks = new[]
                 {
                     // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_KaleosXaanMK2_State> { State = Enemy_KaleosXaanMK2_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
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
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                break;
            case Enemy_KaleosXaanMK2_State.ThreeHitCombo:
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
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

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Knockback),
            () => stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Chase))
            stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_KaleosXaanMK2_State.Idle))
            stateManager.ChangeState(Enemy_KaleosXaanMK2_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_KaleosXaanMK2_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}