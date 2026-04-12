using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
[DisallowMultipleComponent]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Header("State (Read Only)")]
    [SerializeField] private int facingDirection = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip movementAudioClip;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float minAudioDistance = 1f;
    [SerializeField] private float maxAudioDistance = 10f;
    private AudioSource loopingAudioSource;

    private StateManager<PlayerState> stateManager;

    private Vector2 moveInput;
    private Vector2 dashDirection;
    private KnockbackHandler knockbackHandler;
    private void Awake()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
        animator = animator != null ? animator : GetComponent<Animator>();

        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
    }

    private void Start()
    {
        stateManager = new StateManager<PlayerState>(animator, PlayerState.Idle);
        knockbackHandler = new KnockbackHandler(this, rb);

        stateManager.OnStateChanged += OnStateChanged;
        stateManager.OnStateEnter += OnStateEnter;
        stateManager.OnStateExit += OnStateExit;
    }

    private void OnDisable()
    {
        if (stateManager != null)
        {
            stateManager.OnStateChanged -= OnStateChanged;
            stateManager.OnStateEnter -= OnStateEnter;
            stateManager.OnStateExit -= OnStateExit;
        }
    }

    private void Update()
    {
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        if (stateManager.IsInState(PlayerState.Knockback) ||
            stateManager.IsInState(PlayerState.Dash)) return;

        ReadMoveInput();
        HandleFlip();
        UpdateAnimatorState();
        ApplyMovement();
    }

    private void HandleDashInput()
    {
        if (Input.GetButtonDown("Dash") && CanDash())
            StartDash();
    }

    private void ReadMoveInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }

    private void ApplyMovement()
    {
        rb.velocity = moveInput.sqrMagnitude > 0
            ? moveInput.normalized * StatsManager.instance.moveSpeed
            : Vector2.zero;
    }

    private void HandleFlip()
    {
        bool movingRight = moveInput.x > 0 && facingDirection == -1;
        bool movingLeft = moveInput.x < 0 && facingDirection == 1;

        if ((movingRight || movingLeft) && !stateManager.IsInState(PlayerState.Attack))
            Flip();
    }

    private void Flip()
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void FaceToward(float worldX)
    {
        bool shouldFaceRight = worldX > transform.position.x;
        bool alreadyFacingRight = facingDirection == 1;

        if (shouldFaceRight != alreadyFacingRight)
            Flip();
    }

    private void UpdateAnimatorState()
    {
        if (stateManager.IsInState(PlayerState.Attack) ||
            stateManager.IsInState(PlayerState.Hurt) ||
            stateManager.IsInState(PlayerState.Death))
            return;

        if (moveInput.sqrMagnitude > 0)
            stateManager.ChangeState(PlayerState.Move);
        else
            stateManager.ChangeState(PlayerState.Idle);
    }

    private bool CanDash() =>
        !stateManager.IsInState(PlayerState.Dash) &&
        !stateManager.IsInState(PlayerState.Knockback) &&
        StatsManager.instance.currentStamina >= StatsManager.instance.staminaCost;

    private void StartDash()
    {
        dashDirection = moveInput.sqrMagnitude > 0
            ? moveInput.normalized
            : new Vector2(facingDirection, 0f);

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        stateManager.ChangeState(PlayerState.Dash);

        float dashSpeed = StatsManager.instance.moveSpeed * 2.5f;
        float dashDuration = StatsManager.instance.dashDuration;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            rb.velocity = dashDirection * dashSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(StatsManager.instance.dashDelay);

        stateManager.ChangeState(PlayerState.Idle);
    }

    public void KnockBack(Transform enemy, float knockbackForce, float knockbackTime, float stunTime)
    {
        knockbackHandler.ApplyKnockback(transform, enemy, knockbackForce, knockbackTime, stunTime,
            () => stateManager.ChangeState(PlayerState.Knockback),
            () => stateManager.ChangeState(PlayerState.Idle));
    }

    /// <summary>
    /// Called by animation event when attack animation completes
    /// </summary>
    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;

        if (stateManager.IsInState(PlayerState.Attack))
            stateManager.ChangeState(PlayerState.Idle);
    }

    public StateManager<PlayerState> GetStateManager() => stateManager;

    public void StopMovement(float duration)
    {
        StopCoroutine(nameof(StopMovementRoutine));
        StartCoroutine(StopMovementRoutine(duration));
    }

    private IEnumerator StopMovementRoutine(float duration)
    {
        moveInput = Vector2.zero;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(duration);

        moveInput = Vector2.zero;
    }

    #region State Callbacks
    private void OnStateChanged(PlayerState previousState, PlayerState newState)
    {
    }

    private void OnStateEnter(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Move:
                if (movementAudioClip != null)
                {
                    loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(
                        movementAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                    if (loopingAudioSource != null)
                    {
                        loopingAudioSource.transform.SetParent(transform);
                    }
                }
                break;
            case PlayerState.Attack:
                rb.velocity = Vector2.zero;
                break;
            case PlayerState.Hurt:
                rb.velocity = Vector2.zero;
                break;
            case PlayerState.Death:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Move:
                SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
                loopingAudioSource = null;
                break;
        }
    }
    #endregion
}