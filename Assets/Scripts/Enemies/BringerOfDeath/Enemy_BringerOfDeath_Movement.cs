﻿using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_BringerOfDeath_Movement : MonoBehaviour, IEnemy_Movement, IEnemyMovementContext
{
    [Header("Stats and Behavior")]
    [SerializeField] private Enemy_BringerOfDeath_Health health;
    [SerializeField] private bool isKnockbackable = true;
    [SerializeField] private float hurtAnimationDuration = 0.4f;

    private StateManager<Enemy_BringerOfDeath_State> stateManager;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Enemy_BringerOfDeath_Attack attackComponent;

    [Header("Transforms")]
    [SerializeField] private Transform detectionPoint;
    private Vector2 originalPosition;

    [Header("Patrol Settings")]
    [SerializeField] private float idleToPatrolWaitTime;
    private Vector2[] patrolPoints;
    public float patrolDistance;
    int currentPatrolIndex = 0;
    private bool isWaiting = false;
    private float stuckCheckTimer = 0f;
    private Vector2 lastCheckedPosition;
    private float waitTimer = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip footstepAudioClip;
    [SerializeField] private float volume = 1f;
    [SerializeField] private float minAudioDistance = 1f;
    [SerializeField] private float maxAudioDistance = 15f;
    private AudioSource loopingAudioSource;

    private float castRange;

    private float attackCooldownTimer = 0f;
    private GameObject playerLock;

    private KnockbackHandler knockbackHandler;
    private EnemyAttackRecovery attackRecovery;

    // enemy movement helper
    public Transform PlayerTransform { get ; set; }
    public bool IsRecovering { get ; set ; }
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


        if (health == null)
        {
            health = GetComponent<Enemy_BringerOfDeath_Health>();
        }

        if (attackComponent == null)
        {
            attackComponent = GetComponent<Enemy_BringerOfDeath_Attack>();
        }

        originalPosition = transform.position;

        patrolPoints = new Vector2[4];
        patrolPoints[0] = originalPosition + Vector2.up * patrolDistance;
        patrolPoints[1] = originalPosition + Vector2.down * patrolDistance;
        patrolPoints[2] = originalPosition + Vector2.left * patrolDistance;
        patrolPoints[3] = originalPosition + Vector2.right * patrolDistance;

        castRange = health.stats.AttackRange * 2.5f;

        FacingDirection = 1;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        stateManager = new StateManager<Enemy_BringerOfDeath_State>(animator, Enemy_BringerOfDeath_State.Idle);

        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;

        attackRecovery ??= new EnemyAttackRecovery(this, rb);
        knockbackHandler ??= new KnockbackHandler(this, rb);
    }

    private void Update()
    {
        if (health.isDead) return;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!stateManager.IsInState(Enemy_BringerOfDeath_State.Knockback) && !IsRecovering)
        {
            CheckForPlayer();
        }

        if (stateManager.IsInState(Enemy_BringerOfDeath_State.Chase))
        {
            Chase();
        }
        else if (stateManager.IsInState(Enemy_BringerOfDeath_State.Patrol))
        {
            Patrol();
        }
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle);
    }

    public void Chase()
    {
        EnemyMovementHelper.Chase(this, isStopOnAttackRange: false);
    }

    private void Patrol()
    {
        EnemyMovementHelper.Patrol(this, patrolPoints, ref currentPatrolIndex, ref isWaiting, ref waitTimer,
            ref stuckCheckTimer, ref lastCheckedPosition,
            idleToPatrolWaitTime,
            unstuckCheckInterval: 1.0f,   // check every 1 second
            stuckThreshold: 0.3f,          // must move at least 0.3 units per check
            () => stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle),
            () => stateManager.ChangeState(Enemy_BringerOfDeath_State.Patrol));
    }

    public void CheckForPlayer()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(detectionPoint.position, health.behavior.DetectionRange, playerLayer);

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

            if (closestTransform != null)
            {
                // store the locked player so other logic uses the same target
                PlayerTransform = closestTransform;
                playerLock = closestTransform.gameObject;

                float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);

                // Player within spell range
                if (distanceToPlayer <= castRange)
                {
                    rb.velocity = Vector2.zero;

                    if (attackCooldownTimer <= 0)
                    {
                        DecideAttackType();
                        attackCooldownTimer = health.stats.AttackCooldown;
                    }
                }
                // Player outside spell range → chase
                else if (distanceToPlayer > castRange &&
                         !(stateManager.IsInState(Enemy_BringerOfDeath_State.Attack) ||
                           stateManager.IsInState(Enemy_BringerOfDeath_State.Cast)))
                {
                    stateManager.ChangeState(Enemy_BringerOfDeath_State.Chase);
                }
            }
        }
        else
        {
            // Clear any previous lock
            playerLock = null;
            // Optionally keep the `player` reference for other systems, or set to null:
            // player = null;

            if (!stateManager.IsInState(Enemy_BringerOfDeath_State.Patrol) &&
                !stateManager.IsInState(Enemy_BringerOfDeath_State.Attack) &&
                !stateManager.IsInState(Enemy_BringerOfDeath_State.Cast))
            {
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Patrol);
            }
        }
    }

    private void DecideAttackType()
    {
        if (PlayerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, PlayerTransform.position);
        float random = Random.value;

        if (random < health.behavior.SpecialAttackFrequency)
        {
            if (distanceToPlayer <= castRange)
            {
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Cast);
            }
            else
            {
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Chase);
            }
        }

        else
        {
            if (distanceToPlayer <= health.stats.AttackRange)
            {
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Attack);
            }
            else
            {
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Chase);
            }
        }
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;
        IsRecovering = true;
        // Only transition out of attack states
        if (!stateManager.IsInState(Enemy_BringerOfDeath_State.Attack) &&
            !stateManager.IsInState(Enemy_BringerOfDeath_State.Cast))
            return;
        attackRecovery.StartRecovery(health.stats.AttackCooldown,
            () =>
            {
                IsRecovering = true;
                stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle);
            },
            () =>
            {
                IsRecovering = false;
            });
    }

    #region State Callbacks
    private void OnStateEnter(Enemy_BringerOfDeath_State state)
    {
        switch (state)
        {
            case Enemy_BringerOfDeath_State.Attack:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_BringerOfDeath_State.Cast:
                rb.velocity = Vector2.zero;
                break;
            case Enemy_BringerOfDeath_State.Hurt:
                rb.velocity = Vector2.zero;
                StartCoroutine(HurtDelay());
                break;
            case Enemy_BringerOfDeath_State.Death:
                Destroy(gameObject, 1.3f);
                break;
            case Enemy_BringerOfDeath_State.Chase:
                {
                    if (footstepAudioClip != null)
                    {
                        loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                            footstepAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                        if (loopingAudioSource != null)
                        {
                            loopingAudioSource.transform.SetParent(transform);
                        }
                    }
                }
                break;
            case Enemy_BringerOfDeath_State.Patrol:
                {
                    stuckCheckTimer = 0f;
                    lastCheckedPosition = transform.position;
                    if (footstepAudioClip != null)
                    {
                        loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                            footstepAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                        if (loopingAudioSource != null)
                        {
                            loopingAudioSource.transform.SetParent(transform);
                        }
                    }
                    break;
                }
        }
    }

    private IEnumerator HurtDelay()
    {
        yield return new WaitForSeconds(hurtAnimationDuration);
        stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle);
    }

    private void OnStateExit(Enemy_BringerOfDeath_State state)
    {
        switch (state)
        {
            case Enemy_BringerOfDeath_State.Chase:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
            case Enemy_BringerOfDeath_State.Patrol:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
        }
    }
    #endregion
    public void KnockBack(Transform player, float knockbackForce, float knockbackTime, float stunTime)
    {
        if (!isKnockbackable || knockbackHandler == null) return;

        knockbackHandler.ApplyKnockback(transform, player, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(Enemy_BringerOfDeath_State.Knockback),
            () => stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle));
    }

    public StateManager<Enemy_BringerOfDeath_State> GetStateManager()
    {
        return stateManager;
    }

    public bool IsInAnyAttackState() =>
        stateManager.IsInState(Enemy_BringerOfDeath_State.Attack) ||
        stateManager.IsInState(Enemy_BringerOfDeath_State.Cast);

    public void ChangeToChaseState()
    {
        if (!stateManager.IsInState(Enemy_BringerOfDeath_State.Chase))
            stateManager.ChangeState(Enemy_BringerOfDeath_State.Chase);
    }

    public void ChangeToIdleState()
    {
        if (!stateManager.IsInState(Enemy_BringerOfDeath_State.Idle))
            stateManager.ChangeState(Enemy_BringerOfDeath_State.Idle);
    }
}
