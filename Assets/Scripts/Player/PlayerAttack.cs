using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAttack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private float normalAttackRadius = 1f;
    [SerializeField] private GameObject normalAttackHitEffect;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip normalAttackSwingAudioClip;
    [SerializeField] private AudioClip normalAttackHitAudioClip;
    [SerializeField] private float volume = 1f;

    private bool hitEnemy = false;
    private float attackCooldownTimer = 0f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (movement == null)
            movement = GetComponent<PlayerMovement>();

        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (Input.GetButtonDown("Slash") && CanAttack())
            TriggerNormalAttack();
    }

    private bool CanAttack()
    {
        var stateManager = movement.GetStateManager();
        return stateManager != null &&
               attackCooldownTimer <= 0f &&
               !stateManager.IsInState(PlayerState.Attack) &&
               !stateManager.IsInState(PlayerState.Knockback) &&
               !stateManager.IsInState(PlayerState.Death) &&
               !stateManager.IsInState(PlayerState.Dash);
    }

    /// <summary>
    /// Triggers the attack animation. Actual hit detection is called via animation event.
    /// </summary>
    private void TriggerNormalAttack()
    {
        attackCooldownTimer = StatsManager.instance.cooldown;
        movement.GetStateManager().ChangeState(PlayerState.Attack);
        SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume);
    }

    /// <summary>
    /// Called by animation event at the hit frame of the normal attack animation.
    /// </summary>
    public void NormalAttack()
    {
        // Gộp enemyLayer với Layer "Player" (và "Default") để đảm bảo quét trúng Alicia
        int combinedLayer = enemyLayer | LayerMask.GetMask("Player", "Default", "NPC");
        var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, normalAttackRadius, combinedLayer);

        foreach (var hit in hits)
        {
            // CHỐT CHẶN: Bỏ qua chính bản thân Player
            if (hit.gameObject == this.gameObject) continue;

            // 1. Nếu chém trúng Quái (Cái này vẫn cần CompareTag vì quái có nhiều loại)
            if (hit.CompareTag("Enemy"))
            {
                if (!hitEnemy)
                {
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    hitEnemy = true;
                }

                if (normalAttackHitEffect != null)
                    Instantiate(normalAttackHitEffect, hit.transform.position, Quaternion.identity, hit.transform);

                var enemyHealth = hit.GetComponent<IEnemy_Health>();
                enemyHealth?.ChangeHealth(-StatsManager.instance.damage);
                var enemyMovement = hit.GetComponent<IEnemy_Movement>();
                enemyMovement.KnockBack(transform, StatsManager.instance.knockbackForce, StatsManager.instance.knockbackTime, StatsManager.instance.stunTime);
            }
            // 2. NẾU CHÉM TRÚNG BẤT CỨ AI CÓ SCRIPT "NPCCompanion" (Bao gồm Alicia)
            else
            {
                var npc = hit.GetComponent<NPCCompanion>();
                if (npc != null)
                {
                    // Trúng NPC rồi! Truyền sát thương của Player vào, báo True để trừ điểm
                    npc.TakeDamage(StatsManager.instance.damage, true);

                    if (normalAttackHitEffect != null)
                        Instantiate(normalAttackHitEffect, hit.transform.position, Quaternion.identity, hit.transform);
                }
            }
        }

        hitEnemy = false;
    }

    /*public void OnAttackAnimationComplete()
    {
        movement.OnAttackAnimationComplete();
    }*/

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (normalAttackPoint == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawSphere(normalAttackPoint.position, normalAttackRadius);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(normalAttackPoint.position, normalAttackRadius);
    }
#endif
}