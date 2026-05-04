using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kaleos Xaan Phase 2 Movement.
/// </summary>
[DisallowMultipleComponent]
public class Enemy_KaraWinterblade_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_KaraWinterblade_Health health;
    [SerializeField] private bool isKnockbackable = true;

    private StateManager<Enemy_KaraWinterblade_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject deathEffect;
    private List<AttackCategory<Enemy_KaraWinterblade_State>> attackCategories;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private float castRange;

    private float attackCooldownTimer = 0f;

    private EnemyMoveCooldownTracker<Enemy_KaraWinterblade_State> moveCooldowns = new();
    private EnemyAttackRecovery attackRecovery;
    private KnockbackHandler knockbackHandler;

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_KaraWinterblade_State usedState)
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

        if (health == null)
            health = GetComponent<Enemy_KaraWinterblade_Health>();

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

        stateManager = new StateManager<Enemy_KaraWinterblade_State>(animator, Enemy_KaraWinterblade_State.Idle);
        stateManager.OnStateEnter += OnStateEnter;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_KaraWinterblade_State.Knockback) && !IsRecovering)
            CheckForPlayer();

        if (faieBloodwingTransform == null)
        {
            var faie = FindObjectOfType<Enemy_FaieBloodwing_Movement>();
            if (faie != null)
                faieBloodwingTransform = faie.transform;
        }

        if (stateManager.IsInState(Enemy_KaraWinterblade_State.Chase))
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
        return stateManager.IsInState(Enemy_KaraWinterblade_State.Attack) ||
               stateManager.IsInState(Enemy_KaraWinterblade_State.Cast) ||
               stateManager.IsInState(Enemy_KaraWinterblade_State.ThreeHitAttack);
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
                    stateManager.ChangeState(Enemy_KaraWinterblade_State.Attack);
                    RegisterMoveUsed(Enemy_KaraWinterblade_State.Attack);
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
                stateManager.ChangeState(Enemy_KaraWinterblade_State.Idle);
                rb.velocity = Vector2.zero;
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    public void InitializeBehavior()
    {
        attackCategories = new List<AttackCategory<Enemy_KaraWinterblade_State>>
         {
             new() {
                 Frequency = Behavior.UltimateAttackFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaraWinterblade_State> { State = Enemy_KaraWinterblade_State.ThreeHitAttack, Range = Stats.AttackRange, MoveCountCooldown = 1 }
                 }
             },
             new() {
                 Frequency = Behavior.SpecialAttackFrequency,
                 Attacks = new[]
                 {
                    new AttackConfig<Enemy_KaraWinterblade_State> { State = Enemy_KaraWinterblade_State.Cast, Range = castRange, MoveCountCooldown = 5 },
                 }
             },
             new() {
                 Frequency = 1f,
                 Attacks = new[]
                 {
                     // Basic Attack: no count cooldown
                    new AttackConfig<Enemy_KaraWinterblade_State> { State = Enemy_KaraWinterblade_State.Attack, Range = Stats.AttackRange, MoveCountCooldown = 0 }
                 }
             }
         };

        // Pre-populate counters so all attacks are available at the start
        moveCooldowns.Initialize(attackCategories
        .SelectMany(c => c.Attacks)
        .Select(a => a.State));

    }
    #region State Callbacks
    private void OnStateEnter(Enemy_KaraWinterblade_State state)
    {
        switch (state)
        {
            case Enemy_KaraWinterblade_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
            default:
                FacingDirection = TransformHelper.FlipTowards(transform, PlayerTransform, FacingDirection);
                rb.velocity = Vector2.zero;
                break;
        }
    }

    #endregion

    public IEnumerator DeathMovingCoroutine()
    {
        if (faieBloodwingTransform == null)
        {
            var faie = GameObject.FindWithTag("FaieBloodwing");
            if (faie != null)
                faieBloodwingTransform = faie.transform;
        }

        ChangeToIdleState();
        yield return new WaitForSeconds(2f);

        bool reachedTarget = false;

        while (!reachedTarget && faieBloodwingTransform != null)
        {

            float distanceToTarget = Vector2.Distance(transform.position, faieBloodwingTransform.position);
            if (distanceToTarget <= Stats.AttackRange)
            {
                ChangeToIdleState();
                reachedTarget = true;
                rb.velocity = Vector2.zero;
            }
            else
            {
                ChangeToChaseState();
                EnemyMovementHelper.Chase(this, speedMultiplier: Stats.Speed * 0.1f, destinationOverride: faieBloodwingTransform);
            }

            yield return null;
        }
        yield return new WaitForSeconds(2f);
        FacingDirection = TransformHelper.FlipTowards(transform, faieBloodwingTransform, FacingDirection);

        Vector3 midpoint = (transform.position + faieBloodwingTransform.position) / 2f;
        Instantiate(deathEffect, midpoint, Quaternion.identity);

        SpriteRenderer karaSprite = GetComponent<SpriteRenderer>();
        SpriteRenderer faieSprite = null;
        if (faieBloodwingTransform != null)
            faieSprite = faieBloodwingTransform.GetComponent<SpriteRenderer>();

        if (karaSprite != null || faieSprite != null)
        {
            float fadeDuration = 1.5f;
            float elapsedTime = 0f;
            Color karaStartColor = karaSprite != null ? karaSprite.color : Color.white;
            Color faieStartColor = faieSprite != null ? faieSprite.color : Color.white;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);

                if (karaSprite != null)
                {
                    Color karaColor = karaStartColor;
                    karaColor.a = alpha;
                    karaSprite.color = karaColor;
                }

                if (faieSprite != null)
                {
                    Color faieColor = faieStartColor;
                    faieColor.a = alpha;
                    faieSprite.color = faieColor;
                }

                yield return null;
            }

            if (karaSprite != null)
            {
                Color karaFinalColor = karaStartColor;
                karaFinalColor.a = 0f;
                karaSprite.color = karaFinalColor;
            }

            if (faieSprite != null)
            {
                Color faieFinalColor = faieStartColor;
                faieFinalColor.a = 0f;
                faieSprite.color = faieFinalColor;
            }
        }
        health.SpawnPhase2();
    }

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_KaraWinterblade_State.Knockback),
            () => stateManager.ChangeState(Enemy_KaraWinterblade_State.Idle));
    }

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_KaraWinterblade_State.Chase))
            stateManager.ChangeState(Enemy_KaraWinterblade_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_KaraWinterblade_State.Idle))
            stateManager.ChangeState(Enemy_KaraWinterblade_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_KaraWinterblade_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => health.behavior;
    #endregion
}