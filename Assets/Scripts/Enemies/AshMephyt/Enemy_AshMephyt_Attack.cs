using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_AshMephyt_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_AshMephyt_Health health;
    [SerializeField] private Enemy_AshMephyt_Movement movement;
    [SerializeField] private BoxCollider2D hitBox1;
    [SerializeField] private BoxCollider2D hitBox2;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector2 normalAttackHitBox = new(1f, 2f);
    [SerializeField] private float attackMultiplier = 0.7f;
    [SerializeField] private float attackDuration = 1f;
    [SerializeField] private float damageInterval = 0.3f;
    [SerializeField] private GameObject hitEffect;

    [Header("Enraged Burn")]
    [SerializeField] private float burnDuration = 3f;
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private float burnDamageMultiplier = 0.1f;
    [SerializeField] private StatusEffect burnEffect;

    [Header("Attack Flying Settings")]
    [SerializeField] private float flyingBackDuration = 0.5f;
    [SerializeField] private float flyingToPlayerDuration = 0.7f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireBreathAudio;
    [SerializeField] private float volume = 1f;

    private DifficultyModifier difficultyModifier;
    private bool hitPlayer = false;
    private Coroutine attackCoroutine;
    private Coroutine flyingBackCoroutine;
    private Coroutine burnCoroutine;
    private float burnTimeRemaining;
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_AshMephyt_Health>();

        if (movement == null)
            movement = GetComponent<Enemy_AshMephyt_Movement>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void StartNormalAttack()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        hitPlayer = false;
        attackCoroutine = StartCoroutine(NormalAttackPhaseLoop());
    }

    private void StopNormalAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        hitPlayer = false;
    }

    private IEnumerator NormalAttackPhaseLoop()
    {
        float elapsed = 0f;

        while (elapsed < attackDuration)
        {
            NormalAttackTick();
            yield return new WaitForSeconds(damageInterval);
            elapsed += damageInterval;
        }

        //stop naturally
        StopNormalAttack();
    }

    private void NormalAttackTick()
    {
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, 0f, playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            player = hit.transform;

            if (!hitPlayer)
            {
                PlayAudio(0);
                hitPlayer = true;
            }

            DealDamage(attackMultiplier);
            Instantiate(hitEffect, player.position, Quaternion.identity);
            if (IsEnraged())
            {
                ApplyOrRefreshBurn();
            }
        }
    }

    private bool IsEnraged()
    {
        if (health == null) return false;

        return health.stats.CurrentHP <= health.stats.MaxHP * health.behavior.EnrageThreshold;
    }

    private void ApplyOrRefreshBurn()
    {
        burnTimeRemaining = burnDuration;
        
        if (player == null) return;
        
        player.TryGetComponent<StatusEffectManager>(out var statusEffectManager);
        
        if (statusEffectManager != null)
        {
            if (burnCoroutine != null)
                statusEffectManager.StopCoroutine(burnCoroutine);
            
            burnCoroutine = statusEffectManager.StartCoroutine(BurnRoutine(statusEffectManager));
        }
    }

    private IEnumerator BurnRoutine(StatusEffectManager statusEffectManager)
    {
        if (burnEffect != null)
        {
            statusEffectManager.ApplyEffect(burnEffect, true, burnDuration)
                .WithStatLines($"Deal {burnDamageMultiplier * health.stats.Magic} burn damage over {burnDuration} seconds")
                .WithFlavour("0.5s per tick");
        }

        while (burnTimeRemaining > 0f)
        {
            DealDamage(burnDamageMultiplier);
            yield return new WaitForSeconds(burnTickInterval);
            burnTimeRemaining -= burnTickInterval;
        }
        burnCoroutine = null;
    }

    public void StartAttackFlying()
    {
        if (flyingBackCoroutine != null)
            StopCoroutine(flyingBackCoroutine);

        if (player == null)
        {
            if (movement.PlayerTransform != null)
                player = movement.PlayerTransform;
            else
            {
                var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, health.behavior.DetectionRange, playerLayer);

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Player")) continue;

                    player = hit.transform;
                    break;
                }
            }
        }

        flyingBackCoroutine = StartCoroutine(FlyingSequence());
    }

    private IEnumerator FlyingSequence()
    {
        ChangeCollider();

        yield return StartCoroutine(FlyToTarget(new Vector3(-1f, 1f), flyingBackDuration));
        yield return new WaitForSeconds(0.2f);

        movement.FacingDirection = TransformHelper.FlipTowards(transform, player, movement.FacingDirection);

        if (Vector3.Distance(transform.position, player.position) <= 2f)
        {
            yield return StartCoroutine(FlyToTarget(new Vector3(-1f, 0.5f), flyingBackDuration));
        }
        else
        {
            var facingDirection = transform.localScale.x > 0 ? 1 : -1;
            var offsetToPlayer = player.position - transform.position;

            if (facingDirection < 0)
                offsetToPlayer.x = -offsetToPlayer.x;

            yield return StartCoroutine(FlyToTarget(offsetToPlayer, flyingToPlayerDuration));
        }

        ChangeCollider();

        //stop naturally
        if (flyingBackCoroutine != null)
        {
            StopCoroutine(flyingBackCoroutine);
            flyingBackCoroutine = null;
        }
    }

    private IEnumerator FlyToTarget(Vector3 offset, float duration)
    {
        float elapsedTime = 0f;
        var currentPos = transform.position;
        var facingDirection = transform.localScale.x > 0 ? 1 : -1;
        Vector3 destination;
        if (facingDirection > 0)
        {
            destination = currentPos + offset;
        }
        else
        {
            destination = currentPos + new Vector3(-offset.x, offset.y);
        }

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(currentPos, destination, elapsedTime / duration);
            elapsedTime += Time.deltaTime;

            yield return null;
        }
        transform.position = destination;
    }

    private void ChangeCollider()
    {
        hitBox1.enabled = !hitBox1.enabled;
        hitBox2.enabled = !hitBox2.enabled;
    }

    private void DealDamage(float multiplier)
    {
        if (player == null) return;
        float damage = health.stats.Magic * multiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);
        StatsManager.instance.TakeDamage(damage);
    }

    public void PlayAudio(int num, float volumeOverride = -1f)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(fireBreathAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}
