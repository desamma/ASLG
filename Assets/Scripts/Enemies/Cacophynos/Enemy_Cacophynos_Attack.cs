using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Cacophynos_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_Cacophynos_Health health;
    [SerializeField] private BoxCollider2D hitBox1;
    [SerializeField] private BoxCollider2D hitBox2;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector2 normalAttackHitBox = new(1f, 2f);
    [SerializeField] private GameObject hitEffect;

    [Header("Attack Flying Settings")]
    [SerializeField] private float flyingDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpAudio;
    [SerializeField] private AudioClip spinningAudio;
    [SerializeField] private AudioClip swordSlamAudio;
    [SerializeField] private float volume = 1f;

    private DifficultyModifier difficultyModifier;
    private Coroutine flyingCoroutine;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_Cacophynos_Health>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, 0f, playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player") && !hit.CompareTag("NPC")) continue;

            player = hit.transform;

            DealDamage();
            Instantiate(hitEffect, player.position, Quaternion.identity);
        }
    }

    public void StartAttackFlying()
    {
        if (flyingCoroutine != null)
            StopCoroutine(flyingCoroutine);
        else
        {
            var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, health.behavior.DetectionRange, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player") && !hit.CompareTag("NPC")) continue;

                player = hit.transform;
                break;
            }
        }
        PlayAudio(0);
        flyingCoroutine = StartCoroutine(FlyingSequence());
    }

    private IEnumerator FlyingSequence()
    {
        ChangeCollider();

        yield return StartCoroutine(FlyToTarget(new Vector3(1.5f, 0f), flyingDuration));
        PlayAudio(1);

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FlyToTarget(new Vector3(1f, 0f), flyingDuration));
        PlayAudio(2);

        ChangeCollider();

        //stop naturally
        if (flyingCoroutine != null)
        {
            StopCoroutine(flyingCoroutine);
            flyingCoroutine = null;
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

    private void DealDamage(float multiplier = 1f)
    {
        if (player == null) return;
        float damage = health.stats.Magic * multiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);
        
        if (player.CompareTag("Player"))
        {
            StatsManager.instance.TakeDamage(damage);
        }
        else if (player.CompareTag("NPC"))
        {
            if (player.TryGetComponent<NPCCompanion>(out var npc))
            {
                npc.TakeDamage(damage, false);
            }
        }
    }

    public void PlayAudio(int num, float volumeOverride = -1f)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(jumpAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(spinningAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 2:
                SoundFXManager.Instance.PlaySoundFXClip(swordSlamAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}
