using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_ArgeonHighmayne_Movement : MonoBehaviour, IEnemy_Movement
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_ArgeonHighmayne_Health health;
    [SerializeField] private BehaviorProfile behavior;
    [SerializeField] private float auraFarmingDuration = 5f;
    [SerializeField] private float stoppingDistance = 2f;

    private StateManager<Enemy_ArgeonHighmayne_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D charCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private Enemy_ArgeonHighmayneMK2_Attack attackComponent;
    private List<AttackCategory> attackCategories;

    [Header("Death Effects")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSoundEffect;
    [SerializeField] private GameObject phase2EntranceEffect;
    [SerializeField] private float delayToPhase2 = 4f;

    [Header("WarSurgeTeleportAttack")]
    [SerializeField] private GameObject warSurgeMarkEffect;
    [SerializeField] private GameObject warSurgeChargeUpEffect;
    [SerializeField] private GameObject warSurgeAfterTPEffect;
    [SerializeField] private float warSurgeSearchRadius = 50f;
    [SerializeField] private float warSurgeWaitTime = 3f;
    private Transform warSurgeTarget;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;

    private int facingDirection;
    private EnemyStats stats;
    private float castRange;
    private float attackCooldownTimer = 0f;
    private float warSurgeWaitTimer = 0f;
    private bool isRecovering = false;
    private bool isAuraFarming = false;

    private Dictionary<Enemy_ArgeonHighmayne_State, int> moveCooldownCounters = new();

    #region Move Cooldowns
    private void RegisterMoveUsed(Enemy_ArgeonHighmayne_State usedState)
    {
        // Tick down counters for every OTHER attack
        var keys = moveCooldownCounters.Keys.ToList();
        foreach (var key in keys)
        {
            if (key != usedState && moveCooldownCounters[key] > 0)
            {
                moveCooldownCounters[key]--;
            }
        }

        // Reset the cooldown counter for the attack that was just used
        if (attackCategories == null) return;
        foreach (var category in attackCategories)
        {
            foreach (var attack in category.Attacks)
            {
                if (attack.State == usedState)
                {
                    moveCooldownCounters[usedState] = attack.MoveCountCooldown;
                    return;
                }
            }
        }
    }

    private bool IsMoveOnCooldown(Enemy_ArgeonHighmayne_State state)
    {
        return moveCooldownCounters.TryGetValue(state, out int count) && count > 0;
    }
    #endregion

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
        public int MoveCountCooldown;
    }
    #endregion
    private bool IsInAnyAttackState()
    {
        return stateManager.IsInState(Enemy_ArgeonHighmayne_State.Attack) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.WarSurge) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.SunBloom) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.EntropicDecay) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.Decimated) ||
               stateManager.IsInState(Enemy_ArgeonHighmayne_State.AurynNexus);
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackComponent == null)
            attackComponent = GetComponent<Enemy_ArgeonHighmayneMK2_Attack>();

        if (charCollider == null)
            charCollider = GetComponent<Collider2D>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayne_Health>();

        stats = health.stats;
        castRange = stats.AttackRange * 2f;

        facingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        InitializeBehavior();
        behavior.ApplyDifficulty(DifficultyManager.Instance.CurrentDifficulty);

        stateManager = new StateManager<Enemy_ArgeonHighmayne_State>(animator, Enemy_ArgeonHighmayne_State.Idle);
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
        if (health.isDead || isAuraFarming || IsInAnyAttackState()) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (warSurgeWaitTimer > 0)
            warSurgeWaitTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Knockback) && !isRecovering)
            CheckForPlayer();

        if (stateManager.IsInState(Enemy_ArgeonHighmayne_State.Chase))
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

        if (distanceToPlayer <= stoppingDistance)
        {
            rb.velocity = Vector2.zero;
            stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
            return;
        }

        Flip();

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
                if (!stateManager.IsInState(Enemy_ArgeonHighmayne_State.Chase))
                    stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Chase);

                if (attackCooldownTimer <= 0)
                {
                    DecideAttackType();
                    attackCooldownTimer = stats.AttackCooldown;
                }
            }
            else if (!IsInAnyAttackState())
            {
                stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Chase);
            }
        }
        else
        {
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

        if (warSurgeTarget != null && warSurgeWaitTimer <= 0 && !IsInAnyAttackState())
        {
            stateManager.ChangeState(Enemy_ArgeonHighmayne_State.WarSurge);
            player = warSurgeTarget;
            warSurgeTarget = null;
            RegisterMoveUsed(Enemy_ArgeonHighmayne_State.WarSurge);

            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float random = Random.value;
        float cumulativeProbability = 0f;

        foreach (var category in attackCategories)
        {
            cumulativeProbability += category.Frequency;

            if (random < cumulativeProbability)
            {
                var availableAttacks = category.Attacks
                    .Where(a => distanceToPlayer <= a.Range && !IsMoveOnCooldown(a.State))
                    .ToArray();

                if (availableAttacks.Length > 0)
                {
                    var selectedAttack = availableAttacks[Random.Range(0, availableAttacks.Length)];

                    if (selectedAttack.State == Enemy_ArgeonHighmayne_State.WarSurge && warSurgeTarget == null)
                    {
                        if (distanceToPlayer <= warSurgeSearchRadius)
                        {
                            warSurgeTarget = player;
                            Vector3 position = player.position + new Vector3(0, 1.8f, 0);
                            Instantiate(warSurgeMarkEffect, position, Quaternion.identity, player.transform);
                            warSurgeWaitTimer = warSurgeWaitTime;
                        }
                    }
                    else
                    {
                        stateManager.ChangeState(selectedAttack.State);
                        RegisterMoveUsed(selectedAttack.State);
                    }
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
        stateManager.ChangeState(Enemy_ArgeonHighmayne_State.Idle);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(stats.AttackCooldown);
        isRecovering = false;
    }

    public void Flip()
    {
        if (player.position.x > transform.position.x && facingDirection == -1 ||
            player.position.x < transform.position.x && facingDirection == 1)
        {
            facingDirection *= -1;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
    }

    public void InitializeBehavior()
    {
        behavior = new BehaviorProfile
        {
            DetectionRange = 15f,
            SpecialAttackFrequency = 0.3f,
            UltimateAttackFrequency = 0.2f,
            Aggression = 1f,
            MobilityUsageFrequency = 1f,
        };

        attackCategories = new List<AttackCategory>
        {
            new() {
                Frequency = behavior.UltimateAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.Decimated,   Range = castRange, MoveCountCooldown = 5 },

                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.AurynNexus,  Range = castRange, MoveCountCooldown = 5 }
                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.SunBloom, Range = castRange, MoveCountCooldown = 4 },
                }
            },
            new() {
                Frequency = behavior.SpecialAttackFrequency * behavior.MobilityUsageFrequency,
                Attacks = new[]
                {
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.WarSurge, Range = stats.AttackRange, MoveCountCooldown = 3 },
                }
            },
            new() {
                Frequency = 1f,
                Attacks = new[]
                {
                    // Basic Attack: no count cooldown
                    new AttackConfig { State = Enemy_ArgeonHighmayne_State.Attack, Range = stats.AttackRange, MoveCountCooldown = 0 }
                }
            }
        };

        // Pre-populate counters so all attacks are available at the start
        moveCooldownCounters.Clear();
        foreach (var category in attackCategories)
            foreach (var attack in category.Attacks)
                moveCooldownCounters[attack.State] = 0;
    }

    public void PlayWarSurgeChargeUpEffect()
    {
        Vector3 effectPosition = transform.position + new Vector3(0, -1f, 0);
        Instantiate(warSurgeChargeUpEffect, effectPosition, Quaternion.identity);
    }

    private void WarSurgeTP()
    {
        float playerFacingDirection = player.localScale.x > 0 ? 1f : -1f;
        float offsetDistance = 1.5f;
        Vector3 targetPosition = player.position + new Vector3(-playerFacingDirection * offsetDistance, 0f, 0f);
        StartCoroutine(WarSurgeTPWaitTime(targetPosition));
    }

    private IEnumerator WarSurgeTPWaitTime(Vector3 targetPosition)
    {
        yield return new WaitForSeconds(warSurgeWaitTime);

        transform.position = targetPosition;
        Vector3 effectPosition = transform.position + new Vector3(0, -1f, 0);
        Instantiate(warSurgeAfterTPEffect, effectPosition, Quaternion.identity);

        Flip();
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
                Flip();
                break;
            case Enemy_ArgeonHighmayne_State.WarSurge:
                rb.velocity = Vector2.zero;
                Flip();
                WarSurgeTP();
                break;
            case Enemy_ArgeonHighmayne_State.SunBloom:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Decimated:
                Flip();
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.AurynNexus:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_ArgeonHighmayne_State.Death:
                rb.velocity = Vector2.zero;
                charCollider.enabled = false;
                break;
        }
    }

    private void OnStateExit(Enemy_ArgeonHighmayne_State state)
    {
    }
    #endregion

    public void PlayLastJudgement()
    {
        var location = transform.position + new Vector3(0, 1.28f, 0);
        Instantiate(deathEffect, location, Quaternion.identity);
        SoundFXManager.Instance.PlaySoundFXClip(deathSoundEffect, transform, 1f);
        StartCoroutine(DeathToPhase2());
    }
    private IEnumerator DeathToPhase2()
    {
        yield return new WaitForSeconds(delayToPhase2);
        Destroy(gameObject);
        var location = transform.position + new Vector3(0, 1.8f, 0);
        Instantiate(phase2EntranceEffect, location, Quaternion.identity);
    }

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
    public StateManager<Enemy_ArgeonHighmayne_State> GetStateManager() => stateManager;
    public BehaviorProfile GetBehavior() => behavior;
    #endregion

}