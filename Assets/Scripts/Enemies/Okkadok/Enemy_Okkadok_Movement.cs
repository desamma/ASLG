using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Okkadok_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_Okkadok_Health health;
    [SerializeField] private BehaviorProfile behavior;

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
    private EnemyStats stats;
    private bool isRecovering = false;
    private bool runAwayAfterAttack = false;
    private EnemyMoveCooldownTracker<Enemy_Okkadok_State> moveCooldowns = new();

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

        stats = health.stats;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_Okkadok_State>(animator, Enemy_Okkadok_State.Idle);
        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void Update()
    {
        if (health.isDead) return;

        if (!stateManager.IsInState(Enemy_Okkadok_State.Knockback) && !isRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_Okkadok_State.Chase))
            Chase();
        else if (stateManager.IsInState(Enemy_Okkadok_State.Retreat))
            Retreat();
    }

    private void OnEnable() => DifficultyManager.Instance.OnDifficultyChanged += OnDifficultyChanged;
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

    public void CheckForPlayer()
    {
        if (player != null)
        {
            if (Vector2.Distance(transform.position, player.position) > behavior.DetectionRange)
                player = null;
        }
        else
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(detectionPoint.position, behavior.DetectionRange, playerLayer);
            if (hits.Length > 0)
            {
                float best = float.MaxValue;
                Transform closest = null;
                foreach (var col in hits)
                {
                    float sq = (col.transform.position - detectionPoint.position).sqrMagnitude;
                    if (sq < best) { best = sq; closest = col.transform; }
                }
                player = closest;
            }
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
        bool playerFacingUs = IsPlayerFacingEnemy();

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
                    if (!stateManager.IsInState(Enemy_Okkadok_State.Idle))
                    {
                        stateManager.ChangeState(Enemy_Okkadok_State.Idle);
                        rb.velocity = Vector2.zero;
                    }
                    Flip();
                }
            }
        }
        else
        {
            // Player is facing away
            if (!IsInAnyAttackState())
            {
                if (dist <= stats.AttackRange)
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
        rb.velocity = behavior.Aggression * stats.Speed * direction;
    }

    private void Retreat()
    {
        if (player == null) return;
        FlipAwayFromPlayer();
        Vector2 direction = (transform.position - player.position).normalized;
        rb.velocity = retreatSpeedNegate * behavior.Aggression * stats.Speed * direction;
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
                }
                return;
            }
        }
    }
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null || !IsInAnyAttackState()) return;
        StartCoroutine(AttackRecovery());
    }

    private IEnumerator AttackRecovery()
    {
        isRecovering = true;
        stateManager.ChangeState(Enemy_Okkadok_State.Idle);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stats.AttackCooldown);
        isRecovering = false;
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

    private void FlipAwayFromPlayer()
    {
        if (player == null) return;
        // We want to face AWAY from the player, so invert Flip logic
        if ((player.position.x > transform.position.x && facingDirection == 1) ||
            (player.position.x < transform.position.x && facingDirection == -1))
        {
            facingDirection *= -1;
            Vector3 s = transform.localScale; s.x *= -1; transform.localScale = s;
        }
    }

    private bool IsPlayerFacingEnemy()
    {
        if (player == null) return false;
        float dx = transform.position.x - player.position.x;
        // positive localScale.x → player faces right; negative → faces left
        float playerFacingSign = Mathf.Sign(player.localScale.x);
        return (playerFacingSign > 0f && dx > 0f) || (playerFacingSign < 0f && dx < 0f);
    }

    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 10f,

            Aggression = 1f
        };

        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_Okkadok_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        moveCooldowns.Initialize(attackCategories
            .SelectMany(c => c.Attacks)
            .Select(a => a.State));
    }

    #region State Manager Callbacks
    private void OnStateChanged(Enemy_Okkadok_State previous, Enemy_Okkadok_State next) { }

    private void OnStateEnter(Enemy_Okkadok_State state)
    {
        switch (state)
        {
            case Enemy_Okkadok_State.Attack:
                rb.velocity = Vector2.zero;
                Flip();
                break;
            case Enemy_Okkadok_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    private void OnStateExit(Enemy_Okkadok_State state) { }
    #endregion

    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        stateManager.ChangeState(Enemy_Okkadok_State.Knockback);
        StartCoroutine(KnockBackCounter(knockbackTime, stunTime));
        Vector2 dir = (transform.position - player.position).normalized;
        rb.velocity = dir * knockbackForce;
    }

    private IEnumerator KnockBackCounter(float knockbackTime, float stunTime)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stunTime);
        stateManager.ChangeState(Enemy_Okkadok_State.Idle);
    }

    #region Getters
    public StateManager<Enemy_Okkadok_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion
}