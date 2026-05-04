using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Chakkram_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_Chakkram_Health health;
    [SerializeField] private Enemy_Chakkram_Movement movement;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private float normalAttackRadius = 1f;
    [SerializeField] private float throwChakramDelay = 1.5f;

    [Header("Chakkram Throw Attack")]
    [SerializeField] private Transform chakramSpawnPoint;
    [SerializeField] private GameObject chakkramPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip channellingAudio;
    [SerializeField] private AudioClip throwChakramAudio;
    [SerializeField] private float volume = 1f;

    private DifficultyModifier difficultyModifier;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_Chakkram_Health>();

        if (movement == null)
            movement = GetComponent<Enemy_Chakkram_Movement>();

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void ChannellingAttack()
    {
        var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, normalAttackRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                player = hit.transform;
                DealDamage();
            }
        }
        StartCoroutine(ThrowChakkramCoroutine());
    }

    private IEnumerator ThrowChakkramCoroutine()
    {
        if (player == null)
        {
            if (movement.PlayerTransform != null)
                player = movement.PlayerTransform;
            else
            {
                var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, health.behavior.DetectionRange, playerLayer);

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
                    {
                        player = hit.transform;
                        break;
                    }
                }
            }
        }
        yield return new WaitForSeconds(throwChakramDelay);
        PlayAudio(1);
        var direction = (player.position - chakramSpawnPoint.position).normalized;
        var offset = new Vector3(0f, -0.2f);
        GameObject chakkram = Instantiate(chakkramPrefab, chakramSpawnPoint.position, Quaternion.identity);
        var chakkramComponent = chakkram.GetComponent<Enemy_Chakkram_FlyingWheel>();
        if (chakkramComponent != null && health != null)
        {
            chakkramComponent.Initialize(health.stats, direction + offset);
        }

        chakkram.SetActive(true);
    }

    private void DealDamage(float multiplier = 1f)
    {
        if (player == null) return;
        float damage = health.stats.Magic * multiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);
        
        if (player.CompareTag("Player"))
        {
            StatsManager.instance.TakeDamage(damage);
            if(player.TryGetComponent<PlayerMovement>(out var playerMovement))
            {
                playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
            }
        }
        else if (player.CompareTag("NPC"))
        {
            if (player.TryGetComponent<NPCCompanion>(out var npc))
            {
                npc.TakeDamage(damage, false);
            }
        }
    }

    public void PlayAudio(int num)
    {
        PlayAudio(num, -1f);
    }

    public void PlayAudio(int num, float volumeOverride)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(channellingAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(throwChakramAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}
