using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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
    private bool isMovementStopped = false;

    private void Awake()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
        animator = animator != null ? animator : GetComponent<Animator>();
        if (animator != null) animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
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

    private void Update() { HandleDashInput(); }

    private void FixedUpdate()
    {
        if (stateManager.IsInState(PlayerState.Knockback) ||
            stateManager.IsInState(PlayerState.Dash) ||
            isMovementStopped) return;

        ReadMoveInput();
        HandleFlip();
        UpdateAnimatorState();
        ApplyMovement();

        if (moveInput.sqrMagnitude > 0 && QuestManager.instance != null)
            QuestManager.instance.AddMovementProgress(Time.fixedDeltaTime);

        if (StatsManager.instance.currentStamina < StatsManager.instance.maxStamina)
            StatsManager.instance.currentStamina += StatsManager.instance.staminaRegenRate * Time.deltaTime;
    }

    private void HandleDashInput()
    {
        if (Input.GetButtonDown("Dash") && CanDash()) StartDash();
    }

    private void ReadMoveInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
    }

    private void ApplyMovement()
    {
        rb.velocity = moveInput.sqrMagnitude > 0 ? moveInput.normalized * StatsManager.instance.moveSpeed : Vector2.zero;
    }

    private void HandleFlip()
    {
        bool movingRight = moveInput.x > 0 && facingDirection == -1;
        bool movingLeft = moveInput.x < 0 && facingDirection == 1;
        if ((movingRight || movingLeft) && !stateManager.IsInState(PlayerState.Attack)) Flip();
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
        if (shouldFaceRight != alreadyFacingRight) Flip();
    }

    private void UpdateAnimatorState()
    {
        // ĐÃ SỬA: Bảo vệ các trạng thái Skill không bị Update() ghi đè lập tức
        if (stateManager.IsInState(PlayerState.Attack) || 
            stateManager.IsInState(PlayerState.Hurt) || 
            stateManager.IsInState(PlayerState.Death) ||
            stateManager.IsInState(PlayerState.KnightSkill) ||
            stateManager.IsInState(PlayerState.ArcherSkill) ||
            stateManager.IsInState(PlayerState.RogueSkill)) 
            return;
        
        if (moveInput.sqrMagnitude > 0) stateManager.ChangeState(PlayerState.Move);
        else stateManager.ChangeState(PlayerState.Idle);
    }

    private bool CanDash() => !stateManager.IsInState(PlayerState.Dash) && !stateManager.IsInState(PlayerState.Knockback) && StatsManager.instance.currentStamina >= StatsManager.instance.staminaCost;

    private void StartDash()
    {
        dashDirection = moveInput.sqrMagnitude > 0 ? moveInput.normalized : new Vector2(facingDirection, 0f);
        StatsManager.instance.currentStamina -= StatsManager.instance.staminaCost;
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

    public void OnAttackAnimationComplete()
    {
        if (stateManager == null) return;
        if (stateManager.IsInState(PlayerState.Attack)) stateManager.ChangeState(PlayerState.Idle);
    }

    public StateManager<PlayerState> GetStateManager() => stateManager;

    public void StopMovement(float duration)
    {
        StopCoroutine(nameof(StopMovementRoutine));
        StartCoroutine(StopMovementRoutine(duration));
    }

    private IEnumerator StopMovementRoutine(float duration)
    {
        isMovementStopped = true;
        moveInput = Vector2.zero;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(duration);
        isMovementStopped = false;
    }

    #region State Callbacks
    private void OnStateChanged(PlayerState previousState, PlayerState newState) { }
    
    private void OnStateEnter(PlayerState state)
    {
        string className = ClassManager.Instance.CurrentClassData.playerClass.ToString().ToLower();
        string stateName = state.ToString().ToLower(); 

        if (stateName == "move") stateName = "walk";
        // ĐÃ SỬA: Tự động gom chữ "knightskill", "archerskill" thành "skill"
        else if (stateName.Contains("skill")) stateName = "skill";

        if (animator != null) 
        {
            animator.Play($"{className}_{stateName}");
        }
        
        switch (state)
        {
            case PlayerState.Move:
                if (movementAudioClip != null)
                {
                    loopingAudioSource = SoundFXManager.Instance.PlayLoopingSoundFXClip(movementAudioClip, transform, volume, minAudioDistance, maxAudioDistance);
                    if (loopingAudioSource != null) loopingAudioSource.transform.SetParent(transform);
                }
                break;
            case PlayerState.Attack:
            case PlayerState.Hurt:
            case PlayerState.Death:
            // ĐÃ SỬA: Đứng yên khi dùng Skill
            case PlayerState.KnightSkill:
            case PlayerState.ArcherSkill:
            case PlayerState.RogueSkill:
                rb.velocity = Vector2.zero;
                break;
        }
    }

    private void OnStateExit(PlayerState state)
    {
        if (state == PlayerState.Move)
        {
            SoundFXManager.Instance.StopAndDestroyAudioSource(loopingAudioSource);
            loopingAudioSource = null;
        }
    }
    #endregion
}